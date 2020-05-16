using System;

namespace Tedd
{
    internal readonly struct TimeMeasurement
    {
        public readonly Int64 Ticks;
        public readonly Int64 TimestampTicks;
        public readonly int SampleCount;

        public TimeMeasurement(Int64 ticks, Int64 timestampTicks, Int32 sampleCount)
        {
            Ticks = ticks;
            TimestampTicks = timestampTicks;
            SampleCount = sampleCount;
        }
    }
}