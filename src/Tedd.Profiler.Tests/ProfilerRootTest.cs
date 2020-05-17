using System;
using Tedd.RandomUtils;
using Xunit;

namespace Tedd.ProfilerTests
{
    public class ProfilerRootTest
    {
        [Fact]
        public void NonStaticNonDescriptiveCtorTest()
        {
            Assert.Throws<Exception>(() => ProfilerRoot.Default.CreateInstance(new ProfilerOptions(ProfilerType.Counter)));
        }

        [Fact]
        public void GetTextTest()
        {
            for (var round = 0; round < 100; round++)
            {
                var count = ConcurrentRandom.Next(1, 100);
                var profilers = new Profiler[count];
                for (var i = 0; i < count; i++)
                {
                    var name = nameof(GetTextTest) + string.Format("{0:0000}", (int)(i / 2));
                    profilers[i] = ProfilerRoot.Default.CreateInstance(new ProfilerOptions(ProfilerType.Counter), name);
                    profilers[i].Set(i + 10);
                };

                var list = ProfilerRoot.Default.GetMeasurements();

                for (var i = 0; i < profilers.Length; i++)
                {
                    var name = nameof(GetTextTest) + string.Format("{0:0000}", (int)(i / 2));
                    Assert.Equal(name, list[i].Key);
                    Assert.Equal((i + 10).ToString(), list[i].Value);
                }

                for (var i = 0; i < profilers.Length; i++)
                {
                    profilers[i].Dispose();
                }
            }
        }
    }
}