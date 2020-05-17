using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

namespace Tedd
{
    public class ProfilerGroup
    {
        public static readonly ProfilerGroup Default = new ProfilerGroup();
        private static readonly ObjectPool<HashSet<Profiler>> _profileListPool = new ObjectPool<HashSet<Profiler>>(() => new HashSet<Profiler>(), set => set.Clear(), 20);

        private readonly SortedDictionary<string, HashSet<Profiler>> _profilers = new SortedDictionary<string, HashSet<Profiler>>();
        private readonly ReaderWriterLockSlim _profilesLockSlim = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);

        /// <summary>
        /// Creates a new instance of Profiler with custom name.
        /// </summary>
        /// <param name="options">Profiler options</param>
        /// <param name="name">Name of profiler</param>
        /// <returns></returns>
        public Profiler CreateInstance(ProfilerOptions options, string name)
        {
            var profiler = new Profiler(this, options, name);
            AddProfiler(profiler);
            return profiler;
        }

        /// <summary>
        /// Creates a new instance of Profiler with name set to full path of creator, i.e. App.MyClass.
        /// Do not use this from non-static context.
        /// </summary>
        /// <param name="options">Profiler options</param>
        /// <param name="name">Optional name to append to path, i.e. ":Name" would give App.MyClass:Name</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public Profiler CreateInstanceWithPath(ProfilerOptions options, string name = null)
        {
            var stackTrace = new StackTrace();
            var callingFrame = stackTrace.GetFrame(1);
            var callingMethod = callingFrame.GetMethod();
            if (!callingMethod.IsStatic)
                throw new Exception($"{nameof(Profiler)}() created from non-static. Creating profiler without name has huge overhead due to stack analysis. Only do so from static context so it minimizes number of times it is done.");

            return CreateInstance(options, callingMethod.DeclaringType.FullName +  (name??""));
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
