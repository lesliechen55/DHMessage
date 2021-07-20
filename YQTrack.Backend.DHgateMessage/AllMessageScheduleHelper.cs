using FluentScheduler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YQTrack.Backend.Factory;
using YQTrack.Backend.Models;
using YQTrack.Backend.ThirdPlatform.DTO;
using YQTrack.Backend.ThirdPlatform.Entity;
using YQTrack.Backend.ThirdPlatform.IBLL;
using YQTrackV6.Log;

namespace YQTrack.Backend.DHgateMessage
{
    public class AllMessageScheduleHelper : IJob
    {
        public DataRouteModel DataRouteModel { get; set; }

        public void Execute()
        {
            Task.Run(async () =>
            {
                var dHgateMessageBll = FactoryContainer.Create<IDHgateMessageBll>();
                dHgateMessageBll.SetDataRoute(DataRouteModel);
                var list = await dHgateMessageBll.GetAllDHgateShopList();
                foreach (var item in list)
                {
                    try
                    {
                        dHgateMessageBll.ResetMessageSyncTime(item.FShopId);
                    }
                    catch (Exception ex)
                    {
                        LogHelper.LogObj(LogDefine.SyncResetError, ex, item);
                        continue;
                    }
                    //
                    LogHelper.LogObj(LogDefine.SyncReset, item);
                }
            });
        }
    }

    public class JobRegistry : Registry
    {

    }
}
