# Tedd.Profiler
Simple profiler for measuring parts of application.

Availabe on NuGet: [https://www.nuget.org/packages/Tedd.Profiler](https://www.nuget.org/packages/Tedd.Profiler)

# Examples

The Tedd.Profiler.Examples project contains examples on how to use profiler. 

For example, averaging ping times:

```c#
public class PingAverage: IWorker
{
    // Set up a Profiler using default ProfilerGroup
    private static readonly Profiler _profiler = ProfilerGroup.Default.CreateInstance(new ProfilerOptions(ProfilerType.SampleAverageTimeMs, 1_000, 10_000), "Ping");

    private bool _running = false;
    public Task Task { get; private set; } = Task.CompletedTask;

    public void Start()
    {
        // Set up a task to run
        _running = true;
        Task = Task.Run(RunLoop);
    }

    public void Stop()
    {
        // Signal task to stop at next iteration
        _running = false;
    }

    private async Task RunLoop()
    {
        // Set up ping
        var ping = new Ping();
        var options = new PingOptions
        {
            DontFragment = true
        };
        var data = new string('*', 32);
        var buffer = Encoding.ASCII.GetBytes(data);
        var timeout = 120;

        // Do forever until Stop() is called
        while (_running)
        {
            // Send ping
            var reply = await ping.SendPingAsync("www.google.com", timeout, buffer, options);
            if (reply.Status == IPStatus.Success)
            {
                // // // // // // // // // // // // // //
                // Add a measurement to our profiler.  //
                // // // // // // // // // // // // // //
                _profiler.AddTimeMeasurement(reply.RoundtripTime * 10_000, 1);
            }

            // Throttle so we don't flood target
            await Task.Delay(500);
        }
    }
}
```
Creating a Profiler using a ProfilerGroup allows us to pull the key+values from all Profilers in that group.

```c#
// Print result
foreach (var kv in ProfilerGroup.Default.GetMeasurements())
{
    var key = kv.Key.PadRight(50, ' ') + ": ";
    Console.WriteLine(key + kv.Value.PadLeft(15, ' '));
}
```
# Name

When creating a Profiler you can choose any name. Multiple instances of the profiler can have same name.  

If using ProfilerGroup.CreateInstanceWithPath(name) the full name of the class will precede name, for example MyApp.SomeNamespace.ClassName. You should then provide a name with a separator, such as ":method" or ".method".

# Different modes

Each instance of the profiler can function in different modes.

| ProfilerType          | Description                                                  |
| --------------------- | ------------------------------------------------------------ |
| Counter               | Thread synchronized (or not synchronized) integer            |
| Text                  | Text                                                         |
| TimeTotal             | Thread synchronized cumulative time                          |
| SampleAverageTimeMs   | Thread synchronized average of cumulative time based on sample count |
| CountAveragePerSecond | Same as TimeAverage, but value is calculated as samples per second. |

## Counter

The counter can be increased, decreased or set. You can choose between atomic (thread safe) operation or not. An atomic operation uses Interlocked.Add or Interlocked.Inc to increase counter, whereas non-atomic simply add directly. Called directly the atomic is around 2x slower than non-atomic, but your actual result may vary. You must pick the best suited method for your use case.

```c#
var profiler = new Profiler(new ProfilerOptions(ProfilerType.Counter), "Test");
profiler.Inc(5);
profiler.Dec();
profiler.AtomicInc(1);
profiler.AtomicDec(5);
profiler.Set(3);
```
## TimeTotal

Similar to Counter, but uses the ticks-part of AddSample. It will get results from use of timer (with CreateTimer()).

```c#
var profiler = new Profiler(new ProfilerOptions(ProfilerType.TimeTotal), "Test");
profiler.AddTimeMeasurementMs(1, 0); // 1 ms
profiler.AddTimeMeasurement(15_000, 0); // 1.5 ms
// profiler.GetValue() is now: 2.5
```
## SampleAverageTimeMs

Calculating sample average time is done by keeping history of each record added. ProfilerOptions has parameters for cleaning up excess or expires items.

During Cleanup() all excess items are removed from history and subtracted from global numbers. Cleanup() is run on every sample, as well as when you pull numbers (GetValue() or GetText()).

You can set it to manual by setting AutoCleanup = false in ProfilerOptions. If you do that and you sample a lot of samples then it is important to run Cleanup() manually on the Profiler instance regularly, or sample history will fill up infinitely until cleaned by GetValue() or GetText().

Tip: If you are looking for operations per second then keeping a maximum history of 100 records and 2 000 ms may be enough, there is usually no need for very large ranges.

```c#
var profiler = new Profiler(new ProfilerOptions(ProfilerType.SampleAverageTimeMs), "Test");
profiler.AddTimeMeasurementMs(200, 2); // 2 samples took 200 ms = avg of 100ms
using (var timer = profiler.CreateTimer()) // We measure 100ms for one sample
{
    await Task.Delay(100);
    timer.NewSample(); // We can take multiple samples within one timer
    await Task.Delay(100);
}
// profiler.GetValue() is now: 100.42275 ms average (4 samples over 400 ms total)
// Result is not exactly 100 ms because Task.Delay has overhead + is not that accurate.
```
## CountAveragePerSecond

Same as SampleAverageTimeMs, except value calculated will be samples per second based on time one sample takes.

```c#
var profiler = new Profiler(new ProfilerOptions(ProfilerType.SampleAveragePerSecond), "Test");
profiler.AddTimeMeasurementMs(200, 2); // 2 samples took 200 ms = avg of 100ms
using (var timer = profiler.CreateTimer()) // We measure 100ms for one sample
{
    await Task.Delay(100);
    timer.NewSample();
    await Task.Delay(100);
}
// profiler.GetValue() is now: 9.995
// Average for one sample is approximately 100ms, which is approximately 10 per second.
```
# Architecture and Mechanics

The profiler architecture is engineered for low latency and high concurrency, strictly utilizing deterministic data structures.

**Implemented Capabilities:**
*   **Memory Efficiency:** Internally utilizes an `ObjectPool<ProfilerTimer>` pattern to reuse instances and mitigate garbage collection pressure during high-frequency profiling.
*   **Concurrency:** Measurement tracking leverages a thread-safe `ConcurrentQueue<TimeMeasurement>` for lock-free historical recording.
*   **Atomic Aggregation:** Scalar values and reference counters are synchronized utilizing `Interlocked` operations to ensure absolute mathematical precision across asynchronous contexts.

## Roadmap / Future Enhancements (Hypotheses)

Future architectural optimizations focus on the overhead associated with the periodic `Cleanup()` execution. While current empirical testing demonstrates theoretical scalability accommodating tens of millions of samples per second, ongoing profiling will ascertain whether batched buffer aggregation could further optimize execution paths. You can execute localized empirical validation utilizing the `OperationsPerSecond` harness within the Example project.