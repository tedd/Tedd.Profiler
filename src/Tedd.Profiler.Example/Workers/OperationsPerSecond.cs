using System;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tedd.RandomUtils;

namespace Tedd.ProfilerExample.Workers
{
    public class OperationsPerSecond: IWorker
    {
        private static readonly Profiler _profiler = ProfilerGroup.Default.CreateInstanceWithPath(new ProfilerOptions(ProfilerType.SampleAveragePerSecond, 20_00_000, 10_000));

        private bool _running = false;
        public Task Task { get; private set; }

        public void Start()
        {
            _running = true;
            Task = Task.Run(RunLoop);
        }

        public void Stop()
        {
            _running = false;
        }

        private void RunLoop()
        {

            while (_running)
            {
                // Using is scoped
                using (var timer = _profiler.CreateTimer())
                {
                    // Do some heavy stuff
                    //Thread.Sleep(ConcurrentRandom.Next(2, 100));
                }

                // Is the same as
                {
                    using var timer = _profiler.CreateTimer();
                    // Do some heavy stuff
                    //Thread.Sleep(ConcurrentRandom.Next(2, 100));
                }

            }
        }

    }
}