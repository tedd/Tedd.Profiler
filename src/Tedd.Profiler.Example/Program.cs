using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tedd.ProfilerExample.Workers;

namespace Tedd.ProfilerExample
{
    class Program
    {
        static void Main(string[] args)
        {
            // Create a list of workers
            var workers = new List<IWorker>()
            {
                new PingAverage(),
                new OperationsPerSecond(),
                new AtomicCounter(),
                new Counter()
            };

            // Start all workers
            foreach (var worker in workers)
                worker.Start();

            // Keep updating status on screen until user presses Q
            Console.Clear();
            for (;;)
            {
                Console.SetCursorPosition(0,0);
                
                Console.WriteLine("Press Q to exit.");
                Console.WriteLine("");

                // Print result
                foreach (var kv in ProfilerRoot.Default.GetMeasurements())
                {
                    var key = kv.Key.PadRight(50, ' ') + ": ";
                    Console.WriteLine(key + kv.Value.PadLeft(15, ' '));
                }

                // Update screen only every so often
                Thread.Sleep(100);

                // Check for Q keypress
                if (Console.KeyAvailable && Console.ReadKey().KeyChar.ToString().ToUpper() == "Q")
                    break;
            }

            // Stop all workers
            foreach (var worker in workers)
                worker.Stop();

            // Wait for all to finish
            Task.WaitAll(workers.Select(s => s.Task).ToArray());
        }
    }
}
