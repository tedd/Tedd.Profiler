using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Tedd.ProfilerTests
{
    public class ProfilerDivTest
    {
        [Fact]
        public void UnknownProfileType()
        {
            var profiler = new Profiler(new ProfilerOptions((ProfilerType)12345), nameof(UnknownProfileType));
            Assert.Throws<Exception>(() => profiler.GetText());
        }
    }

    public class ProfilerCounterTest
    {
        [Fact]
        public void NonStaticNonDescriptiveCtorTest()
        {
            Assert.Throws<Exception>(() => new Profiler(new ProfilerOptions(ProfilerType.Counter)));
        }

        [Fact]
        public void CounterTest()
        {
            var profiler = new Profiler(new ProfilerOptions(ProfilerType.Counter), nameof(CounterTest));
            Assert.Equal(0, profiler.Counter);
            profiler.Inc();
            Assert.Equal(1, profiler.Counter);
            Assert.Equal(1, profiler.GetValue());
            profiler.Inc(10);
            Assert.Equal(11, profiler.Counter);
            Assert.Equal(11, profiler.GetValue());
            profiler.Dec();
            Assert.Equal(10, profiler.Counter);
            Assert.Equal(10, profiler.GetValue());
            profiler.Dec(5);
            Assert.Equal(5, profiler.Counter);
            Assert.Equal(5, profiler.GetValue());

            profiler.Set(3);
            Assert.Equal(3, profiler.Counter);
            profiler.Set(0);
            Assert.Equal(0, profiler.Counter);
            profiler.AtomicInc();
            Assert.Equal(1, profiler.Counter);
            profiler.AtomicInc(10);
            Assert.Equal(11, profiler.Counter);
            profiler.AtomicDec();
            Assert.Equal(10, profiler.Counter);
            profiler.AtomicDec(5);
            Assert.Equal(5, profiler.Counter);
            Assert.Equal(5, profiler.GetValue());

        }

        [Fact]
        public void CounterParallelTest()
        {
            var profiler = new Profiler(new ProfilerOptions(ProfilerType.Counter), nameof(CounterParallelTest));
            Int64 sum = 0;
            Task.WaitAll(new[]
                {
                Task.Run(() => AtomicAddLoop(profiler, ref sum)),
                Task.Run(() => AtomicAddLoop(profiler, ref sum)),
                Task.Run(() => AtomicAddLoop(profiler, ref sum)),
                Task.Run(() => AtomicAddLoop(profiler, ref sum)),
                Task.Run(() => AtomicAddLoop(profiler, ref sum)),
                Task.Run(() => AtomicAddLoop(profiler, ref sum)),
                Task.Run(() => AtomicAddLoop(profiler, ref sum)),
                Task.Run(() => AtomicAddLoop(profiler, ref sum)),
                Task.Run(() => AtomicAddLoop(profiler, ref sum)),
                Task.Run(() => AtomicAddLoop(profiler, ref sum))
                }
            );
            Assert.Equal( sum, profiler.Counter);
        }

        private void AtomicAddLoop(Profiler profiler, ref Int64 sum)
        {
            var s = 0;
            var rnd = new Random();
            for (var i = 0; i < 1_000_000; i++)
            {
                var r = rnd.NextInt16();
                s += r;
                profiler.AtomicInc(r);
            }

            Interlocked.Add(ref sum, s);
        }

        [Fact]
        public void CounterParallelFailTest()
        {
            var profiler = new Profiler(new ProfilerOptions(ProfilerType.Counter), nameof(CounterParallelFailTest));
            Int64 sum = 0;
            Task.WaitAll(new[]
                {
                    Task.Run(() => AddLoop(profiler, ref sum)),
                    Task.Run(() => AddLoop(profiler, ref sum)),
                    Task.Run(() => AddLoop(profiler, ref sum)),
                    Task.Run(() => AddLoop(profiler, ref sum)),
                    Task.Run(() => AddLoop(profiler, ref sum)),
                    Task.Run(() => AddLoop(profiler, ref sum)),
                    Task.Run(() => AddLoop(profiler, ref sum)),
                    Task.Run(() => AddLoop(profiler, ref sum)),
                    Task.Run(() => AddLoop(profiler, ref sum)),
                    Task.Run(() => AddLoop(profiler, ref sum))
                }
            );
             Assert.NotEqual(sum, profiler.Counter);
        }

        private void AddLoop(Profiler profiler, ref Int64 sum)
        {
            var s = 0;
            var rnd = new Random();
            for (var i = 0; i < 1_000_000; i++)
            {
                var r = rnd.NextInt16();
                s += r;
                profiler.Inc(r);
            }

            Interlocked.Add(ref sum, s);
        }


    }
}
