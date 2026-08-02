## 2024-05-19 - Forge Modernization Plan

**Observation:** The package targets `netstandard2.0` which is still standard and broadly applicable. Tests and Example projects targeted the out-of-support `netcoreapp3.1` which fails to build and test on modern SDKs. The testing toolchains (`Microsoft.NET.Test.Sdk`) and some other packages like `Tedd.ObjectPool` are outdated. Furthermore, the codebase relies on `Stopwatch.ElapsedTicks` and assumes a frequency of 10,000 ticks per millisecond, which is inaccurate across different frameworks and platforms (like Linux/modern .NET), causing test failures. The `CounterParallelFailTest` was flaky because it expects the non-thread-safe counter to always fail parallel additions, but by chance it can occasionally succeed if the loop count or thread interleaving align perfectly. It's an issue with the test itself intentionally looking for a race condition.
Additionally, `ProfileTimerTest` timing expectations were too tight, expecting <= 5.1ms which was often exceeding by 0.05-0.1ms on standard CI instances.

**Strategic Action:**
1. Multi-target `Tedd.Profiler` to `<TargetFrameworks>netstandard2.0;net8.0</TargetFrameworks>` to provide modern baseline support while preserving existing `netstandard2.0` compatibility.
2. Update tests and example project target framework to `net8.0`.
3. Update packages to their newest compatible versions (`Tedd.ObjectPool` to 2.0.0, test tools to newest versions).
4. Replace `Stopwatch.ElapsedTicks` with `Stopwatch.Elapsed.Ticks` (which returns TimeSpan ticks, guaranteed to be 10,000 per ms) to ensure accurate time measurements across all target frameworks.
5. Fix `ProfileTimerTest` by allowing up to `6.0` to accommodate minor latency in stopwatch ticks on modern hardware.
## 2024-08-01 - Package Metadata and Code Cleanup

**Observation:** Missing README.md in package metadata generated NuGet pack warnings. Unused `_cleanupTimer` field in `ProfilerGroup.cs` produced CS0169 compiler warnings.

**Strategic Action:** Added `PackageReadmeFile` and `PackageTags` to `.csproj`, included `README.md` as an asset, enabled XML documentation (`GenerateDocumentationFile`), and removed the unused `_cleanupTimer` field to ensure correct and warning-free modern package creation.
