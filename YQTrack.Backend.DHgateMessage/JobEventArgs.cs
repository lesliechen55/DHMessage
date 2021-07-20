using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YQTrack.Backend.DHgateMessage.Model;

namespace YQTrack.Backend.DHgateMessage
{
    /// <summary>
    /// JobEventArgs
    /// </summary>
    public class JobEventArgs: EventArgs
    {
        public JobInfo JobInfo { get; set; }

        /// <summary>
        /// 构造
        /// </summary>
        /// <param name="jobInfo"></param>
        public JobEventArgs(JobInfo jobInfo)
        {
            JobInfo = jobInfo;
        }
    }
}
