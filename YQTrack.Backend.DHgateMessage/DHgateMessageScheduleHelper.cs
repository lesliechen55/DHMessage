using FluentScheduler;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using YQTrack.Backend.DHgateMessage.PerfCounter;
using YQTrack.Backend.Factory;
using YQTrack.Backend.Models;
using YQTrack.Backend.Models.Enums;
using YQTrack.Backend.ThirdPlatform.IBLL;
using YQTrack.Backend.Sharding;
using YQTrack.Configuration;
using YQTrackV6.Log;
using YQTrack.Backend.ThirdPlatform.Entity;
using YQTrack.Backend.ThirdPlatform.DTO;
using YQTrack.Backend.Enums;
using YQTrack.Schedule;
using System.Collections.Concurrent;
using YQTrack.Backend.DHgateMessage.Model;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace YQTrack.Backend.DHgateMessage
{
    public class DHgateMessageScheduleHelper
    {
        /// <summary>
        /// 调度间隔（毫秒）
        /// </summary>
        const int Interval = 1 * 60 * 1000;
        /// <summary>
        /// 心跳间隔（秒）
        /// </summary>
        const int HeartbeatInterval = 30;

        const int BeforeDays = 365;

        const int TotalTask = 5;

        /// <summary>
        /// 任务调度控制
        /// </summary>
        TaskCoordinator _TaskCoordinator = null;
        /// <summary>
        /// 心跳定时器
        /// </summary>
        Timer _HeartbeatTimer;
        //
        ConcurrentDictionary<string, DataRouteModel> _RouteDictionary = new ConcurrentDictionary<string, DataRouteModel>();
        //
        ConcurrentDictionary<string, ScheduleTask> _ScheduleTaskDictionary = new ConcurrentDictionary<string, ScheduleTask>();
        //
        public delegate void JobEvent(object sender, JobEventArgs e);
        public event JobEvent OnJobEvent;

        /// <summary>
        /// 开始调度服务
        /// </summary>
        public void StartService()
        {
            //PerfCounterSchedule
            PerfCounterSchedule.Default.Init();

            //
            ConfigManager.Initialize(typeof(DefaultConfig));
            DBShardingRouteFactory.InitDBShardingConfig(ConfigManager.GetConfig<DefaultConfig>().SellerDBShardingConfig);
            DBShardingRouteFactory.InitDBShardingConfig(ConfigManager.GetConfig<DefaultConfig>().SellerMessageDBShardingConfig);
            LogHelper.LogObj(LogDefine.InitializeSchedule, nameof(DBShardingRouteFactory));
            //
            DHgateMessageTaskHelper.Init();

            //所有可用的库
            var configNodes = DBShardingRouteFactory.GetDBTypeRule(YQDbType.Seller.ToString());
            foreach (var nodeInfo in configNodes.NodeRoutes)
            {
                foreach (var dbInfo in nodeInfo.Value.DBRules)
                {
                    if (dbInfo.Value.IsArchived)
                        continue;
                    //
                    foreach (var tableInfo in dbInfo.Value.TableRoutes)
                    {
                        var dataRoute = new DataRouteModel { NodeId = (byte)nodeInfo.Key, DbNo = (byte)dbInfo.Key, TableNo = tableInfo.TableNo, UserRole = (byte)UserRoleType.Seller };
                        _RouteDictionary[$"{nodeInfo.Key}.{dbInfo.Key}.{tableInfo.TableNo}"] = dataRoute;
                    }
                }
            }
            //
            _HeartbeatTimer = new Timer(DoHearbeatTimer);
            _TaskCoordinator = new TaskCoordinator(ConfigManager.GetConfig<DefaultConfig>().ScheduleSellerDHgateMessage);
            _TaskCoordinator.OnTaskAssigned += TaskCoordinator_OnTaskAssigned;
            _TaskCoordinator.OnTaskCanceled += TaskCoordinator_OnTaskCanceled;
            //Register
            _TaskCoordinator.Register(_RouteDictionary.Keys);
            //
            _HeartbeatTimer.Change(TimeSpan.FromSeconds(HeartbeatInterval), TimeSpan.FromMilliseconds(-1));

            //定时任务
            JobManager.JobStart += JobManager_JobStart;
            JobManager.JobEnd += JobManager_JobEnd;
            JobManager.JobException += JobManager_JobException;
        }

        /// <summary>
        /// 停止调度服务
        /// </summary>
        /// <returns></returns>
        public void StopService()
        {
            //
            _TaskCoordinator.UnRegister();
            //
            LogHelper.LogObj(LogDefine.ServerStopping, nameof(DHgateMessageTaskHelper));
            //
            while (_ScheduleTaskDictionary.Values.Any(t => t.Task.Status == TaskStatus.Running))
            {
                LogHelper.LogObj(LogDefine.WaitForExit, nameof(DHgateMessageTaskHelper));
                Task.Delay(10000).Wait();
            }
            LogHelper.LogObj(LogDefine.ServerStopped, nameof(DHgateMessageTaskHelper));
        }


        #region TaskCoordinator

        /// <summary>
        /// TaskAssigned
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TaskCoordinator_OnTaskAssigned(object sender, TaskAssignEventArgs e)
        {
            LogHelper.LogObj(
                new LogDefinition(LogLevel.Debug, "TaskCoordinator_OnTaskAssigned"), e);
            //
            if (_ScheduleTaskDictionary.ContainsKey(e.TaskInfo.TaskId))
            {
                LogHelper.LogObj(
                    new LogDefinition(LogLevel.Error, "TaskCoordinator_OnTaskAssigned:存在任务"), e);
                return;
            }
            //创建调度任务
            var jobInfo = new JobInfo
            {
                EventName = "TaskCoordinator_OnTaskAssigned",
                EventDesc = e.TaskInfo.TaskId
            };
            var route = _RouteDictionary[e.TaskInfo.TaskId];
            //
            var taskList = new List<TDHgateMsgSyncCtrl>();
            if (!string.IsNullOrEmpty(e.TaskInfo.TaskExtra))
            {
                try
                {
                    taskList = JsonConvert.DeserializeObject<List<TDHgateMsgSyncCtrl>>(e.TaskInfo.TaskExtra);
                }
                catch (Exception ex)
                {
                    LogHelper.LogObj(
                    new LogDefinition(LogLevel.Error, "AddTaskError"), ex, e.TaskInfo);
                }
            }
            //
            var cancellationTokenSource = new CancellationTokenSource();
            var scheduleTask = new ScheduleTask
            {
                Task = CreateService(route, cancellationTokenSource.Token, taskList),
                CancelTokenSource = cancellationTokenSource
            };
            _ScheduleTaskDictionary[e.TaskInfo.TaskId] = scheduleTask;
            //
            OnJobEvent?.Invoke(this, new JobEventArgs(jobInfo));
            LogHelper.LogObj(LogDefine.ServerStarted, jobInfo);
            //初始化定时任务
            var job = new AllMessageScheduleHelper
            {
                DataRouteModel = route
            };
            var hour = 0;
            var min = 0;
            if (_ScheduleTaskDictionary.Count <= 0)
            {
                var registry = new JobRegistry();
                registry.Schedule(job).WithName(e.TaskInfo.TaskId).ToRunEvery(1).Days().At(hour, min);
                JobManager.UseUtcTime();
                JobManager.Initialize(registry);
            }
            else
            {
                JobManager.UseUtcTime();
                JobManager.AddJob(job, (s) => s.WithName(e.TaskInfo.TaskId).ToRunEvery(1).Days().At(hour, min));
            }
            //
            jobInfo = new JobInfo
            {
                EventName = "JobManager_AddJob",
                EventDesc = e.TaskInfo.TaskId
            };
            OnJobEvent?.Invoke(this, new JobEventArgs(jobInfo));
            //
            LogHelper.LogObj(LogDefine.AddJob, jobInfo);
        }

        /// <summary>
        /// TaskCanceled
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TaskCoordinator_OnTaskCanceled(object sender, TaskAssignEventArgs e)
        {
            LogHelper.LogObj(
                new LogDefinition(LogLevel.Debug, "TaskCoordinator_OnTaskCanceled"), e);
            //
            if (!_ScheduleTaskDictionary.ContainsKey(e.TaskInfo.TaskId))
            {
                LogHelper.LogObj(
                    new LogDefinition(LogLevel.Debug, "TaskCoordinator_OnTaskCanceled:不存在任务"), e);
            }
            //
            var task = _ScheduleTaskDictionary[e.TaskInfo.TaskId];
            task.CancelTokenSource.Cancel();
            while (task.Task.Status == TaskStatus.Running)
            {
                LogHelper.LogObj(LogDefine.WaitForExit, new { e.TaskInfo.TaskId });
                Task.Delay(10000).Wait();
            }
            //
            var route = _RouteDictionary[e.TaskInfo.TaskId];
            ClearTask(route);
            //
            var jobInfo = new JobInfo
            {
                EventName = "TaskCoordinator_OnTaskCanceled",
                EventDesc = e.TaskInfo.TaskId
            };
            OnJobEvent?.Invoke(this, new JobEventArgs(jobInfo));
            try
            {
                JobManager.RemoveJob(e.TaskInfo.TaskId);

                //
                jobInfo = new JobInfo
                {
                    EventName = "JobManager_RemoveJob",
                    EventDesc = e.TaskInfo.TaskId
                };
                OnJobEvent?.Invoke(this, new JobEventArgs(jobInfo));
                LogHelper.LogObj(LogDefine.RemoveJob, jobInfo);
            }
            catch (Exception ex)
            {
                LogHelper.LogObj(LogDefine.RemoveJobException, ex, jobInfo);
            }
        }

        /// <summary>
        /// 心跳定时处理
        /// </summary>
        /// <param name="state"></param>
        private void DoHearbeatTimer(object state)
        {
            try
            {
                var list = _TaskCoordinator.Tasks;
                if (list.Count() == 0)
                {
                    LogHelper.Log(LogDefine.Heartbeat);
                    return;
                }
                foreach (var item in list)
                {
                    var runTaskList = new List<TDHgateMsgSyncCtrl>();
                    //var route = _RouteDictionary[item.TaskId];
                    //var dHgateMessageBll = FactoryContainer.Create<IDHgateMessageBll>();
                    //dHgateMessageBll.SetDataRoute(route);

                    //var runTaskList = dHgateMessageBll.GetSyncTaskList();
                    //LogHelper.LogObj(new LogDefinition(LogLevel.Debug, "Hearbeat_TaskList"), new { item.TaskId, Task = runTaskList.Select(a => a.FShopId) });
                    //传递数据
                    _TaskCoordinator.Hearbeat(item.TaskId, JsonConvert.SerializeObject(runTaskList));
                }
            }
            catch (Exception exp)
            {
                LogHelper.LogObj(LogDefine.HearbeatException, exp);
            }
            finally
            {
                _HeartbeatTimer.Change(TimeSpan.FromSeconds(HeartbeatInterval), TimeSpan.FromMilliseconds(-1));
            }
        }
        #endregion


        #region Job

        private void JobManager_JobException(JobExceptionInfo obj)
        {
            LogHelper.LogObj(new LogDefinition(LogLevel.Error, "JobManager_JobException"),
                obj.Exception, obj);
        }

        private void JobManager_JobEnd(JobEndInfo obj)
        {
            LogHelper.LogObj(new LogDefinition(LogLevel.Info, "JobManager_JobEnd"), obj);
        }

        private void JobManager_JobStart(JobStartInfo obj)
        {
            LogHelper.LogObj(new LogDefinition(LogLevel.Info, "JobManager_JobStart"), obj);
        }

        #endregion
        /// <summary>
        /// CreateService
        /// </summary>
        /// <param name="dataRoute"></param>
        /// <returns></returns>
        private async Task CreateService(DataRouteModel dataRoute, CancellationToken cancelToken, IEnumerable<TDHgateMsgSyncCtrl> taskList)
        {
            ConcurrentDictionary<long, long> shopDictionary = new ConcurrentDictionary<long, long>();
            var dHgateMessageBll = FactoryContainer.Create<IDHgateMessageBll>();
            dHgateMessageBll.SetDataRoute(dataRoute);
            while (true && !cancelToken.IsCancellationRequested)
            {
                int success = 0;
                int failure = 0;
                try
                {
                    var list = (await dHgateMessageBll.GetDHgateShopList()).ToList();

                    if (list.Any() && shopDictionary.Count < TotalTask)
                    {
                        var take = TotalTask - shopDictionary.Count;
                        if (take > 0)
                        {
                            var runList = list.Take(take);
                            try
                            {
                                Parallel.ForEach(runList, async (item) =>
                                {
                                    if (shopDictionary.ContainsKey(item.FShopId))
                                    {
                                        return;
                                    }

                                    if (shopDictionary.Count >= TotalTask)
                                    {
                                        return;
                                    }

                                    shopDictionary[item.FShopId] = item.FShopId;

                                    var addTaskResult = AddTask(item, dataRoute);

                                    if (addTaskResult)
                                    {
                                        var isAll = item.BeforeDays == 365;
                                        var taskName = isAll ? "GetMessageListAll" : "GetMessageList";
                                        var task = new
                                        {
                                            taskName,
                                            item.FUserId,
                                            item.FShopId,
                                            item.FShopName,
                                            item.BeforeDays
                                        };

                                        await Task.Run(async () =>
                                        {
                                            var syncResult = default(bool);
                                            try
                                            {
                                                LogHelper.LogObj(LogDefine.SyncStart, task);
        
                                                syncResult = await DHgateMessageTaskHelper.SyncMessage(item, dataRoute, isAll);

                                                //await DHgateMessageTaskHelper.SyncTrachMessage(item, dataRoute, isAll);//获取站内信垃圾箱数据

                                                if (isAll)
                                                {
                                                    await DHgateMessageTaskHelper.SyncOtherMessage(item, dataRoute);
                                                    await DHgateMessageTaskHelper.MessageLinkOrder(item, dataRoute);
                                                }
                                            }
                                            catch (Exception ex)
                                            {
                                                LogHelper.LogObj(LogDefine.SubmitException, ex, new
                                                {
                                                    task,
                                                    syncResult
                                                });
                                            }
                                            LogHelper.LogObj(LogDefine.SyncEnd, new
                                            {
                                                task,
                                                syncResult
                                            });
                                            return syncResult;

                                        }, cancelToken).ContinueWith(t =>
                                        {
                                            if (t != null && t.IsFaulted)
                                            {
                                                LogHelper.LogObj(LogDefine.SubmitException, t.Exception.InnerException, new
                                                {
                                                    task,
                                                    t.Result
                                                });
                                            }
                                            if (t.Exception != null)
                                            {
                                                LogHelper.LogObj(LogDefine.SubmitException, t.Exception.InnerException, new
                                                {
                                                    task,
                                                    t.Result
                                                });
                                            }

                                            if (t.IsCompleted)
                                            {
                                                LogHelper.LogObj(LogDefine.SubmitSuccess, new
                                                {
                                                    task,
                                                    t.Result
                                                });
                                            }
                                            //删除控制
                                            var deleteBll = FactoryContainer.Create<IDHgateMessageBll>();
                                            deleteBll.SetDataRoute(dataRoute);
                                            deleteBll.Delete(item.FShopId);

                                            if (!shopDictionary.TryRemove(item.FShopId, out var shopId))
                                            {
                                                LogHelper.LogObj(LogDefine.SyncError, new
                                                {
                                                    task,
                                                    Name = "RemoveError"
                                                });
                                            }

                                            if (t.Result)
                                            {
                                                success++;
                                                PerfCounterSchedule.Default.AddSuccess(dataRoute.DbNo.ToString());
                                            }
                                            else
                                            {
                                                failure++;
                                                PerfCounterSchedule.Default.AddFail(dataRoute.DbNo.ToString());
                                            }
                                        });
                                    }

                                });
                            }
                            catch (Exception ex)
                            {
                                LogHelper.LogObj(LogDefine.SubmitException, ex, dataRoute);
                            }
                        }
                    }

                    #region
                    /*
                    foreach (var item in list)
                    {
                        var addTaskResult = AddTask(item, dataRoute);
                        //
                        if (addTaskResult)
                        {
                            var task = new
                            {
                                item.FUserId,
                                item.FShopId,
                                item.FShopName,
                                item.BeforeDays
                            };
                            await Task.Run(async () =>
                            {
                                LogHelper.LogObj(LogDefine.SyncStart, new
                                {
                                    Name = "GetMessageList",
                                    task
                                });
                                var syncResult = await DHgateMessageTaskHelper.SyncMessage(item, dataRoute);
                                LogHelper.LogObj(LogDefine.SyncEnd, new
                                {
                                    Name = "GetMessageList",
                                    task,
                                    syncResult
                                });
                                return syncResult;
                            }, cancelToken).ContinueWith(t =>
                            {
                                if (t.Exception != null)
                                {
                                    LogHelper.LogObj(LogDefine.SubmitException, t.Exception.InnerException, new
                                    {
                                        task,
                                        t.Result
                                    });
                                }
                                //
                                if (t.IsCompleted)
                                {
                                    LogHelper.LogObj(LogDefine.SubmitSuccess, new
                                    {
                                        task,
                                        t.Result
                                    });
                                }
                                //删除控制
                                dHgateMessageBll.Delete(item.FShopId);
                                //
                                if (t.Result)
                                {
                                    success++;
                                    PerfCounterSchedule.Default.AddSuccess(dataRoute.DbNo.ToString());
                                }
                                else
                                {
                                    failure++;
                                    PerfCounterSchedule.Default.AddFail(dataRoute.DbNo.ToString());
                                }
                            });
                        }
                    }
                    */
                    #endregion
                }
                catch (Exception exp)
                {
                    LogHelper.LogObj(LogDefine.SubmitException, exp, dataRoute);
                }
                //等待下次开始调度
                LogHelper.LogObj(LogDefine.WaitNextSchedule, dataRoute);
                await Task.Delay(TimeSpan.FromMilliseconds(Interval));
            }
        }

        /// <summary>
        /// AddTask
        /// </summary>
        /// <param name="dHgateShopDto"></param>
        /// <param name="dataRoute"></param>
        /// <returns></returns>
        private bool AddTask(DHgateShopDto dHgateShopDto, DataRouteModel dataRoute)
        {
            var addTaskResult = false;

            var beforeDays = 1;
            if (dHgateShopDto.FNextMessageSyncingTime == null)
            {
                beforeDays = 365;
            }
            //
            var task = new TDHgateMsgSyncCtrl
            {
                FShopId = dHgateShopDto.FShopId,
                FUserId = dHgateShopDto.FUserId,
                FBeforeDays = beforeDays
            };
            try
            {
                var dHgateMessageBll = FactoryContainer.Create<IDHgateMessageBll>();
                dHgateMessageBll.SetDataRoute(dataRoute);
                addTaskResult = dHgateMessageBll.AddSyncTask(task);
                //
                dHgateShopDto.BeforeDays = beforeDays;
                //
                dHgateMessageBll.UpdateMessageSyncTime(task.FShopId);
            }
            catch (Exception exp)
            {
                LogHelper.LogObj(LogDefine.SubmitWarn, exp, new
                {
                    Name = "AddTask",
                    task,
                    addTaskResult
                });
                PerfCounterSchedule.Default.AddWarn(dataRoute.DbNo.ToString());
            }

            return addTaskResult;
        }

        /// <summary>
        /// ClearTask
        /// </summary>
        private void ClearTask(DataRouteModel route)
        {
            var dHgateMessageBll = FactoryContainer.Create<IDHgateMessageBll>();
            dHgateMessageBll.SetDataRoute(route);
            dHgateMessageBll.Delete(0);
        }
    }
}
