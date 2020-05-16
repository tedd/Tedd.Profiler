using System;
using Tedd.RandomUtils;
using Xunit;

namespace Tedd.ProfilerTests
{
    public class ProfilerDivTest
    {
        private static class StaticCtor
        {
            public static Profiler Profiler = new Profiler(new ProfilerOptions(ProfilerType.Counter));
        }

        [Fact]
        public void StaticCtorTest()
        {
            StaticCtor.Profiler.Set(5);
            Assert.Equal(5, StaticCtor.Profiler.Counter);
            Assert.EndsWith("StaticCtor", StaticCtor.Profiler.Name);
        }

        [Fact]
        public void UnknownProfileTypeTest()
        {
            var profiler = new Profiler(new ProfilerOptions((ProfilerType)12345), nameof(UnknownProfileTypeTest));
            Assert.Throws<Exception>(() => profiler.GetText());
        }
        [Fact]
        public void FormatterTest()
        {
            for (var round = 0; round < 100; round++)
            {
                var profiler = new Profiler(new ProfilerOptions(ProfilerType.SampleAverageTimeMs, 100, 100, "{0:0}"),
                    nameof(FormatterTest));
                Int64 totalCount = 0;
                var avgCount = 0;
                for (var i = 0; i < 100; i++)
                {
                    var r = ConcurrentRandom.NextInt32();
                    var a = ConcurrentRandom.Next(1, 100);
                    totalCount += r;
                    avgCount += a;
                    profiler.AddTimeMeasurement(r, a);

                }

                var avg = Math.Round(((double) totalCount / 10_000D) / avgCount);
                Assert.Equal(avg.ToString(), profiler.GetText());
            }
        }
    }
}