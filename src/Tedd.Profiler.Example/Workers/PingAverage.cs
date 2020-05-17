using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Tedd.ProfilerExample.Workers
{
    public class PingAverage: IWorker
    {
        private static readonly Profiler _profiler = ProfilerRoot.Default.CreateInstance(new ProfilerOptions(ProfilerType.SampleAverageTimeMs, 1_000, 10_000));

        private bool _running = false;
        public Task Task { get; private set; }

        public void Start()
        {
            _running = true;
            Task= Task.Run(RunLoop);
        }

        public void Stop()
        {
            _running = false;
        }

        private void RunLoop()
        {
            var ping = new Ping();
            var options = new PingOptions()
            {
                DontFragment = true
            };
            var data = new String('*', 32);
            var buffer = Encoding.ASCII.GetBytes(data);
            var timeout = 120;

            while (_running)
            {
                var reply = ping.Send("www.google.com", timeout, buffer, options);
                if (reply.Status == IPStatus.Success)
                {
                    // Add a measurement
                    _profiler.AddTimeMeasurement(reply.RoundtripTime * 10_000, 1);
                }

                // Lets not send too much ping
                Thread.Sleep(500);
            }
        }

    }
}
