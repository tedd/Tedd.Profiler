using Xunit;

namespace Tedd.ProfilerTests
{
    public class ProfilerStaticCtorTest
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
    }
}