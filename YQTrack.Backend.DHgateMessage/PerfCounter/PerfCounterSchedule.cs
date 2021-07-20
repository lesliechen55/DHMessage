using System.Diagnostics;
using YQTrackV6.PerfCounter;

namespace YQTrack.Backend.DHgateMessage.PerfCounter
{
    internal sealed class PerfCounterSchedule : PerfCounterCategoryMulti
    {
        private PerfCounterSchedule() : base("YQ-Track.BackendServer.DHgateMessageSchedule")
        {
            AddCreationData(DefAllDelta, PerformanceCounterType.CounterDelta64);
            AddCreationData(DefAllCount, PerformanceCounterType.NumberOfItems64);

            AddCreationData(DefSuccessDelta, PerformanceCounterType.CounterDelta64);
            AddCreationData(DefSuccessCount, PerformanceCounterType.NumberOfItems64);

            AddCreationData(DefWarnDelta, PerformanceCounterType.CounterDelta64);
            AddCreationData(DefWarnCount, PerformanceCounterType.NumberOfItems64);

            AddCreationData(DeFailDelta, PerformanceCounterType.CounterDelta64);
            AddCreationData(DefFailCount, PerformanceCounterType.NumberOfItems64);
        }


        public static PerfCounterSchedule Default { get; } = new PerfCounterSchedule();

        private const string DefAllDelta = "AllDelta";
        private const string DefAllCount = "AllCount";

        private const string DefSuccessDelta = "SuccessDelta";
        private const string DefSuccessCount = "SuccessCount";

        private const string DefWarnDelta = "WarnDelta";
        private const string DefWarnCount = "WarnCount";

        private const string DeFailDelta = "FailDelta";
        private const string DefFailCount = "FailCount";

        public void AddSuccess(string instance)
        {
            Default[DefSuccessDelta].Increment(instance);
            Default[DefSuccessCount].Increment(instance);

            AddAll(instance);
        }

        public void AddWarn(string instance)
        {
            Default[DefWarnDelta].Increment(instance);
            Default[DefWarnCount].Increment(instance);

            AddAll(instance);
        }


        public void AddFail(string instance)
        {
            Default[DeFailDelta].Increment(instance);
            Default[DeFailDelta].Increment(instance);

            AddAll(instance);
        }

        private void AddAll(string instance)
        {

            Default[DefAllDelta].Increment(instance);
            Default[DefAllCount].Increment(instance);
        }
    }
}
