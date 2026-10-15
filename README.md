# Tedd.Profiler
Simple profiler for measuring parts of an application.

Available on NuGet: [https://www.nuget.org/packages/Tedd.Profiler](https://www.nuget.org/packages/Tedd.Profiler)

## Architectural Execution Flow

### Implemented Capabilities (Architectural Facts)
- **High-Performance Concurrent Aggregation**: Measurements are safely aggregated across multiple threads utilizing `Interlocked` operations and lock-free constructs (`ConcurrentQueue<T>`).
- **Memory-Optimized Instantiation**: Reduces garbage collection overhead by leveraging object pooling for `ProfilerTimer` instances.
- **Centralized Grouping Infrastructure**: Profilers are tracked within a `ProfilerGroup`, utilizing `ReaderWriterLockSlim` to ensure thread-safe enumeration without impeding high-throughput profiling operations.
- **Microsecond Precision Scaling**: Enforces strict utilization of `Stopwatch.Elapsed.Ticks` to guarantee a uniform 10,000 ticks per millisecond scaling across disparate platform hardware frequencies.

### Roadmap (Hypotheses)
- **Hierarchical Data Binding**: Anticipated integration of real-time data binding to telemetry dashboards for live structural monitoring.
- **Routed Event Infrastructure**: Future conceptualization of a routed event system capable of propagating profiling threshold alerts across distributed application boundaries.
*(Note: These components represent planned future enhancements and are not functionally integrated into the current API surface).*

## Examples

The `Tedd.Profiler.Example` project contains examples demonstrating profiler utilization.

For instance, averaging ping times:

```csharp
public class PingAverage : IWorker
{
    // Set up a Profiler utilizing the default ProfilerGroup.
    // Note: CreateInstanceWithPath automatically derives the profiler name from the calling context.
    private static readonly Profiler _profiler = ProfilerGroup.Default.CreateInstanceWithPath(
        new ProfilerOptions(ProfilerType.SampleAverageTimeMs, 1_000, 10_000));

    private bool _running = false;
    public Task? Task { get; private set; }

    public void Start()
    {
        // Initialize worker task execution
        _running = true;
        Task = System.Threading.Tasks.Task.Run(RunLoop);
    }

    public void Stop()
    {
        // Signal task termination
        _running = false;
    }

    private void RunLoop()
    {
        // Configure network ping parameters
        using var ping = new Ping();
        var options = new PingOptions { DontFragment = true };
        var data = new string('*', 32);
        var buffer = Encoding.ASCII.GetBytes(data);
        const int timeout = 120;

        // Execute continuous operational loop
        while (_running)
        {
            var reply = ping.Send("www.google.com", timeout, buffer, options);
            if (reply.Status == IPStatus.Success)
            {
                // Integrate measurement into the centralized profiler
                _profiler.AddTimeMeasurement(reply.RoundtripTime * 10_000, 1);
            }

            // Impose operational throttle
            Thread.Sleep(500);
        }
    }
}
```

Creating a `Profiler` using a `ProfilerGroup` permits extraction of key-value pairs from all aggregated profilers within that specific group.

```csharp
// Output aggregated profiling results
foreach (var (key, value) in ProfilerGroup.Default.GetMeasurements())
{
    var formattedKey = key.PadRight(50, ' ') + ": ";
    Console.WriteLine(formattedKey + value.PadLeft(15, ' '));
}
```

## Naming Convention

During `Profiler` instantiation, an arbitrary nomenclature may be applied. Multiple profiler instances can share an identical name.

When invoking `ProfilerGroup.CreateInstanceWithPath(options, name)`, the fully qualified class name precedes the specified name (e.g., `MyApp.SomeNamespace.ClassName:method`). It is recommended to prefix the provided name parameter with a delimiter, such as `":"` or `"."`.

## Operational Modes

Each `Profiler` instance operates in one of the following configurations, defined by `ProfilerType`:

| ProfilerType          | Description                                                                  |
| --------------------- | ---------------------------------------------------------------------------- |
| Counter               | Synchronized (or non-synchronized) discrete integer tracking                 |
| Text                  | Explicit string representation                                               |
| TimeTotal             | Thread-synchronized cumulative time aggregation                              |
| SampleAverageTimeMs   | Thread-synchronized temporal average derived from cumulative sample count    |
| SampleAveragePerSecond| Analagous to `SampleAverageTimeMs`, representing calculated samples per second|

### Counter

The counter can be increased, decreased, or explicitly set. Atomic operations leverage `Interlocked.Add` or `Interlocked.Increment` for thread-safe execution, while non-atomic operations manipulate the state directly. Atomic operations impose approximately a 2x performance reduction; select the optimal methodology based on concurrency requirements.

```csharp
var profiler = new Profiler(new ProfilerOptions(ProfilerType.Counter), "TestCounter");
profiler.Inc(5);
profiler.Dec();
profiler.AtomicInc(1);
profiler.AtomicDec(5);
profiler.Set(3);
```

### TimeTotal

Functions similarly to `Counter`, but aggregates the underlying tick data via `AddSample`. It optimally accepts data generated by `CreateTimer()`.

```csharp
var profiler = new Profiler(new ProfilerOptions(ProfilerType.TimeTotal), "TestTimeTotal");
profiler.AddTimeMeasurementMs(1); // 1 ms
profiler.AddTimeMeasurement(15_000, 1); // 1.5 ms
// profiler.GetValue() yields 2.5
```

### SampleAverageTimeMs

Maintains an empirical history of captured samples to compute average execution time. `ProfilerOptions` exposes parameters for managing excess or expired historical items.

During `Cleanup()`, expired items are dequeued and subtracted from global aggregates. `Cleanup()` executes on every sample insertion and upon data extraction (`GetValue()` or `GetText()`).

Manual cleanup can be configured by setting `AutoClean = false` in `ProfilerOptions`. If automated cleanup is disabled under high-throughput scenarios, `Cleanup()` must be invoked periodically to prevent unbounded memory allocation.

*Optimization Note*: For calculating operations per second, a maximum history of 100 records and 2,000 ms is typically sufficient, eliminating the overhead of retaining expansive temporal ranges.

```csharp
var profiler = new Profiler(new ProfilerOptions(ProfilerType.SampleAverageTimeMs), "TestAvgTime");
profiler.AddTimeMeasurementMs(200, 2); // 2 samples executed in 200 ms (100ms average)
using (var timer = profiler.CreateTimer())
{
    Thread.Sleep(100);
    timer.NewSample(); // Supports multiple sample registrations within a single timer lifecycle
    Thread.Sleep(100);
}
// profiler.GetValue() yields approximately 100.0 ms (4 samples aggregating 400 ms)
// Variance occurs due to OS-level Thread.Sleep scheduling overhead.
```

### SampleAveragePerSecond

Computes the frequency of operations per second derived from the average temporal duration per sample.

```csharp
var profiler = new Profiler(new ProfilerOptions(ProfilerType.SampleAveragePerSecond), "TestAvgFreq");
profiler.AddTimeMeasurementMs(200, 2); // 2 samples executed in 200 ms (100ms average)
using (var timer = profiler.CreateTimer())
{
    Thread.Sleep(100);
    timer.NewSample();
    Thread.Sleep(100);
}
// profiler.GetValue() yields approximately 10.0
// An average sample duration of 100ms equates to 10 operations per second.
```

## Performance Profile

The architectural design prioritizes computational minimalism and deterministic execution. Internal object pooling guarantees optimized memory reuse.

The most computationally intensive mechanism is the averaging algorithm, wherein a `ConcurrentQueue<T>` manages historical states. While sample insertion operates at high velocity, `Cleanup()` execution demands marginal auxiliary processing.

Empirical benchmarks indicate a modern architecture can achieve in excess of 20 million measurements per second on a single execution thread. Even maintaining a historical queue of 20 million records with a 50ms cleanup cycle, throughput consistently exceeds 17 million samples per second. Experimental validation can be conducted utilizing the `OperationsPerSecond` infrastructure within the `Tedd.Profiler.Example` project.
