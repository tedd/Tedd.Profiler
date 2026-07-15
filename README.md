# Tedd.Profiler

A robust, high-frequency analytical instrumentation framework engineered to precisely quantify execution parameters within targeted application domains.

Available via NuGet: [https://www.nuget.org/packages/Tedd.Profiler](https://www.nuget.org/packages/Tedd.Profiler)

## Architectural Mechanics

The framework employs advanced concurrency primitives to guarantee structural integrity across multi-threaded operations. Core components utilize `System.Collections.Concurrent.ConcurrentQueue<T>` for deterministic, lock-free time measurement aggregation. Cross-thread state synchronization within `ProfilerGroup` is orchestrated via `System.Threading.ReaderWriterLockSlim`, ensuring isolated read protocols while permitting atomic write access. Measurements demand absolute precision and leverage `Stopwatch.Elapsed.Ticks` to consistently yield 10,000 temporal units per millisecond, bypassing platform-dependent `ElapsedTicks` variances.

## Roadmap Hypotheses
While the core framework is empirically stabilized, we are currently evaluating structural enhancements to augment pedagogical integration. Specifically, hypotheses surrounding **hierarchical data binding** and a **routed event infrastructure** are actively modeled for future architectural iterations. These theoretical constructs are isolated from the current operational API surface.

# Examples

The Tedd.Profiler.Examples project contains examples on how to use profiler. 

For example, averaging ping times:

```csharp
using System;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Tedd.ProfilerExample.Workers;

public class PingAverage : IWorker
{
    // Set up a Profiler using default ProfilerGroup with the absolute path
    private static readonly Profiler _profiler = ProfilerGroup.Default.CreateInstanceWithPath(new ProfilerOptions(ProfilerType.SampleAverageTimeMs, 1_000, 10_000));

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

    private void RunLoop()
    {
        // Set up ping
        using var ping = new Ping();
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
            var reply = ping.Send("www.google.com", timeout, buffer, options);
            if (reply.Status == IPStatus.Success)
            {
                // Add a measurement to our profiler instance
                _profiler.AddTimeMeasurement(reply.RoundtripTime * 10_000, 1);
            }

            // Throttle to mitigate target saturation
            Thread.Sleep(500);
        }
    }
}
```
Creating a Profiler using a ProfilerGroup allows us to pull the key+values from all Profilers in that group.

```csharp
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
| SampleAveragePerSecond| Same as SampleAverageTimeMs, but value is calculated as samples per second. |

## Counter

The counter can be increased, decreased or set. You can choose between atomic (thread-safe) operations or non-atomic operations. An atomic operation utilizes `Interlocked.Add` or `Interlocked.Increment` to increase the counter, whereas a non-atomic operation modifies the state directly. When called consecutively, atomic operations exhibit lower throughput than non-atomic counterparts, though practical results will vary depending on architectural contention. Choose the method mathematically optimal for your execution environment.

```csharp
var profiler = ProfilerGroup.Default.CreateInstance(new ProfilerOptions(ProfilerType.Counter), "Test");
profiler.Inc(5);
profiler.Dec();
profiler.AtomicInc(1);
profiler.AtomicDec(5);
profiler.Set(3);
```
## TimeTotal

Similar to Counter, but targets the tick parameter within `AddTimeMeasurement`. Results are derived from active timers initiated via `CreateTimer()`.

```csharp
var profiler = ProfilerGroup.Default.CreateInstance(new ProfilerOptions(ProfilerType.TimeTotal), "Test");
profiler.AddTimeMeasurementMs(1, 0); // 1 ms
profiler.AddTimeMeasurement(15_000, 0); // 1.5 ms
// profiler.GetValue() returns: 2.5
```
## SampleAverageTimeMs

Calculating sample average time requires maintaining a historical index of each appended record. `ProfilerOptions` specifies parameters for expunging excess or expired entries.

During `Cleanup()`, all excess items are dequeued from the history store and subtracted from global accumulators. `Cleanup()` executes synchronously on every sample addition, as well as during data retrieval via `GetValue()` or `GetText()`.

You may override this by setting `AutoClean = false` in `ProfilerOptions`. Operating in manual mode under high sample volumes requires deterministic, periodic manual execution of `Cleanup()` on the `Profiler` instance. Failure to do so will precipitate an infinite expansion of the sample history queue until interrupted by a `GetValue()` or `GetText()` call.

Tip: For standard operations-per-second tracking, a maximum history of 100 records and a 2,000 ms retention window is typically optimal; extensive ranges offer diminishing returns.

```csharp
var profiler = ProfilerGroup.Default.CreateInstance(new ProfilerOptions(ProfilerType.SampleAverageTimeMs), "Test");
profiler.AddTimeMeasurementMs(200, 2); // 2 samples took 200 ms = avg of 100ms
using (var timer = profiler.CreateTimer()) // We measure 100ms for one sample
{
    Thread.Sleep(100);
    timer.NewSample(); // We can record multiple samples within one timer lifecycle
    Thread.Sleep(100);
}
// profiler.GetValue() approximates: 100.42275 ms average (4 samples over 400 ms total)
// The result deviates slightly from 100 ms due to Thread.Sleep operational overhead and underlying timer resolution.
```
## SampleAveragePerSecond

Functionally equivalent to `SampleAverageTimeMs`, but the calculated metric reflects samples per second based on the temporal cost of a single sample.

```csharp
var profiler = ProfilerGroup.Default.CreateInstance(new ProfilerOptions(ProfilerType.SampleAveragePerSecond), "Test");
profiler.AddTimeMeasurementMs(200, 2); // 2 samples took 200 ms = avg of 100ms
using (var timer = profiler.CreateTimer()) // We measure 100ms for one sample
{
    Thread.Sleep(100);
    timer.NewSample();
    Thread.Sleep(100);
}
// profiler.GetValue() approximates: 9.995
// The average time per sample is ~100ms, equating to ~10 operations per second.
```
# Performance

Implementations are kept simple. Objects are pooled internally for reuse.

The heaviest operation is averaging types, where a ConcurrentQueue keeps track of history. Adding a sample is relatively fast, but running Cleanup() (automatically run by GetValue()/GetText()) takes a bit more resources.

A modern computer should be able to reach excess of 20 million measurements per second on a single core. Even with a history of 20 million entries and cleanup every 50ms it should reach excess of 17 million samples per second. You can test different parameters yourself inside OperationsPerSecond class in the Example project.