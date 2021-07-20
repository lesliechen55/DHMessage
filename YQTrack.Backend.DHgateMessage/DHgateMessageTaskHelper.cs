using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Polly;
using YQTrack.Backend.DHgate;
using YQTrack.Backend.DHgate.Model;
using YQTrack.Backend.DHgateMessage.Model;
using YQTrack.Backend.DHgateMessage.PerfCounter;
using YQTrack.Backend.Factory;
using YQTrack.Backend.Models;
using YQTrack.Backend.RedisCache;
using YQTrack.Backend.Seller.TrackInfo.ESHelper;
using YQTrack.Backend.Sharding;
using YQTrack.Backend.ThirdPlatform.DTO;
using YQTrack.Backend.ThirdPlatform.Entity;
using YQTrack.Backend.ThirdPlatform.Entity.Message;
using YQTrack.Backend.ThirdPlatform.IBLL;
using YQTrack.Configuration;
using YQTrackV6.Common.Utils;
using YQTrackV6.Log;

namespace YQTrack.Backend.DHgateMessage
{
    public class DHgateMessageTaskHelper
    {
        private static DHgateService _DHgateService;

        /// <summary>
        /// 初始化
        /// </summary>
        public static void Init()
        {
            PerfCounterSync.Default.Init();
            //
            ConfigManager.Initialize(typeof(DefaultConfig));
            DBShardingRouteFactory.InitDBShardingConfig(ConfigManager.GetConfig<DefaultConfig>().SellerDBShardingConfig);
            DBShardingRouteFactory.InitDBShardingConfig(ConfigManager.GetConfig<DefaultConfig>().SellerMessageDBShardingConfig);
            SellerElasticSearchSettingHelper.InitConfig(ConfigManager.GetConfig<DefaultConfig>().SellerESConfig,
                ConfigManager.GetConfig<DefaultConfig>().RabbitMqConfigDefault);
            RedisCacheSettingsHelper.InitRedisConfig(ConfigManager.GetConfig<DefaultConfig>().RedisConfigs);
            //
            LogHelper.LogObj(LogDefine.InitializeTask, nameof(DBShardingRouteFactory));
            //
            _DHgateService = new DHgateService(new DHgateConfiguration());
        }

        /// <summary>
        /// 同步
        /// </summary>
        /// <param name="dHgateShopDto"></param>
        /// <param name="dataRouteModel"></param>
        /// <param name="isAll"></param>
        /// <returns></returns>
        public static async Task<bool> SyncMessage(DHgateShopDto dHgateShopDto, DataRouteModel dataRouteModel, bool isAll = false)
        {
            var task = new
            {
                dHgateShopDto.FUserId,
                dHgateShopDto.FShopId,
                dHgateShopDto.FShopName,
                dHgateShopDto.BeforeDays,
                dHgateShopDto.FNextMessageSyncingTime
            };
            if (string.IsNullOrEmpty(dHgateShopDto.FAccessToken) || string.IsNullOrWhiteSpace(dHgateShopDto.FAccessToken))
            {
                LogHelper.LogObj(LogDefine.TokenEmpty, task);
                return false;
            }
            //
            var token = GetToken(dHgateShopDto.FAccessToken);
            if (string.IsNullOrEmpty(token))
            {
                return false;
            }
            //
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            //同步
            var syncContext = new SyncContext
            {
                DHgateShopDto = dHgateShopDto,
                DataRouteModel = dataRouteModel,
                Token = token,
                CurrentPage = 1
            };
            await GetMessageList(syncContext, isAll);
            stopwatch.Stop();
            LogHelper.LogObj(LogDefine.SyncMessageList, new
            {
                Name = "GetMessageList",
                stopwatch.Elapsed.TotalSeconds,
                task,
                syncContext.TotalCount,
                syncContext.NewCount,
                syncContext.UpdateCount
            });
            //
            return true;
        }

        /// <summary>
        /// SyncOtherMessage
        /// </summary>
        /// <param name="dHgateShopDto"></param>
        /// <param name="dataRouteModel"></param>
        /// <returns></returns>
        public static async Task<bool> SyncOtherMessage(DHgateShopDto dHgateShopDto, DataRouteModel dataRouteModel)
        {
            var task = new
            {
                dHgateShopDto.FUserId,
                dHgateShopDto.FShopId,
                dHgateShopDto.FShopName,
                dHgateShopDto.BeforeDays,
                dHgateShopDto.FNextMessageSyncingTime
            };
            if (string.IsNullOrEmpty(dHgateShopDto.FAccessToken) || string.IsNullOrWhiteSpace(dHgateShopDto.FAccessToken))
            {
                LogHelper.LogObj(LogDefine.TokenEmpty, task);
                return false;
            }
            //
            var token = GetToken(dHgateShopDto.FAccessToken);
            if (string.IsNullOrEmpty(token))
            {
                return false;
            }
            //
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            //同步
            var syncContext = new SyncContext
            {
                DHgateShopDto = dHgateShopDto,
                DataRouteModel = dataRouteModel,
                Token = token,
                CurrentPage = 1
            };
            await GetMessageListOther(syncContext);
            stopwatch.Stop();
            LogHelper.LogObj(LogDefine.SyncMessageList, new
            {
                Name = "SyncOtherMessage",
                stopwatch.Elapsed.TotalSeconds,
                task,
                syncContext.TotalCount,
                syncContext.NewCount,
                syncContext.UpdateCount
            });
            //
            return true;
        }



