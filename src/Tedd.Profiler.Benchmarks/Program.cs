using BenchmarkDotNet.Running;

namespace Tedd.Benchmarks
{
    public class Program
    {
        public static void Main(string[] args)
        {
            BenchmarkRunner.Run<ProfilerBenchmarks>();
        }
    }
}
