using System.Diagnostics;
using YQTrackV6.PerfCounter;

namespace YQTrack.Backend.DHgateMessage.PerfCounter
{
    internal sealed class PerfCounterSync : PerfCounterCategorySingle
    {
        private PerfCounterSync() : base("YQ-Track.BackendServer.DHgateMessageSync")
        {
            AddCreationData(DefAllDelta, PerformanceCounterType.CounterDelta64);
            AddCreationData(DefAllCount, PerformanceCounterType.NumberOfItems64);

            AddCreationData(DefUpdateDelta, PerformanceCounterType.CounterDelta64);
            AddCreationData(DefUpdateCount, PerformanceCounterType.NumberOfItems64);

            AddCreationData(DefNewDelta, PerformanceCounterType.CounterDelta64);
            AddCreationData(DefNewCount, PerformanceCounterType.NumberOfItems64);

            AddCreationData(DefNoUpdateDelta, PerformanceCounterType.CounterDelta64);
            AddCreationData(DefNoUpdateCount, PerformanceCounterType.NumberOfItems64);
        }

        public static PerfCounterSync Default { get; } = new PerfCounterSync();

        private const string DefAllDelta = "AllDelta";
        private const string DefAllCount = "AllCount";

        private const string DefUpdateDelta = "UpdateDelta";
        private const string DefUpdateCount = "UpdateCount";

        private const string DefNewDelta = "NewDelta";
        private const string DefNewCount = "NewCount";

        private const string DefNoUpdateDelta = "NoUpdateDelta";
        private const string DefNoUpdateCount = "NoUpdateCount";

        public void AddUpdate(int count)
        {
            Default[DefUpdateDelta].IncrementBy(count);
            Default[DefUpdateCount].IncrementBy(count);

            AddAll(count);
        }

        public void AddNew(int count)
        {
            Default[DefNewDelta].IncrementBy(count);
            Default[DefNewCount].IncrementBy(count);

            AddAll(count);
        }

        public void AddNoUpdate(int count)
        {
            Default[DefNewDelta].IncrementBy(count);
            Default[DefNewCount].IncrementBy(count);

            AddAll(count);
        }


        private void AddAll(int count)
        {

            Default[DefAllDelta].IncrementBy(count);
            Default[DefAllCount].IncrementBy(count);
        }
    }
}
