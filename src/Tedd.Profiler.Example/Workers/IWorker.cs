using System.Threading.Tasks;

namespace Tedd.ProfilerExample.Workers
{
    public interface IWorker
    {
        void Start();
        void Stop();
        Task Task { get; }
    }
}