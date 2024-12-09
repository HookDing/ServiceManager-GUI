using System.ServiceProcess;
using ServiceManagerGUI.Services;
using ServiceManagerGUI.Windows;

namespace ServiceManagerGUI
{
    internal static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            if (Environment.UserInteractive)
            {
                // 窗口模式
                ApplicationConfiguration.Initialize();
                Application.Run(new ServiceManagerForm());
            }
            else
            {
                // 服务模式
                ServiceBase[] ServicesToRun = new ServiceBase[]
                {
                    new TestLogService()
                };
                ServiceBase.Run(ServicesToRun);
            }
        }
    }
}