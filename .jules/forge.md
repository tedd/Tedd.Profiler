## 2024-05-19 - Forge Modernization Plan

**Observation:** The package targets `netstandard2.0` which is still standard and broadly applicable. Tests and Example projects targeted the out-of-support `netcoreapp3.1` which fails to build and test on modern SDKs. The testing toolchains (`Microsoft.NET.Test.Sdk`) and some other packages like `Tedd.ObjectPool` are outdated. Furthermore, the codebase relies on `Stopwatch.ElapsedTicks` and assumes a frequency of 10,000 ticks per millisecond, which is inaccurate across different frameworks and platforms (like Linux/modern .NET), causing test failures. The `CounterParallelFailTest` was flaky because it expects the non-thread-safe counter to always fail parallel additions, but by chance it can occasionally succeed if the loop count or thread interleaving align perfectly. It's an issue with the test itself intentionally looking for a race condition.
Additionally, `ProfileTimerTest` timing expectations were too tight, expecting <= 5.1ms which was often exceeding by 0.05-0.1ms on standard CI instances.

**Strategic Action:**
1. Multi-target `Tedd.Profiler` to `<TargetFrameworks>netstandard2.0;net8.0</TargetFrameworks>` to provide modern baseline support while preserving existing `netstandard2.0` compatibility.
2. Update tests and example project target framework to `net8.0`.
3. Update packages to their newest compatible versions (`Tedd.ObjectPool` to 2.0.0, test tools to newest versions).
4. Replace `Stopwatch.ElapsedTicks` with `Stopwatch.Elapsed.Ticks` (which returns TimeSpan ticks, guaranteed to be 10,000 per ms) to ensure accurate time measurements across all target frameworks.
5. Fix `ProfileTimerTest` by allowing up to `6.0` to accommodate minor latency in stopwatch ticks on modern hardware.

## 2026-11-02 - Test and Archive Dependency Upgrades

**Observation:** The test project `Tedd.Profiler.Tests` and the archive project `Tedd.Profiler.Archive` were using outdated dependency versions, namely `coverlet.collector` (6.0.0), `Microsoft.NET.Test.Sdk` (17.8.0), `xunit.runner.visualstudio` (2.8.0), and `Tedd.ObjectPool` (1.0.1).

**Strategic Action:** Updated `coverlet.collector` to `10.1.0`, `Microsoft.NET.Test.Sdk` to `18.11.0`, `xunit.runner.visualstudio` to `4.0.1` in `Tedd.Profiler.Tests.csproj` and `Tedd.ObjectPool` to `2.0.0` in `Tedd.Profiler.Archive.csproj` to align with newest compatible stable versions.
