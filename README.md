# Tedd.Profiler
Simple profiler for measuring parts of application.

Available on NuGet: [https://www.nuget.org/packages/Tedd.Profiler](https://www.nuget.org/packages/Tedd.Profiler)

## Architectural Paradigms

Tedd.Profiler implements a highly deterministic, decentralized measurement generation model with centralized aggregation. **The framework explicitly does not utilize hierarchical data binding or routed event infrastructure.**

Instead, the library relies on a deterministic pull-based aggregation model via the `ProfilerGroup`. Profilers are individually instantiated and act as independent data collection nodes. At the point of aggregation, a central instance—such as `ProfilerGroup.Default`—iterates its locally managed collections, aggregating the synchronized state of every active profiler, effectively mitigating overhead from event bubbling or complex binding contexts.

### Architectural Execution Flow

1.  **Instantiation:** Profilers are instantiated either standalone or associated with a `ProfilerGroup`.
2.  **Collection:** Active code components invoke lightweight, deterministic measurement functions (e.g., `Inc`, `AtomicInc`, `AddTimeMeasurementMs`, `CreateTimer`). History for average types is tracked using thread-safe, concurrent data structures (e.g., `ConcurrentQueue<T>`).
3.  **Aggregation:** Metrics are queried via `ProfilerGroup.GetMeasurements()`, extracting current, atomic states from the aggregated collection nodes in a strict pull mechanism.
4.  **Cleanup:** Memory is systematically managed. Setting `AutoClean` purges expired elements algorithmically within operations or when invoking `Cleanup()`.

## Examples

The Tedd.Profiler.Examples project contains examples on how to use the profiler.

For example, averaging ping times:

```csharp
public class PingAverage : IWorker
{
    // Set up a Profiler using default ProfilerGroup
    private static readonly Profiler _profiler = ProfilerGroup.Default.CreateInstance(new ProfilerOptions(ProfilerType.SampleAverageTimeMs, 1_000, 10_000), "PingAvg");

    private bool _running = false;
    public Task Task { get; private set; }

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
                // // // // // // // // // // // // // //
                // Add a measurement to our profiler.  //
                // // // // // // // // // // // // // //
                _profiler.AddTimeMeasurementMs(reply.RoundtripTime, 1);
            }

            // Throttle so we don't flood target
            Thread.Sleep(500);
        }
    }
}
```

Creating a Profiler using a `ProfilerGroup` allows us to pull the key+values from all Profilers in that group.

```csharp
// Print result
foreach (var kv in ProfilerGroup.Default.GetMeasurements())
{
    var key = kv.Key.PadRight(50, ' ') + ": ";
    Console.WriteLine(key + kv.Value.PadLeft(15, ' '));
}
```

## Name

When creating a Profiler you can choose any name. Multiple instances of the profiler can have the same name.

If using `ProfilerGroup.CreateInstanceWithPath(options, name)`, the full name of the class will precede the name, for example `MyApp.SomeNamespace.ClassName`. You should then provide a name with a separator, such as `":method"` or `".method"`.

## Different modes

Each instance of the profiler can function in different modes.

| ProfilerType             | Description                                                                 |
| ------------------------ | --------------------------------------------------------------------------- |
| Counter                  | Thread synchronized (or not synchronized) integer                           |
| Text                     | Text                                                                        |
| TimeTotal                | Thread synchronized cumulative time                                         |
| SampleAverageTimeMs      | Thread synchronized average of cumulative time based on sample count        |
| SampleAveragePerSecond   | Same as SampleAverageTimeMs, but value is calculated as samples per second. |

### Counter

The counter can be increased, decreased or set. You can choose between atomic (thread-safe) operation or not. An atomic operation uses `Interlocked.Add` or `Interlocked.Increment` to increase the counter, whereas non-atomic simply adds directly. Called directly the atomic is around 2x slower than non-atomic, but your actual result may vary. You must pick the best suited method for your use case.

```csharp
var profiler = new Profiler(new ProfilerOptions(ProfilerType.Counter), "Test");
profiler.Inc(5);
profiler.Dec();
profiler.AtomicInc();
profiler.AtomicDec(5);
profiler.Set(3);
```

### TimeTotal

Similar to Counter, but uses the ticks-part of AddSample. It will get results from the use of a timer (with `CreateTimer()`).

```csharp
var profiler = new Profiler(new ProfilerOptions(ProfilerType.TimeTotal), "Test");
profiler.AddTimeMeasurementMs(1, 0); // 1 ms
profiler.AddTimeMeasurementMs(1, 0); // another 1 ms
// profiler.GetValue() is now: 2
```

### SampleAverageTimeMs

Calculating sample average time is done by keeping history of each record added. `ProfilerOptions` has parameters for cleaning up excess or expired items.

During `Cleanup()`, all excess items are removed from history and subtracted from global numbers. `Cleanup()` is run on every sample, as well as when you pull numbers (`GetValue()` or `GetText()`).

You can set it to manual by setting `AutoClean = false` on `ProfilerOptions`. If you do that and you sample a lot of samples, then it is important to run `Cleanup()` manually on the Profiler instance regularly, or sample history will fill up infinitely until cleaned by `GetValue()` or `GetText()`.

Tip: If you are looking for operations per second then keeping a maximum history of 100 records and 2,000 ms may be enough; there is usually no need for very large ranges.

```csharp
var profiler = new Profiler(new ProfilerOptions(ProfilerType.SampleAverageTimeMs), "Test");
profiler.AddTimeMeasurementMs(200, 2); // 2 samples took 200 ms = avg of 100ms
using (var timer = profiler.CreateTimer()) // We measure 100ms for one sample
{
    Thread.Sleep(100);
    timer.NewSample(); // We can take multiple samples within one timer
    Thread.Sleep(100);
}
// profiler.GetValue() is now: ~100 ms average (3 samples over ~300 ms total)
// Result is not exactly 100 ms because Thread.Sleep has overhead + is not that accurate.
```

### SampleAveragePerSecond

Same as `SampleAverageTimeMs`, except the value calculated will be samples per second based on the time one sample takes.

```csharp
var profiler = new Profiler(new ProfilerOptions(ProfilerType.SampleAveragePerSecond), "Test");
profiler.AddTimeMeasurementMs(200, 2); // 2 samples took 200 ms = avg of 100ms
using (var timer = profiler.CreateTimer()) // We measure 100ms for one sample
{
    Thread.Sleep(100);
    timer.NewSample();
    Thread.Sleep(100);
}
// profiler.GetValue() is now: ~10
// Average for one sample is approximately 100ms, which is approximately 10 per second.
```

## Performance

Implementations are kept simple. Objects are pooled internally for reuse.

The heaviest operation is averaging types, where a `ConcurrentQueue` keeps track of history. Adding a sample is relatively fast, but running `Cleanup()` (automatically run by `GetValue()` / `GetText()`) takes a bit more resources.

A modern computer should be able to reach excess of 20 million measurements per second on a single core. Even with a history of 20 million entries and cleanup every 50ms it should reach excess of 17 million samples per second. You can test different parameters yourself inside the `OperationsPerSecond` class in the Example project.
