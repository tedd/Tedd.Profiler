using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace Tedd.Benchmarks
{
    [MemoryDiagnoser]
    public class ProfilerBenchmarks
    {
        private Tedd.Legacy.Profiler _legacyProfiler = null!;
        private Tedd.Profiler _newProfiler = null!;

        [GlobalSetup]
        public void Setup()
        {
            var legacyOptions = new Tedd.Legacy.ProfilerOptions(Tedd.Legacy.ProfilerType.SampleAverageTimeMs);
            _legacyProfiler = new Tedd.Legacy.Profiler(legacyOptions, "LegacyProfiler");

            var newOptions = new Tedd.ProfilerOptions(Tedd.ProfilerType.SampleAverageTimeMs);
            _newProfiler = new Tedd.Profiler(newOptions, "NewProfiler");
        }

        [Benchmark(Baseline = true)]
        public void LegacyAddTimeMeasurementMs()
        {
            _legacyProfiler.AddTimeMeasurementMs(100);
        }

        [Benchmark]
        public void OptimizedAddTimeMeasurementMs()
        {
            _newProfiler.AddTimeMeasurementMs(100);
        }
    }
}
