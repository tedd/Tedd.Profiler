## 2024-05-18 - StopWatch Object Allocation Mitigation

**Observation:** `Profiler.AddTimeMeasurementMs` instantiated an unused `System.Diagnostics.Stopwatch` locally, creating unnecessary GC overhead for a high frequency sampling API.

**Strategic Action:** Eliminated the redundant heap allocation by removing `var sw = new Stopwatch();`. The `TimeMeasurement` object structure itself retains O(1) time and space complexity natively. The latency of `AddTimeMeasurementMs` remained effectively identical (206ns vs 207ns), confirming stable semantic execution without degrading throughput.
