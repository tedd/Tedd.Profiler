namespace Tedd
{
    public readonly struct ProfilerOptions
    {
        public readonly ProfilerType ProfilerType;
        public readonly int MaxHistoryItems;
        public readonly int MaxHistoryAgeMs;
        public readonly string StringFormat;
        internal readonly int MaxHistoryAgeTicks;
        internal readonly bool IsAverage;

        public ProfilerOptions(ProfilerType profilerType, int maxHistoryItems = 100, int maxHistoryAgeMs = 2000, string stringFormat= "{0:0,0.00}")
        {
            MaxHistoryItems = maxHistoryItems;
            MaxHistoryAgeMs = maxHistoryAgeMs;
            MaxHistoryAgeTicks = maxHistoryAgeMs * 10_000;
            ProfilerType = profilerType;
            StringFormat = stringFormat;

            IsAverage = (profilerType == ProfilerType.SampleAverageTimeMs ||
                         profilerType == ProfilerType.SampleAveragePerSecond);
        }
    }
}