using Autofac;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using YQTrack.Backend.Factory;
using YQTrack.Backend.Models;
using YQTrackV6.Log;

namespace YQTrack.Backend.DHgateMessage.Host
{
    static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionTrapper;
            Application.ThreadException += Application_ThreadException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
            SetUnhandledExceptionFilter(Win32_UnhandledException);

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            var builder = new ContainerBuilder();
            var assemblys = Directory.GetFiles(Environment.CurrentDirectory, "*.dll").Select(Assembly.LoadFile);
            builder.RegisterAssemblyTypes(assemblys.ToArray())
                .Where(t => typeof(IDependency).IsAssignableFrom(t))
                .AsImplementedInterfaces();
            var container = builder.Build();
            FactoryContainer.Init(container);

            Application.Run(new MainForm());
        }

        /// <summary>
        /// 应用程序线程异常
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            LogHelper.Log(new LogDefinition(LogLevel.Error, "Application_ThreadException"), e.Exception);
        }

        /// <summary>
        /// 应用未处理的异常
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void UnhandledExceptionTrapper(object sender, UnhandledExceptionEventArgs e)
        {
            LogHelper.LogObj(new LogDefinition(LogLevel.Error, "UnhandledExceptionTrapper"), e.ExceptionObject);
        }

        /// <summary>
        /// 任务线程未被关注的异常
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            LogHelper.Log(new LogDefinition(LogLevel.Error, "TaskScheduler_UnobservedTaskException"), e.Exception);
            e.Exception.Handle(error => true);
            e.SetObserved();
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int FilterDelegate(IntPtr exceptionPointers);
        [DllImport("kernel32.dll")]
        private static extern FilterDelegate SetUnhandledExceptionFilter(FilterDelegate lpTopLevelExceptionFilter);
        private static int Win32_UnhandledException(IntPtr nope)
        {
            //AppDomain_UnhandledException将在此之前执行,如需调试必须注意
            LogHelper.Log(new LogDefinition(LogLevel.Fatal, "程序崩溃"));
            Environment.Exit(0);
            return 1;
        }
    }
}
