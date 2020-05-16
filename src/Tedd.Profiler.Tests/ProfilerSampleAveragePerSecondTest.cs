using System;
using Tedd.RandomUtils;
using Xunit;

namespace Tedd.ProfilerTests
{
    public class ProfilerSampleAveragePerSecondTest
    {
        // Mostly covered in ProfilerSampleAverageTimeMsTest
        [Fact]
        public void StaticTest()
        {
            for (var max = 1; max < 100; max++)
            {
                var profiler = new Profiler(new ProfilerOptions(ProfilerType.SampleAveragePerSecond, max, 1000, null), nameof(StaticTest));
                Int64 totalCount = 0;
                var avgCount = 0;
                for (var i = 0; i < 1000; i++)
                {
                    var r = ConcurrentRandom.NextInt32();
                    var a = ConcurrentRandom.Next(1, 100);
                    profiler.AddTimeMeasurement(r, a);
                }
                for (var i = 0; i < max; i++)
                {
                    var r = ConcurrentRandom.NextInt32();
                    var a = ConcurrentRandom.Next(1, 100);
                    totalCount += r;
                    avgCount += a;
                    profiler.AddTimeMeasurement(r, a);
                }

                var avg = ((double)totalCount / 10_000D) / avgCount;
                avg = 1D / avg;
                Assert.InRange(profiler.GetValue(), avg - 0.1D, avg + 0.1D);
                Assert.Equal(profiler.GetValue().ToString(), profiler.GetText());
            }
        }
    }
}