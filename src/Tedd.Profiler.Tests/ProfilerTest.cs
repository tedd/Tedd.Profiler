using System;
using Xunit;

namespace Tedd.ProfilerTests
{
    public class ProfilerTest
    {
        [Fact]
        public void NonStaticNonDescriptiveCtor()
        {
            Assert.Throws<Exception>(() => new Profiler(new ProfilerOptions(ProfilerType.Counter)));
        }
       
        [Fact]
        public void CounterTest()
        {
            var profiler = new Profiler(new ProfilerOptions(ProfilerType.Counter), "ProfilerTest");
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

        }
    }
}
