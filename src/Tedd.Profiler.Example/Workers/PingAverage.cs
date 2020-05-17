using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Tedd.ProfilerExample.Workers
{
    public class PingAverage : IWorker
    {
        // Set up a Profiler using default ProfilerGroup
        private static readonly Profiler _profiler = ProfilerGroup.Default.CreateInstanceWithPath(new ProfilerOptions(ProfilerType.SampleAverageTimeMs, 1_000, 10_000));

        private bool _running = false;
        public Task Task { get; private set; }

        public void Start()
        {
            // Set up a task to run
            _running = true;
            Task = Task.Run(RunLoop);
        }

        public void Stop()
        {
            // Signal task to stop at next iteration
            _running = false;
        }

        private void RunLoop()
        {
            // Set up ping
            var ping = new Ping();
            var options = new PingOptions()
            {
                DontFragment = true
            };
            var data = new String('*', 32);
            var buffer = Encoding.ASCII.GetBytes(data);
            var timeout = 120;

            // Do forever until Stop() is called
            while (_running)
            {
                // Send ping
                var reply = ping.Send("www.google.com", timeout, buffer, options);
                if (reply.Status == IPStatus.Success)
                {
                    // // // // // // // // // // // // // //
                    // Add a measurement to our profiler.  //
                    // // // // // // // // // // // // // //
                    _profiler.AddTimeMeasurement(reply.RoundtripTime * 10_000, 1);
                }

                // Throttle so we don't flood target
                Thread.Sleep(500);
            }
        }
    }
}
