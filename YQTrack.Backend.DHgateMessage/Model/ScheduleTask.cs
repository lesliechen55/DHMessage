using System.Threading;
using System.Threading.Tasks;

namespace YQTrack.Backend.DHgateMessage.Model
{
    /// <summary>
    /// ScheduleTask
    /// </summary>
    internal class ScheduleTask
    {
        /// <summary>
        /// Task
        /// </summary>
        public Task Task { get; set; }

        /// <summary>
        /// 取消操作通知传播对象
        /// </summary>
        public CancellationTokenSource CancelTokenSource { get; set; }
    }
}