        public static async Task<bool> SyncTrachMessage(DHgateShopDto dHgateShopDto, DataRouteModel dataRouteModel, bool isAll = false)
        {
            var task = new
            {
                dHgateShopDto.FUserId,
                dHgateShopDto.FShopId,
                dHgateShopDto.FShopName,
                dHgateShopDto.BeforeDays,
                dHgateShopDto.FNextMessageSyncingTime
            };
            if (string.IsNullOrEmpty(dHgateShopDto.FAccessToken) || string.IsNullOrWhiteSpace(dHgateShopDto.FAccessToken))
            {
                LogHelper.LogObj(LogDefine.TokenEmpty, task);
                return false;
            }
            //
            var token = GetToken(dHgateShopDto.FAccessToken);
            if (string.IsNullOrEmpty(token))
            {
                return false;
            }
            //
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            //同步
            var syncContext = new SyncContext
            {
                DHgateShopDto = dHgateShopDto,
                DataRouteModel = dataRouteModel,
                Token = token,
                CurrentPage = 1
            };
            await GetTrachMessageList(syncContext, isAll);
            stopwatch.Stop();
            LogHelper.LogObj(LogDefine.SyncMessageList, new
            {
                Name = "SyncTrachMessage",
                stopwatch.Elapsed.TotalSeconds,
                task,
                syncContext.TotalCount,
                syncContext.NewCount,
                syncContext.UpdateCount
            });
            //
            return true;
        }



