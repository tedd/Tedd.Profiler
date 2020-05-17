using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

namespace Tedd
{
    public class ProfilerRoot
    {
        public static readonly ProfilerRoot Default = new ProfilerRoot();
        private static readonly ObjectPool<HashSet<Profiler>> _profileListPool = new ObjectPool<HashSet<Profiler>>(() => new HashSet<Profiler>(), set => set.Clear(), 20);

        private readonly SortedDictionary<string, HashSet<Profiler>> _profilers = new SortedDictionary<string, HashSet<Profiler>>();
        private readonly ReaderWriterLockSlim _profilesLockSlim = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
        
        public Profiler CreateInstance(ProfilerOptions options, string name)
        {
            var profiler = new Profiler(this,options, name);
            AddProfiler(profiler);
            return profiler;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public Profiler CreateInstance(ProfilerOptions options)
        {
            var stackTrace = new StackTrace();
            var callingFrame = stackTrace.GetFrame(1);
            var callingMethod = callingFrame.GetMethod();
            if (!callingMethod.IsStatic)
                throw new Exception($"{nameof(Profiler)}() created from non-static. Creating profiler without name has huge overhead due to stack analysis. Only do so from static context so it minimizes number of times it is done.");

            return CreateInstance(options, callingMethod.DeclaringType.FullName);
        }

        private void AddProfiler(Profiler profiler)
        {
            _profilesLockSlim.EnterWriteLock();
            try
            {
                if (!_profilers.TryGetValue(profiler.Name, out var list))
                {
                    list = _profileListPool.Allocate();
                    _profilers.Add(profiler.Name, list);
                }

                list.Add(profiler);
            }
            finally
            {
                _profilesLockSlim.ExitWriteLock();
            }
        }

        internal void RemoveProfiler(Profiler profiler)
        {
            _profilesLockSlim.EnterWriteLock();
            try
            {
                //while (_deleteQueue.TryPop(out var profiler))
                {
                    if (_profilers.TryGetValue(profiler.Name, out var list))
                    {
                        list.Remove(profiler);
                        if (list.Count == 0)
                        {
                            _profilers.Remove(profiler.Name);
                            _profileListPool.Free(list);
                        }
                    }
                }
            }
            finally
            {
                _profilesLockSlim.ExitWriteLock();
            }
        }

        /// <summary>
        /// Generates a list of key+value from profilers created from this root
        /// </summary>
        /// <returns></returns>
        public List<KeyValuePair<string, string>> GetMeasurements()
        {
            var list = new List<KeyValuePair<string, string>>();
            _profilesLockSlim.EnterReadLock();
            try
            {
                foreach (var kvp in _profilers)
                    foreach (var v in kvp.Value)
                        list.Add(new KeyValuePair<string, string>(kvp.Key, v.GetText()));
            }
            finally
            {
                _profilesLockSlim.ExitReadLock();
            }

            return list;
        }
    }
}
