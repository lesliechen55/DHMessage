using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using YQTrackV6.Common.Windows;
using YQTrackV6.Log;

namespace YQTrack.Backend.DHgateMessage.Host
{
    public partial class MainForm : PrintMessageForm
    {
        DHgateMessageScheduleHelper _DHgateMessageScheduleHelper;

        public MainForm()
        {
            InitializeComponent();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            //
            _DHgateMessageScheduleHelper = new DHgateMessageScheduleHelper();
            _DHgateMessageScheduleHelper.OnJobEvent += _DHgateMessageScheduleHelper_OnJobEvent;
            try
            {
                _DHgateMessageScheduleHelper.StartService();
                //
                MyPrintMessage("服务启动完成!");
            }
            catch (Exception exp)
            {
                LogHelper.LogObj(new LogDefinition(LogLevel.Fatal, "ServerStartException"), exp);
                Environment.Exit(-1);
            }
        }

        /// <summary>
        /// JobEvent
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _DHgateMessageScheduleHelper_OnJobEvent(object sender, JobEventArgs e)
        {
            MyPrintMessage($"{e.JobInfo.EventName}:{e.JobInfo.EventDesc}");
        }

        protected override string MyReadParmsInfo()
        {
            return $"进程号: {Process.GetCurrentProcess().Id}";
        }

        #region 重载
        /// <summary>
        /// 功能列表
        /// </summary>
        /// <returns></returns>
        protected override IList<ActionItem> MyActionItemList()
        {
            var list = new List<ActionItem> { new ActionItem("重新启动", AutoReload) };
            return list;
        }
        /// <summary>
        /// 退出
        /// </summary>
        protected override void MyExit()
        {
            if (_DHgateMessageScheduleHelper != null)
            {
                _DHgateMessageScheduleHelper.StopService();
                _DHgateMessageScheduleHelper = null;
            }
            Environment.Exit(0);
        }

        /// <summary>
        /// 重新启动
        /// </summary>
        private void AutoReload()
        {
            if (MessageBox.Show(@"确定重新启动当前服务？", @"重启", MessageBoxButtons.OKCancel) == DialogResult.OK)
            {
                if (_DHgateMessageScheduleHelper != null)
                {
                    _DHgateMessageScheduleHelper.StopService();
                    _DHgateMessageScheduleHelper = null;
                }
                //
                string runPath = AppDomain.CurrentDomain.BaseDirectory;
                string appName = AppDomain.CurrentDomain.FriendlyName;

                var info = new ProcessStartInfo
                {
                    UseShellExecute = true,
                    WorkingDirectory = runPath,
                    FileName = appName,
                    Arguments = "",
                };
                Process p = Process.Start(info);
                //
                Thread.Sleep(3000);
                Dispose(true);
                Environment.Exit(0);
            }
        }

        #endregion
    }
}
