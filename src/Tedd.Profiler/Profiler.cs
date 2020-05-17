using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Mime;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

namespace Tedd
{
    public class Profiler : IDisposable
    {
        private static readonly ObjectPool<ProfilerTimer> ProfileTimerPool = new ObjectPool<ProfilerTimer>(() => new ProfilerTimer(), 20);
        private readonly ConcurrentQueue<TimeMeasurement> _timeMeasurements = new ConcurrentQueue<TimeMeasurement>();
        private int _sampleCount = 0;
        private Int64 _sampleTotalTime = 0;
        private ProfilerGroup _profilerGroup;
        public readonly ProfilerOptions Options;
        public readonly string Name;
        private Int64 _counter;
        public Int64 Counter { get => _counter; }
        private string _text;

        public string Text { get => _text; }

        private readonly Stopwatch _stopwatch = new Stopwatch();

        //[CallerMemberName] string name = ""
        public Profiler(ProfilerOptions options, string name)
        {
            Options = options;
            Name = name;
            _stopwatch.Start();
        }

        internal Profiler(ProfilerGroup profilerGroup, ProfilerOptions options, string name)
        {
            _profilerGroup = profilerGroup;
            Options = options;
            Name = name;
            _stopwatch.Start();
        }


        /// <summary>
        /// Increase Counter by locking.
        /// Hint: For use in multi-threading scenarios where accuracy is important. Costs 10x or more performance compared to Inc(). 
        /// </summary>
        /// <returns>New value of Counter</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Int64 AtomicInc() => Interlocked.Increment(ref _counter);
        /// <summary>
        /// Increase Counter by locking.
        /// Hint: For use in multi-threading scenarios where accuracy is important. Costs 10x or more performance compared to Inc(). 
        /// </summary>
        /// <returns>New value of Counter</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Int64 AtomicInc(int i) => Interlocked.Add(ref _counter, i);
        /// <summary>
        /// Decrease Counter by locking.
        /// Hint: For use in multi-threading scenarios where accuracy is important. Costs 10x or more performance compared to Dec(). 
        /// </summary>
        /// <returns>New value of Counter</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Int64 AtomicDec() => Interlocked.Decrement(ref _counter);
        /// <summary>
        /// Decrease Counter by locking.
        /// Hint: For use in multi-threading scenarios where accuracy is important. Costs 10x or more performance compared to Dec(). 
        /// </summary>
        /// <returns>New value of Counter</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Int64 AtomicDec(int i) => Interlocked.Add(ref _counter, -i);

        /// <summary>
        /// Increase Counter.
        /// </summary>
        /// <returns>New value of Counter</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Inc(int i = 1) => _counter += i;
        /// <summary>
        /// Decrease Counter.
        /// </summary>
        /// <returns>New value of Counter</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dec(int i = 1) => _counter -= i;

        /// <summary>
        /// Sets Counter to number.
        /// </summary>
        /// <returns>New value of Counter</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set(int i) => _counter = i;

        /// <summary>
        /// Sets text.
        /// </summary>
        /// <returns>New value of text</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set(string text) => _text = text;

        /// <summary>
        /// Return a ProfileTimer object that allows
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ProfilerTimer CreateTimer() => ProfileTimerPool.Allocate().Start(this);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void FinishTimer(ProfilerTimer profilerInstance)
        {
            ProfileTimerPool.Free(profilerInstance);
        }

        /// <summary>
        /// Add time measurements in milliseconds.
        /// Hint: Stopwatch is a good source of high frequency timer, and it returns milliseconds.
        /// </summary>
        /// <param name="ms">Number of milliseconds</param>
        /// <param name="sampleCount">Number of samples this time measurement is for (used for average calculation)</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddTimeMeasurementMs(Int64 ms, int sampleCount = 1)
        {
            var sw = new Stopwatch();
            AddTimeMeasurement((Int64)(ms * 10_000), sampleCount);
        }

