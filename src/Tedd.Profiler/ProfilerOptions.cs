namespace Tedd
{
    public readonly struct ProfilerOptions
    {
        public readonly ProfilerType ProfilerType;
        public readonly int MaxHistoryItems;
        public readonly int MaxHistoryAgeMs;
        internal readonly int MaxHistoryAgeTicks;

        public ProfilerOptions(ProfilerType profilerType, int maxHistoryItems = 100, int maxHistoryAgeMs = 2000)
        {
            MaxHistoryItems = maxHistoryItems;
            MaxHistoryAgeMs = maxHistoryAgeMs;
            MaxHistoryAgeTicks = maxHistoryAgeMs * 10_000;
            ProfilerType = profilerType;
        }
    }
}