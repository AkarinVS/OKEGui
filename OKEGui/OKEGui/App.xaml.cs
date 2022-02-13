using OKEGui.Utils;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace OKEGui
{
    /// <summary>
    /// 程序接入点。使用静态构造器来执行程序开始前的检查和其他任务。
    /// 主界面的设计和逻辑请见 Gui/MainWindow
    /// </summary>
    public partial class App : Application
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        public const int WM_QUERYENDSESSION = 0x11;
        public const int WM_ENDSESSION = 0x16;

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool ShutdownBlockReasonCreate(IntPtr hWnd, [MarshalAs(UnmanagedType.LPWStr)] string reason);
        [DllImport("user32.dll", SetLastError = true)]
        static extern bool ShutdownBlockReasonDestroy(IntPtr hWnd);
        [DllImport("kernel32.dll")]
        static extern bool SetProcessShutdownParameters(uint dwLevel, uint dwFlags);

        private void App_SessionEnding(object sender, SessionEndingCancelEventArgs e)
        {

            IntPtr windowHandle = Process.GetCurrentProcess().MainWindowHandle;
            // Prevent windows shutdown
            ShutdownBlockReasonCreate(windowHandle, "干啥呢干啥呢，OKEGui还没关呢？");
            e.Cancel = true;

        }

        private void AppStartup(object sender, StartupEventArgs e)
        {
            if (EnvironmentChecker.CheckEnviornment())
            {
                Initializer.ConfigLogger();
                Initializer.WriteConfig();
                Initializer.ClearOldLogs();
                Initializer.CheckUpdate();
                Logger.Info("程序正常启动");
            }
            else
            {
                Environment.Exit(0);
            }
        }
    }
}
