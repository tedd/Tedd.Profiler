using System;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tedd;

namespace SnippetTest
{
    public interface IWorker
    {
        void Start();
        void Stop();
        Task Task { get; }
    }

    public class PingAverage : IWorker
    {
        // Set up a Profiler using default ProfilerGroup
        private static readonly Profiler _profiler = ProfilerGroup.Default.CreateInstance(new ProfilerOptions(ProfilerType.SampleAverageTimeMs, 1_000, 10_000), "Ping");

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

        private async Task RunLoop()
        {
            // Set up ping
            var ping = new Ping();
            var options = new PingOptions
            {
                DontFragment = true
            };
            var data = new string('*', 32);
            var buffer = Encoding.ASCII.GetBytes(data);
            var timeout = 120;

            // Do forever until Stop() is called
            while (_running)
            {
                // Send ping
                var reply = await ping.SendPingAsync("www.google.com", timeout, buffer, options);
                if (reply.Status == IPStatus.Success)
                {
                    // // // // // // // // // // // // // //
                    // Add a measurement to our profiler.  //
                    // // // // // // // // // // // // // //
                    _profiler.AddTimeMeasurement(reply.RoundtripTime * 10_000, 1);
                }

                // Throttle so we don't flood target
                await Task.Delay(500);
            }
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var p = new PingAverage();
        }
    }
}
