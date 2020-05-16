# Tedd.Profiler
Simple profiling class for measuring count / average of parts of application

# Examples



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

The counter can be increased, decreased or set. You can choose between atomic (thread safe) operation or not. An atomic operation uses Interlocked.Add or Interlocked.Inc to increase counter, whereas non-atomic simply add directly. You must pick the best suited method for your use case.



## SampleAverageTimeMs

Calculating sample average time is done by keeping track of each record added. This means a history of all measurements are kept. When history count or history age is crossed the oldest items are removed from history and subtracted from global numbers.

If you are looking for operations per second then keeping a maximum history of 100 records and 2 000 ms may be enough.

## CountAveragePerSecond

Exactly the same as SampleAverageTimeMs, except value calculated will be samples per second.

# Performance

Implementations are kept simple. Objects are pooled internally for reuse.

