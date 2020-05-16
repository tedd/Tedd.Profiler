using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Tedd
{
    public class ProfilerTimer : IDisposable
    {
        private Profiler _profiler;
        internal Stopwatch Stopwatch = new Stopwatch();

        /// <summary>
        /// Number of samples this measurement is for.
        /// </summary>
        public int SampleCount;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ProfilerTimer Start(Profiler profiler)
        {
            _profiler = profiler;
            SampleCount = 1;
            Stopwatch.Restart();
            return this;
        }

        /// <summary>
        /// Increases SampleCount. Average time = total time is divided by number of samples.
        /// Hint: Use for inside a loop if you want to measure each item there.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void NewSample()
        {
            SampleCount++;
        }

        internal void Stop()
        {
            Stopwatch.Stop();
            _profiler.AddTimeMeasurement(Stopwatch.ElapsedTicks, SampleCount);
            _profiler?.FinishTimer(this);
        }

        public void Dispose()
        {
            if (!Stopwatch.IsRunning)
                throw new Exception($"Double disposed object. {nameof(ProfilerTimer)} objects should only be disposed once. Upon dispose they are pooled internally for reuse.");
            Stop();
        }
    }
}