        /// <summary>
        /// 关联订单
        /// </summary>
        /// <param name="dHgateShopDto"></param>
        /// <param name="dataRouteModel"></param>
        /// <returns></returns>
        public static async Task MessageLinkOrder(DHgateShopDto dHgateShopDto, DataRouteModel dataRouteModel)
        {
            var userId = dHgateShopDto.FUserId;
            var shopId = dHgateShopDto.FShopId;
            //
            var syncContext = new SyncContext
            {
                DHgateShopDto = dHgateShopDto,
                DataRouteModel = dataRouteModel
            };
            //
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            //
            var dHgateMessageBll = FactoryContainer.Create<IDHgateMessageBll>();
            dHgateMessageBll.SetDataRoute(syncContext.DataRouteModel);
            var messageList = await dHgateMessageBll.SearchDHgateMessage(userId
                , shopId);
            if (messageList != null && messageList.Any())
            {
                var orderIds = messageList.Select(a => a.FOrder_Id).Distinct().ToList();
                //
                while (orderIds.Any())
                {
                    var tempIds = orderIds.Take(100);
                    var searchList = new List<string>();
                    //foreach (var id in tempIds)
                    //    searchList.Add($"'{id}'");
                    //var removeList = new List<string>();
                    //foreach (var id in tempIds)
                    //    removeList.Add(id);

                    var removeList = new List<string>();
                    foreach (var id in tempIds)
                    {
                        searchList.Add($"'{id}'");
                        removeList.Add(id);
                    }

                    try
                    {
                        var orderDtos = await dHgateMessageBll.GetDHgateOrderDto(shopId, searchList);
                        if (orderDtos.Any())
                        {
                            var tempList = new List<LinkDHgateOrderDto>();
                            foreach (var order in orderDtos)
                            {
                                var item = messageList.FirstOrDefault(a => a.FOrder_Id == order.FOrder_Id);
                                if (item.FOrderId <= 0)
                                {
                                    item.FOrderId = order.FOrderId;
                                    tempList.Add(item);
                                }
                            }
                            //
                            if (tempList.Any())
                            {
                                await dHgateMessageBll.LinkDHgateMessage(tempList);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LogHelper.LogObj(LogDefine.SyncWarn, ex, new
                        {
                            Name = "GetDHgateOrderDto",
                            shopId,
                            tempIds
                        });
                    }
                    //
                    foreach (var id in removeList)
                        orderIds.Remove(id);
                }
            }
            //
            stopwatch.Stop();
            LogHelper.LogObj(LogDefine.SyncMessageList, new
            {
                Name = "MessageLinkOrder",
                stopwatch.Elapsed.TotalSeconds,
                dHgateShopDto.FUserId,
                dHgateShopDto.FShopId,
                dHgateShopDto.FShopName
            });
        }

        #region 私有方法

        #region GetMessageList
        /// <summary>
        /// 同步
        /// </summary>
        /// <param name="syncContext"></param>
        /// <param name="isAll"></param>
        /// <returns></returns>
        private static async Task GetMessageList(SyncContext syncContext, bool isAll = false)
        {
            var task = new
            {
                syncContext.DHgateShopDto.FUserId,
                syncContext.DHgateShopDto.FShopId,
                syncContext.DHgateShopDto.FShopName,
                syncContext.DHgateShopDto.BeforeDays
            };

            //设置重试
            var policy = Policy.Handle<Exception>().RetryAsync(5, (exception, retryCount, context) =>
            {
                LogHelper.LogObj(LogDefine.SyncWarn, exception,
                    new { task, methodName = context["methodName"], errorMsg = exception.Message });
            });
            //
            await policy.ExecuteAsync(
                    async (t) =>
                    {
                        await GetMessageListLoop(syncContext, isAll);
                    },
                     new Dictionary<string, object>() { { "methodName", "GetMessageListLoop" } }
                 ).ContinueWith(t =>
                 {
                     if (t.Exception != null)
                     {
                         LogHelper.LogObj(LogDefine.SyncWarn, t.Exception.InnerException, task);
                     }
                 });

            //await GetTrashMessageListLoop(syncContext, isAll); //获取垃圾箱数据
        }

        /// <summary>
        /// 同步
        /// </summary>
        /// <param name="syncContext"></param>
        /// <param name="isAll"></param>
        /// <returns></returns>
        private static async Task GetMessageListLoop(SyncContext syncContext, bool isAll = false)
        {
            var result = await _DHgateService.GetMessageList(syncContext.CurrentPage, syncContext.DHgateShopDto.BeforeDays, syncContext.Token);
            //
            if (!result.IsSuccess || string.IsNullOrEmpty(result.ResultData))
            {
                //重试
                throw new Exception(result.ErrorMsg);
            }
            //
            if (!string.IsNullOrEmpty(result.ErrorMsg))
            {
                LogHelper.LogObj(LogDefine.SyncWarn, new
                {
                    method = "GetMessageList",
                    syncContext.DHgateShopDto.FUserId,
                    syncContext.DHgateShopDto.FShopId,
                    syncContext.DHgateShopDto.FShopName,
                    syncContext.DHgateShopDto.BeforeDays,
                    result.ErrorMsg
                });
            }
            //保存数据
            var isHaveMore = await SaveData(result.ResultData, syncContext, isAll);
            //
            if (isHaveMore)
            {
                syncContext.CurrentPage += 1;
                //
                await GetMessageList(syncContext, isAll);
            }
        }



        private static async Task GetTrachMessageList(SyncContext syncContext, bool isAll = false)
        {
            var task = new
            {
                syncContext.DHgateShopDto.FUserId,
                syncContext.DHgateShopDto.FShopId,
                syncContext.DHgateShopDto.FShopName,
                syncContext.DHgateShopDto.BeforeDays
            };

            //设置重试
            var policy = Policy.Handle<Exception>().RetryAsync(5, (exception, retryCount, context) =>
            {
                LogHelper.LogObj(LogDefine.SyncWarn, exception,
                    new { task, methodName = context["methodName"], errorMsg = exception.Message });
            });
            //
            await policy.ExecuteAsync(
                    async (t) =>
                    {
                        await GetTrashMessageListLoop(syncContext, isAll);//获取垃圾箱数据
                    },
                     new Dictionary<string, object>() { { "methodName", "GetTrachMessageList" } }
                 ).ContinueWith(t =>
                 {
                     if (t.Exception != null)
                     {
                         LogHelper.LogObj(LogDefine.SyncWarn, t.Exception.InnerException, task);
                     }
                 });
        }




        /// <summary>
        /// 同步垃圾箱数据
        /// </summary>
        /// <param name="syncContext"></param>
        /// <param name="isAll"></param>
        /// <returns></returns>
        private static async Task GetTrashMessageListLoop(SyncContext syncContext, bool isAll = false)
        {
            var result = await _DHgateService.GetTrashMessageList(syncContext.CurrentPage, syncContext.DHgateShopDto.BeforeDays, syncContext.Token, "1");
            //
            if (!result.IsSuccess || string.IsNullOrEmpty(result.ResultData))
            {
                //重试
                throw new Exception(result.ErrorMsg);
            }
            //
            if (!string.IsNullOrEmpty(result.ErrorMsg))
            {
                LogHelper.LogObj(LogDefine.SyncWarn, new
                {
                    method = "GetTrashMessageListLoop",
                    syncContext.DHgateShopDto.FUserId,
                    syncContext.DHgateShopDto.FShopId,
                    syncContext.DHgateShopDto.FShopName,
                    syncContext.DHgateShopDto.BeforeDays,
                    result.ErrorMsg
                });
            }
            //保存数据
            var isHaveMore = await SaveData(result.ResultData, syncContext, isAll);
            //
            if (isHaveMore)
            {
                syncContext.CurrentPage += 1;
                //
                await GetTrachMessageList(syncContext, isAll);
            }
        }


        /// <summary>
        /// 保存数据
        /// </summary>
        /// <param name="resultStr"></param>
        /// <param name="syncContext"></param>
        /// <param name="isAll"></param>
        /// <returns>是否有更多</returns>
        private static async Task<bool> SaveData(string resultStr, SyncContext syncContext, bool isAll = false)
        {
            //解析
            var resultList = GetTDHgateMessages(resultStr, syncContext)?.ToList();
            //解析内容后为空，表示没有下一页
            if (resultList == null || !resultList.Any())
                return false;
            //
            var userId = syncContext.DHgateShopDto.FUserId;
            var shopId = syncContext.DHgateShopDto.FShopId;
            var dHgateMessageBll = FactoryContainer.Create<IDHgateMessageBll>();
            dHgateMessageBll.SetDataRoute(syncContext.DataRouteModel);

            //判断是否有同步到新的内容
            var bllResult = await dHgateMessageBll.SaveDHgateMessage(resultList
                , userId
                , shopId
                , isAll);

            //
            var updateCount = bllResult.Item2;
            var newCount = 0;
            //
            var needSave = bllResult.Item1;
            if (needSave.Any())
            {
                //查找订单并默认获取OrderItem来填充产品信息
                var orderIds = needSave.Where(a => !string.IsNullOrEmpty(a.FOrder_Id) && a.FOrderId == 0).Select(a => a.FOrder_Id).ToList();
                while (orderIds.Any())
                {
                    var tempIds = orderIds.Take(100);
                    var searchList = new List<string>();
                    //foreach (var id in tempIds)
                    //    searchList.Add($"'{id}'");
                    var removeList = new List<string>();
                    //foreach (var id in tempIds)
                    //    removeList.Add(id);

                    foreach (var id in tempIds)
                    {
                        searchList.Add($"'{id}'");
                        removeList.Add(id);
                    }

                    //
                    var orderDtos = await dHgateMessageBll.GetDHgateOrderDto(shopId, searchList);
                    if (orderDtos.Any())
                    {
                        foreach (var order in orderDtos)
                        {
                            var item = resultList.FirstOrDefault(a => a.FOrder_Id == order.FOrder_Id);
                            if (item.FOrderId <= 0)
                            {
                                item.FOrderId = order.FOrderId;
                                item.FItem_Id = order.FItem_Id;
                                item.FItem_Title = order.FItem_Title;
                                item.FItem_Url = order.FItem_Url;
                                item.FItem_Image = order.FItem_Image;
                            }
                        }
                    }
                    //
                    foreach (var id in removeList)
                        orderIds.Remove(id);
                }
                
                foreach (var message in needSave)
                {
                    var detailList = await GetMessageDetailList(message.FMsgId, syncContext);
                    //Detail
                    message.FMsgList = JsonConvert.SerializeObject(detailList);
                    //
                    message.FIsReply = detailList.First().senderNickName.Trim() == syncContext.DHgateShopDto.FShopName.Trim();
                    //
                    if (message.FSubjectId == 0)
                    {
                        if (message.FMsgType == "001" && !string.IsNullOrEmpty(message.FItem_Id))
                        {
                            if (long.TryParse(message.FItem_Id, out var itemId))
                            {
                                var productList = await GetProduct(syncContext, message.FItem_Id);
                                if (productList.Any())
                                {
                                    message.FItem_Image = productList.First().ItemImage;
                                }
                            }
                        }
                    }
                }

                //保存数据
                var result = await dHgateMessageBll.SaveNewDHgateMessage(needSave
                     , userId
                     , shopId);
                updateCount += result.Item2;
                newCount = result.Item1;

            }
            //
            var noUpdate = resultList.Count - newCount - updateCount;
            //
            dHgateMessageBll.UpdateRecord(shopId, newCount, resultList.Count);
            //
            syncContext.TotalCount += resultList.Count;
            syncContext.NewCount += newCount;
            syncContext.UpdateCount += updateCount;
            //
            PerfCounterSync.Default.AddNew(newCount);
            PerfCounterSync.Default.AddUpdate(updateCount);
            PerfCounterSync.Default.AddNoUpdate(noUpdate);
            //
            resultList.Clear();
            //默认获取下一页
            return true;
        }


        /// <summary>
        /// GetTDHgateMessages
        /// </summary>
        /// <param name="resultString"></param>
        /// <param name="syncContext"></param>
        /// <returns></returns>
        private static IEnumerable<TDHgateMessage> GetTDHgateMessages(string resultString, SyncContext syncContext)
        {
            var result = new List<TDHgateMessage>();
            //解析
            var jObj = JObject.Parse(resultString);
            if (jObj["msgList"] != null && jObj["msgList"].ToArray().Any())
            {
                //List
                var msgIdList = new List<string>();
                foreach (var msg in jObj["msgList"].ToArray())
                {
                    if (syncContext.PreMsgIdList.Any(a => a == msg["msgId"].ToString()))
                    {
                        //获取到相同内容
                        break;
                    }
                    var msgId = msg["msgId"].ToString();
                    //
                    #region 整合message
                    //
                    var message = new TDHgateMessage
                    {
                        FUserId = syncContext.DHgateShopDto.FUserId,
                        FShopId = syncContext.DHgateShopDto.FShopId,
                        FMsgId = msgId,
                        FMsgTitle = msg["msgTitle"].ToString(),
                        FMsgType = msg["msgType"].ToString(),
                        FLastReplyTime = DateTime.Parse(msg["lastReplyTime"].ToString()).ToUniversalTime(),
                        FReplyCount = int.Parse(msg["msgReplyCount"].ToString()),
                        FIsDelete = false
                    };
                    //
                    //接收人站内信状态 0:正常,1:垃圾箱
                    string receiverStatus = msg["receiverStatus"] == null ? "0" : msg["receiverStatus"].ToString();
                    if (receiverStatus == "1")
                    {
                        message.FMessageFolder = (int)MessageFolder.Trash;//垃圾箱处理
                    }
                    else
                    {
                        message.FMessageFolder = ReturnMessageFolder(message.FMsgType);
                    }
                    //
                    if (message.FMsgType == "001")
                    {
                        message.FItem_Id = msg["param"].ToString();
                        message.FItem_Title = message.FMsgTitle;
                    }
                    if (message.FMsgType == "002")
                    {
                        message.FOrder_Id = msg["param"].ToString();
                    }
                    //
                    message.FIsSupportReply = message.FMessageFolder == (int)MessageFolder.BuyerMessage;
                    //
                    var shopName = syncContext.DHgateShopDto.FShopName;
                    //
                    if (msg["receiverNickName"].ToString().Trim() == shopName.Trim())
                    {
                        message.FSellerNickName = msg["receiverNickName"].ToString();
                        message.FSellerUserId = msg["recieverOrgId"].ToString();
                        message.FSellerReceiverId = msg["recieverId"].ToString();
                        //
                        message.FBuyerNickName = msg["senderNickName"].ToString();
                        message.FBuyerUserId = msg["senderOrgId"].ToString();
                        message.FBuyerReceiverId = msg["senderId"].ToString();
                        //
                        message.FIsRead = msg["receiverRead"].ToString() == "1";
                        message.FIsMark = msg["receiverMark"].ToString() == "1";

                        message.FIsDelete = receiverStatus == "2";//by austin 21-07-08
                    }
                    else
                    {
                        message.FSellerNickName = msg["senderNickName"].ToString();
                        message.FSellerUserId = msg["senderOrgId"].ToString();
                        message.FSellerReceiverId = msg["senderId"].ToString();
                        //
                        message.FBuyerNickName = msg["receiverNickName"].ToString();
                        message.FBuyerUserId = msg["recieverOrgId"].ToString();
                        message.FBuyerReceiverId = msg["recieverId"].ToString();
                        //
                        message.FIsRead = msg["senderRead"].ToString() == "1";
                        message.FIsMark = msg["senderMark"].ToString() == "1";

                        var status = msg["senderStatus"].ToString();
                        message.FIsDelete = status == "2";//by austin 21-07-08
                    }
                    #endregion
                    //
                    message.FIsReply = message.FSellerNickName.Trim() == shopName.Trim();//by austin 21-07-08
                    result.Add(message);
                    msgIdList.Add(message.FMsgId);
                }
                if (result.Any())
                {
                    syncContext.PreMsgIdList = msgIdList;
                }
            }
            //
            return result;
        }


        #endregion

        #region GetMessageDetail

        /// <summary>
        /// GetMessageDetailList
        /// </summary>
        /// <param name="msgId"></param>
        /// <param name="syncContext"></param>
        /// <returns></returns>
        private static async Task<IEnumerable<YQTrack.Backend.DHgate.Model.DHgateMessage>> GetMessageDetailList(string msgId, SyncContext syncContext)
        {
            var detailList = await GetDetailList(syncContext, msgId);
            if (detailList.Any())
            {
                detailList.OrderByDescending(a => a.createTime);
            }
            return detailList;
        }

        /// <summary>
        /// GetDetailList
        /// </summary>
        /// <param name="syncContext"></param>
        /// <param name="msgId"></param>
        /// <returns></returns>
        private static async Task<IEnumerable<YQTrack.Backend.DHgate.Model.DHgateMessage>> GetDetailList(SyncContext syncContext, string msgId)
        {
            var result = new List<YQTrack.Backend.DHgate.Model.DHgateMessage>();
            var detailString = await GetMessageDetail(syncContext, msgId);
            if (string.IsNullOrEmpty(detailString))
            {
                return result;
            }
            //
            try
            {
                var jObject = JObject.Parse(detailString);
                if (jObject["msgDetailList"] != null && jObject["msgDetailList"].ToArray().Any())
                {
                    foreach (var m in jObject["msgDetailList"].ToArray())
                    {
                        var dHgateMessage = new YQTrack.Backend.DHgate.Model.DHgateMessage
                        {
                            content = m["content"].ToString(),
                            createTime = DateTime.Parse(m["createTime"].ToString()).ToUniversalTime(),
                            senderNickName = m["senderNickName"].ToString(),
                            attNames = m["attNames"].ToString(),
                            attUrls = m["attUrls"].ToString()
                        };
                        //
                        if (!string.IsNullOrEmpty(dHgateMessage.senderNickName))
                        {
                            result.Add(dHgateMessage);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogObj(LogDefine.SyncWarn, ex, new
                {
                    syncContext.DHgateShopDto.FShopId,
                    msgId,
                    detailString
                });
            }
            //
            return result;
        }

        /// <summary>
        /// 同步
        /// </summary>
        /// <param name="syncContext"></param>
        /// <param name="msgId"></param>
        /// <returns></returns>
        private static async Task<string> GetMessageDetail(SyncContext syncContext, string msgId)
        {
            var result = string.Empty;
            var task = new
            {
                syncContext.DHgateShopDto.FUserId,
                syncContext.DHgateShopDto.FShopId,
                syncContext.DHgateShopDto.FShopName,
                syncContext.DHgateShopDto.BeforeDays,
                msgId
            };

            //设置重试
            var policy = Policy.Handle<Exception>().RetryAsync(5, (exception, retryCount, context) =>
            {
                LogHelper.LogObj(LogDefine.SyncWarn, exception,
                    new { task, methodName = context["methodName"], errorMsg = exception.Message });
            });
            //
            await policy.ExecuteAsync(
                    async (t) =>
                    {
                        result = await GetMessageDetailLoop(syncContext, msgId);
                    },
                     new Dictionary<string, object>() { { "methodName", "GetMessageDetail" } }
                 ).ContinueWith(t =>
                 {
                     if (t.Exception != null)
                     {
                         LogHelper.LogObj(LogDefine.SyncWarn, t.Exception.InnerException, task);
                     }
                 });
            return result;
        }

        /// <summary>
        /// 同步
        /// </summary>
        /// <param name="syncContext"></param>
        /// <param name="msgId"></param>
        /// <returns></returns>
        private static async Task<string> GetMessageDetailLoop(SyncContext syncContext, string msgId)
        {
            var result = await _DHgateService.GetMessageDetail(msgId, syncContext.Token);
            //
            if (!result.IsSuccess || string.IsNullOrEmpty(result.ResultData))
            {
                //重试
                throw new Exception(result.ErrorMsg);
            }
            //
            if (!string.IsNullOrEmpty(result.ErrorMsg))
            {
                LogHelper.LogObj(LogDefine.SyncWarn, new
                {
                    method = "GetMessageDetail",
                    syncContext.DHgateShopDto.FUserId,
                    syncContext.DHgateShopDto.FShopId,
                    syncContext.DHgateShopDto.FShopName,
                    syncContext.DHgateShopDto.BeforeDays,
                    msgId,
                    result.ErrorMsg
                });
            }
            //
            return result.ResultData;
        }

        #endregion

        #region GetMessageListOther
        /// <summary>
        /// GetMessageListOther
        /// </summary>
        /// <param name="syncContext"></param>
        /// <returns></returns>
        private static async Task GetMessageListOther(SyncContext syncContext)
        {
            var task = new
            {
                syncContext.DHgateShopDto.FUserId,
                syncContext.DHgateShopDto.FShopId,
                syncContext.DHgateShopDto.FShopName,
                syncContext.DHgateShopDto.BeforeDays
            };
            //设置重试
            var policy = Policy.Handle<Exception>().RetryAsync(5, (exception, retryCount, context) =>
            {
                LogHelper.LogObj(LogDefine.SyncWarn, exception,
                    new { task, methodName = context["methodName"], param = context["param"] });
            });

            var dHgateMessageBll = FactoryContainer.Create<IDHgateMessageBll>();
            dHgateMessageBll.SetDataRoute(syncContext.DataRouteModel);
            var list = await dHgateMessageBll.SearchDHgateMessageDto(syncContext.DHgateShopDto.FUserId,
                 syncContext.DHgateShopDto.FShopId);
            if (list != null && list.Any())
            {
                var messages = list.ToList();
                var removeList = new List<DHgateMessageDto>();
                while (messages.Any())
                {
                    removeList.Clear();
                    var tempMessage = messages.Take(1).ToList();
                    foreach (var id in tempMessage)
                        removeList.Add(id);

                    //
                    var msgId = tempMessage.First();
                    await policy.ExecuteAsync(
                        async (t) =>
                        {
                            await GetMessageListOther(msgId, syncContext);
                        },
                         new Dictionary<string, object>() { { "methodName", "GetMessageListOther" }, { "param", msgId } }
                     ).ContinueWith(t =>
                     {
                         if (t.Exception != null)
                         {
                             LogHelper.LogObj(LogDefine.SyncWarn, t.Exception.InnerException, task);
                         }
                     });
                    //
                    foreach (var id in removeList)
                        messages.Remove(id);
                }
            }
        }

        /// <summary>
        /// GetMessageListOther
        /// </summary>
        /// <param name="messageDto"></param>
        /// <param name="syncContext"></param>
        /// <returns></returns>
        private static async Task GetMessageListOther(DHgateMessageDto messageDto, SyncContext syncContext)
        {
            var result = await _DHgateService.GetDetail(messageDto.FMsgId, syncContext.Token);
            //
            if (!result.IsSuccess || string.IsNullOrEmpty(result.ResultData))
            {
                //重试
                throw new Exception(result.ErrorMsg);
            }
            //
            if (!string.IsNullOrEmpty(result.ErrorMsg))
            {
                LogHelper.LogObj(LogDefine.SyncWarn,
                   new
                   {
                       method = "GetMessageListOther",
                       task = new
                       {
                           syncContext.DHgateShopDto.FUserId,
                           syncContext.DHgateShopDto.FShopId,
                           syncContext.DHgateShopDto.FShopName,
                           messageDto.FMsgId
                       },
                       result.ErrorMsg
                   });
            }
            //保存数据
            UpdateData(result.ResultData, messageDto, syncContext);
        }

        /// <summary>
        /// UpdateData
        /// </summary>
        /// <param name="resultString"></param>
        /// <param name="messageDto"></param>
        /// <param name="syncContext"></param>
        /// <returns></returns>
        private static void UpdateData(string resultString, DHgateMessageDto messageDto, SyncContext syncContext)
        {
            //解析
            var jObj = JObject.Parse(resultString);
            if (jObj["message"] == null)
            {
                return;
            }
            if (jObj["message"]["messageTopic"] != null && jObj["message"]["messageInfo"] != null)
            {
                var shopName = syncContext.DHgateShopDto.FShopName;
                if (jObj["message"]["messageTopic"]["receiverNickname"].ToString().Trim() == shopName)
                {
                    messageDto.FIsRead = jObj["message"]["messageTopic"]["recivereaded"].ToString() == "1";
                    messageDto.FIsMark = jObj["message"]["messageTopic"]["reciveMarked"].ToString() == "1";
                    //
                    var status = jObj["message"]["messageTopic"]["recivestat"].ToString();
                    messageDto.FIsDelete = status == "2";
                    if (messageDto.FMessageFolder != (int)MessageFolder.Trash && status == "1")
                    {
                        messageDto.FMessageFolder = (int)MessageFolder.Trash;
                    }
                }
                else
                {
                    messageDto.FIsRead = jObj["message"]["messageTopic"]["senderreaded"].ToString() == "1";
                    messageDto.FIsMark = jObj["message"]["messageTopic"]["senderMarked"].ToString() == "1";
                    //
                    var status = jObj["message"]["messageTopic"]["senderstat"].ToString();
                    messageDto.FIsDelete = status == "2";
                    if (messageDto.FMessageFolder != (int)MessageFolder.Trash && status == "1")
                    {
                        messageDto.FMessageFolder = (int)MessageFolder.Trash;
                    }
                }
                //
                var lastReplyTimeStr = jObj["message"]["messageTopic"]["lastreplytime"].ToString();
                if (DateTime.TryParse(lastReplyTimeStr, out var lastReplyTime))
                {
                    messageDto.FLastReplyTime = lastReplyTime.ToUniversalTime();
                }
                else
                {
                    messageDto.FLastReplyTime = ConvertStringToDateTime(lastReplyTimeStr).ToUniversalTime();
                }
                //
                messageDto.FReplyCount = int.Parse(jObj["message"]["messageTopic"]["msgreplycount"].ToString());
                //
                var detailList = new List<YQTrack.Backend.DHgate.Model.DHgateMessage>();
                foreach (var m in jObj["message"]["messageInfo"].ToArray())
                {
                    var dHgateMessage = new YQTrack.Backend.DHgate.Model.DHgateMessage
                    {
                        content = m["content"].ToString(),
                        senderNickName = m["senderNickname"].ToString(),
                        attNames = m["attatchment0"].ToString(),
                        attUrls = m["attatchment"].ToString()
                    };
                    //
                    var createTimeStr = m["createtime"].ToString();
                    if (DateTime.TryParse(createTimeStr, out var createTime))
                    {
                        dHgateMessage.createTime = createTime.ToUniversalTime();
                    }
                    else
                    {
                        dHgateMessage.createTime = ConvertStringToDateTime(createTimeStr).ToUniversalTime();
                    }
                    //
                    if (!string.IsNullOrEmpty(dHgateMessage.senderNickName))
                    {
                        detailList.Add(dHgateMessage);
                    }
                }
                //
                if (detailList.Any())
                {
                    detailList.OrderByDescending(a => a.createTime);
                    messageDto.FIsReply = detailList.First().senderNickName.Trim() == shopName.Trim();
                }
                //
                var dHgateMessageBll = FactoryContainer.Create<IDHgateMessageBll>();
                dHgateMessageBll.SetDataRoute(syncContext.DataRouteModel);
                dHgateMessageBll.UpdateMessageStatus(messageDto);
            }
        }
        #endregion

        #region GetProduct

        /// <summary>
        /// GetProduct
        /// </summary>
        /// <param name="syncContext"></param>
        /// <param name="itemId"></param>
        /// <returns></returns>
        private static async Task<IEnumerable<OrderProductModel>> GetProduct(SyncContext syncContext, string itemId)
        {
            var result = new List<OrderProductModel>();
            var productString = await GetProductResult(syncContext, itemId);
            if (string.IsNullOrEmpty(productString))
            {
                return result;
            }
            //
            try
            {
                var jObject = JObject.Parse(productString);
                if (jObject["itemCode"] != null && jObject["itemCode"].Count() > 0)
                {
                    result.Add(new OrderProductModel
                    {
                        ItemId = jObject["itemCode"].ToString(),
                        ItemTitle = jObject["itemBase"]?["itemName"]?.ToString(),
                        ItemImage = jObject["itemImgList"]?.ToArray()?.First()?["imgUrl"].ToString()
                    });
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogObj(LogDefine.SyncWarn, ex, new
                {
                    syncContext.DHgateShopDto.FShopId,
                    itemId,
                    productString
                });
            }
            //
            return result;
        }

        /// <summary>
        /// GetProductResult
        /// </summary>
        /// <param name="syncContext"></param>
        /// <param name="itemId"></param>
        /// <returns></returns>
        private static async Task<string> GetProductResult(SyncContext syncContext, string itemId)
        {
            var result = string.Empty;
            var task = new
            {
                syncContext.DHgateShopDto.FUserId,
                syncContext.DHgateShopDto.FShopId,
                syncContext.DHgateShopDto.FShopName,
                syncContext.DHgateShopDto.BeforeDays,
                itemId
            };

            //设置重试
            var policy = Policy.Handle<Exception>().RetryAsync(5, (exception, retryCount, context) =>
            {
                LogHelper.LogObj(LogDefine.SyncWarn, exception,
                    new { task, methodName = context["methodName"], errorMsg = exception.Message });
            });
            //
            await policy.ExecuteAsync(async (t) =>
            {
                return await GetProductLoop(syncContext, itemId);
            },
                      new Dictionary<string, object>() { { "methodName", "GetProduct" } }
                  ).ContinueWith(t =>
                  {
                      if (t.Exception != null)
                      {
                          LogHelper.LogObj(LogDefine.SyncWarn, t.Exception.InnerException, task);
                      }
                      result = t.Result;
                  });
            return result;
        }


        /// <summary>
        /// GetProductLoop
        /// </summary>
        /// <param name="syncContext"></param>
        /// <param name="orderNo"></param>
        /// <returns></returns>
        private static async Task<string> GetProductLoop(SyncContext syncContext, string orderNo)
        {
            var result = await _DHgateService.GetProduct(orderNo, syncContext.Token);
            if (result.ErrorCode == "00000001")
            {
                return null;
            }
            //
            if (!result.IsSuccess || string.IsNullOrEmpty(result.ResultData))
            {
                //重试
                throw new Exception(result.ErrorMsg);
            }
            //
            if (!string.IsNullOrEmpty(result.ErrorMsg))
            {
                LogHelper.LogObj(LogDefine.SyncWarn, new
                {
                    method = "GetProduct",
                    syncContext.DHgateShopDto.FUserId,
                    syncContext.DHgateShopDto.FShopId,
                    syncContext.DHgateShopDto.FShopName,
                    syncContext.DHgateShopDto.BeforeDays,
                    orderNo,
                    result.ErrorMsg
                });
            }
            //
            return result.ResultData;
        }

        #endregion

        #region Other
        /// <summary>
        /// ReturnMessageFolder
        /// </summary>
        /// <param name="msgType"></param>
        /// <returns></returns>
        private static int ReturnMessageFolder(string msgType)
        {
            var result = 0;
            switch (msgType)
            {
                //001,002,003
                case "001":
                case "002":
                case "003":
                    result = (int)MessageFolder.BuyerMessage;
                    break;
                //004,005,006,007,008,009
                case "004":
                case "005":
                case "006":
                case "007":
                case "008":
                case "009":
                    result = (int)MessageFolder.SystemMessage;
                    break;
                //010,011,012,013
                case "010":
                case "011":
                case "012":
                case "013":
                    result = (int)MessageFolder.SiteNotice;
                    break;
            }
            return result;
        }

        /// <summary>        
        /// 时间戳转为C#格式时间        
        /// </summary>        
        /// <param name="timeStamp"></param>        
        /// <returns></returns>        
        private static DateTime ConvertStringToDateTime(string timeStamp)
        {
            DateTime dtStart = TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1));
            long lTime = long.Parse(timeStamp + "0000");
            TimeSpan toNow = new TimeSpan(lTime);
            return dtStart.Add(toNow);
        }

        /// <summary>
        /// GetToken
        /// </summary>
        /// <param name="tokenString"></param>
        private static string GetToken(string tokenString)
        {
            var token = JsonConvert.DeserializeObject<Dictionary<string, object>>(tokenString);
            return token["access_token"].ToString();
        }
        #endregion

        #endregion
    }
}
