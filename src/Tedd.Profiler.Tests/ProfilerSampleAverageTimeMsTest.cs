using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Tedd.RandomUtils;
using Xunit;

namespace Tedd.ProfilerTests
{
    public class ProfilerSampleAverageTimeMsTest
    {


        [Fact]
        public void MaxHistoryTest()
        {
            for (var max = 1; max < 100; max++)
            {
                var profiler = new Profiler(new ProfilerOptions(ProfilerType.SampleAverageTimeMs, max, 1000, null), nameof(MaxHistoryTest));
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
                Assert.InRange(profiler.GetValue(), avg - 0.1D, avg + 0.1D);
                Assert.Equal(profiler.GetValue().ToString(), profiler.GetText());
            }
        }

        [Fact]
        public void TimeExpireTest()
        {
            for (var max = 100; max < 200; max++)
            {
                var profiler = new Profiler(new ProfilerOptions(ProfilerType.SampleAverageTimeMs, max, 10, null), nameof(TimeExpireTest));
                Int64 totalCount = 0;
                var avgCount = 0;
                for (var i = 0; i < 100; i++)
                {
                    var r = ConcurrentRandom.NextInt32();
                    var a = ConcurrentRandom.Next(1, 100);
                    profiler.AddTimeMeasurement(r, a);
                }
                Thread.Sleep(10);
                for (var i = 0; i < max - 100; i++)
                {
                    var r = ConcurrentRandom.NextInt32();
                    var a = ConcurrentRandom.Next(1, 100);
                    totalCount += r;
                    avgCount += a;
                    profiler.AddTimeMeasurement(r, a);
                }

                if (avgCount != 0 && totalCount != 0)
                {
                    var avg = ((double)totalCount / 10_000D) / avgCount;
                    Assert.InRange(profiler.GetValue(), avg - 0.1D, avg + 0.1D);
                }

                Assert.Equal(profiler.GetValue().ToString(), profiler.GetText());

                Assert.Throws<ArgumentOutOfRangeException>(() => profiler.AddTimeMeasurement(1, 0));
                Assert.Throws<ArgumentOutOfRangeException>(() => profiler.AddTimeMeasurement(1, -1));
            }
        }


        [Fact]
        public void ProfileTimerTest()
        {
            var profiler = new Profiler(new ProfilerOptions(ProfilerType.SampleAverageTimeMs, 1000, 10000), nameof(ProfileTimerTest));
            var sw = new Stopwatch();
            for (var r = 0; r < 100; r++)
            {
                sw.Restart();
                using var timer = profiler.CreateTimer();
                while (sw.ElapsedMilliseconds < 10) { }
                timer.NewSample();
            }
            Assert.InRange(profiler.GetValue(), 4, 5.1);
        }
        [Fact]
        public void ProfileTimerDoubleDisposeTest()
        {
            var profiler = new Profiler(new ProfilerOptions(ProfilerType.SampleAverageTimeMs, 1000, 10000), nameof(ProfileTimerTest));
            Assert.Throws<Exception>(() =>
            {
                using (var timer = profiler.CreateTimer())
                {
                    timer.Dispose();
                }
            });
        }
    }
}