        /// <summary>
        /// Add time measurements in ticks.
        /// Hint 1: There are 10 000 ticks in 1 ms.
        /// Hint 2: Stopwatch is a good source of high frequency timer, and it returns ticks.
        /// </summary>
        /// <param name="ticks">Number of ticks</param>
        /// <param name="sampleCount">Number of samples this time measurement is for (used for average calculation)</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddTimeMeasurement(Int64 ticks, int sampleCount = 1)
        {
            if (Options.IsAverage && sampleCount < 1)
                throw new ArgumentOutOfRangeException(nameof(sampleCount));

            if (Options.IsAverage)
            {
                _timeMeasurements.Enqueue(new TimeMeasurement(ticks: ticks, timestampTicks: _stopwatch.ElapsedTicks,
                    sampleCount: sampleCount));

                Interlocked.Add(ref _sampleCount, sampleCount);
            }

            Interlocked.Add(ref _sampleTotalTime, ticks);

            if (Options.AutoClean)
                Cleanup();
        }

        /// <summary>
        /// When ProfileType is of type average we need to clean up history queue. This is normally done when GetValue is called.
        /// But if GetValue() is not called regularly this may cause history buffer to fill up.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Cleanup()
        {
            if (!Options.IsAverage)
                return;

            Int32 sc = 0;
            Int64 t = 0;
            // Remove excess over max count
            while (_timeMeasurements.Count > Options.MaxHistoryItems)
            {
                if (_timeMeasurements.TryDequeue(out var tm))
                {
                    sc += tm.SampleCount;
                    t += tm.Ticks;
                }
            }

            // Remove expired
            var maxHistoryAgeTicks = Options.MaxHistoryAgeMs * 10_000;
            while (_timeMeasurements.TryPeek(out var tm))
            {
                var age = _stopwatch.ElapsedTicks - tm.TimestampTicks;
                if (age < maxHistoryAgeTicks)
                    break;

                // If Cleanup was run from another thread simultaneously we may fail in dequeue, or dequeue an item that is not expired. That is fine...
                if (_timeMeasurements.TryDequeue(out tm))
                {
                    sc += tm.SampleCount;
                    t += tm.Ticks;
                }
            }

            // Atomic update of global counters
            if (sc != 0)
                Interlocked.Add(ref _sampleCount, -sc);
            if (t != 0)
                Interlocked.Add(ref _sampleTotalTime, -t);

        }

        /// <summary>
        /// Get string representation of vaue.
        /// </summary>
        /// <returns></returns>
        public string GetText()
        {
            if (Options.ProfilerType == ProfilerType.Text)
                return Text;
            if (Options.StringFormat != null)
                return String.Format(Options.StringFormat, GetValue());
            //else
            return GetValue().ToString();
        }

        /// <summary>
        /// Get value
        /// </summary>
        /// <returns></returns>
        public double GetValue()
        {
            Cleanup();

            if (Options.ProfilerType == ProfilerType.Counter)
                return Counter;

            if (Options.ProfilerType == ProfilerType.TimeTotal)
                return (double)_sampleTotalTime / 10_000D;

            if (Options.ProfilerType == ProfilerType.SampleAveragePerSecond)
            {
                if (_sampleCount == 0)
                    return 0;

                return 1000D / ((double)(((double)_sampleTotalTime / (double)_sampleCount) / 10_000D));
            }

            if (Options.ProfilerType == ProfilerType.SampleAverageTimeMs)
            {
                if (_sampleCount == 0)
                    return 0;

                return (double)(((double)_sampleTotalTime / (double)_sampleCount) / 10_000D);
            }

            throw new Exception($"Unknown ProfilerType {Options.ProfilerType}");
        }


        #region IDisposable

        private void CheckLeak()
        {
            if (_profilerGroup != null)
                throw new Exception($"{nameof(Profiler)} created from {nameof(ProfilerGroup)} but not disposed, this causes leak in {nameof(ProfilerGroup)}.");
        }

        /// <summary>Detaches Profiler from ProfilerGroup. This is necessary to avoid exception in Finalizer.</summary>
        public void Dispose()
        {
            _profilerGroup?.RemoveProfiler(this);
            _profilerGroup = null;
            GC.SuppressFinalize(this);
        }

        /// <summary>If Dispose() was not run this will throw exception.</summary>
        ~Profiler()
        {
            CheckLeak();
        }

        #endregion
    }
}
