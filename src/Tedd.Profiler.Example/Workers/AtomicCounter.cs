using System.Threading.Tasks;

namespace Tedd.ProfilerExample.Workers
{
    public class AtomicCounter: IWorker
    {
        private static readonly Profiler _profiler = ProfilerRoot.Default.CreateInstance(new ProfilerOptions(ProfilerType.Counter, 100, 200, "{0:0,0}"));

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
                _profiler.AtomicInc();
            }
        }

    }
}