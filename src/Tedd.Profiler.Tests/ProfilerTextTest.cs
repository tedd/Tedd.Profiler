using Tedd.RandomUtils;
using Xunit;

namespace Tedd.ProfilerTests
{
    public class ProfilerTextTest
    {
        [Fact]
        public void SetTextTest()
        {
            var profiler = new Profiler(new ProfilerOptions(ProfilerType.Text), nameof(SetTextTest));
            Assert.Null(profiler.Text);
            for (var r = 0; r < 100; r++)
            {
                var text = ConcurrentRandom.NextString("abcdefghijklmnopqrstuvwxyz0123456789", 10);
                profiler.Set(text);
                Assert.Equal(text, profiler.Text);
                Assert.Equal(text, profiler.GetText());
            }
        }
    }
}