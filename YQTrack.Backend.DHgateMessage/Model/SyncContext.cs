using System.Collections.Generic;
using YQTrack.Backend.Models;
using YQTrack.Backend.ThirdPlatform.DTO;

namespace YQTrack.Backend.DHgateMessage.Model
{
    /// <summary>
    /// 同步上下文
    /// </summary>
    public class SyncContext
    {
        public DHgateShopDto DHgateShopDto { get; set; }
        public DataRouteModel DataRouteModel { get; set; }
        public string Token { get; set; }
        public int CurrentPage { get; set; }
        public List<string> PreMsgIdList { get; set; } = new List<string>();
        public int TotalCount { get; set; }
        public int UpdateCount { get; set; }
        public int NewCount { get; set; }
    }
}
