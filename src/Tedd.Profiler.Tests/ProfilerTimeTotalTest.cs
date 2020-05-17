using System;
using System.Collections.Generic;
using System.Linq;
using Tedd.RandomUtils;
using Xunit;

namespace Tedd.ProfilerTests
{
    public class ProfilerTimeTotalTest
    {
        [Fact]
        public void TotalTest()
        {
            for (var max = 1; max < 100; max++)
            {
                var profiler = new Profiler(new ProfilerOptions(ProfilerType.TimeTotal, max, 1000, null), nameof(TotalTest));
                var queue = new Queue<int>();
                for (var i = 0; i < 1000; i++)
                {
                    var r = ConcurrentRandom.NextInt32();
                    queue.Enqueue(r);
                    profiler.AddTimeMeasurement(r, Math.Abs(ConcurrentRandom.NextInt32()));
                }

                var s = queue.Select(s => (Int64)s).Sum() / 10_000D;
                Assert.Equal(s, profiler.GetValue());
                Assert.Equal(String.Format("{0:0,0.00}", s), profiler.GetText());
            }
        }
    }
}