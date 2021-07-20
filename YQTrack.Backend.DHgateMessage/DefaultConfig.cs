using System.ComponentModel;
using YQTrack.Backend.Seller.TrackInfo.ESHelper;
using YQTrack.Backend.Sharding.Config;
using YQTrackV6.RabbitMQ.Config;
using YQTrack.Backend.RabbitMQ.ConfigSetting;
using YQTrack.Backend.RedisCache;
using YQTrack.Schedule;

namespace YQTrack.Backend.DHgateMessage
{
    internal class DefaultConfig
    {
        [Category("DBSharding-SellerConfig")]
        public DBShardingConfig SellerDBShardingConfig { get; set; }

        [Category("DBSharding-SellerMessage")]
        public DBShardingConfig SellerMessageDBShardingConfig { get; set; }

        [Category("SellerESConfig")]
        public ESItemConfig SellerESConfig { get; set; }

        [Category("RabbitMQ-Default-Seller-Es")]
        public RabbitMqConfigDefault RabbitMqConfigDefault { get; set; }

        [Category("EcommerceRedisCacheConfig")]
        public RedisConfigs RedisConfigs { get; set; }

        [Category("ScheduleSellerDHgateMessage")]
        public Config ScheduleSellerDHgateMessage { set; get; }
    }
}
