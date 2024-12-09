using System.Diagnostics;
using System.ServiceProcess;

namespace ServiceManagerGUI.Services
{
    public class ServiceManager
    {
        private readonly string serviceName;
        private readonly string servicePath;
        private readonly string displayName;

        public ServiceManager(string serviceName, string servicePath, string displayName)
        {
            this.serviceName = serviceName;
            this.servicePath = servicePath;
            this.displayName = displayName;
        }

        public bool IsServiceExists()
        {
            try
            {
                ServiceController[] services = ServiceController.GetServices();
                return services.Any(s => s.ServiceName.Equals(serviceName, StringComparison.OrdinalIgnoreCase));
            }
            catch
            {
                return false;
            }
        }

        public bool IsServiceRunning()
        {
            try
            {
                if (!IsServiceExists()) return false;

                using var service = new ServiceController(serviceName);
                return service.Status == ServiceControllerStatus.Running;
            }
            catch
            {
                return false;
            }
        }

        private ServiceController GetServiceController()
        {
            if (!IsServiceExists())
                throw new InvalidOperationException("服务不存在");

            return new ServiceController(serviceName);
        }

        public (bool success, string message) InstallService()
        {
            try
            {
                using var process = new Process();
                process.StartInfo = CreateStartInfo("sc.exe", 
                    $"create {serviceName} binPath= \"{servicePath}\" start= auto DisplayName= \"{displayName}\"");
                process.Start();
                process.WaitForExit();

                return process.ExitCode == 0 
                    ? (true, "服务安装成功") 
                    : (false, "服务安装失败");
            }
            catch (Exception ex)
            {
                return (false, $"安装服务失败：{ex.Message}");
            }
        }

        public (bool success, string message) UninstallService()
        {
            try
            {
                using var process = new Process();
                process.StartInfo = CreateStartInfo("sc.exe", $"delete {serviceName}");
                process.Start();
                process.WaitForExit();

                return process.ExitCode == 0 
                    ? (true, "服务卸载成功") 
                    : (false, "服务卸载失败");
            }
            catch (Exception ex)
            {
                return (false, $"卸载服务失败：{ex.Message}");
            }
        }

        private ProcessStartInfo CreateStartInfo(string fileName, string arguments)
        {
            return new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                Verb = "runas",
                UseShellExecute = true,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };
        }

        public (bool success, string message) StartService()
        {
            try
            {
                if (!IsServiceExists())
                {
                    return (false, "服务未安装，请先安装服务");
                }

                using var service = new ServiceController(serviceName);
                if (service.Status != ServiceControllerStatus.Running)
                {
                    using var process = new Process();
                    process.StartInfo = CreateStartInfo("net", $"start {serviceName}");
                    process.Start();
                    process.WaitForExit();

                    return process.ExitCode == 0 
                        ? (true, "服务已启动") 
                        : (false, "服务启动失败");
                }

                return (true, "服务已在运行");
            }
            catch (Exception ex)
            {
                return (false, $"启动服务失败：{ex.Message}");
            }
        }

        public (bool success, string message) StopService()
        {
            try
            {
                using var service = new ServiceController(serviceName);
                if (service.Status == ServiceControllerStatus.Running)
                {
                    using var process = new Process();
                    process.StartInfo = CreateStartInfo("net", $"stop {serviceName}");
                    process.Start();
                    process.WaitForExit();

                    return process.ExitCode == 0 
                        ? (true, "服务已停止") 
                        : (false, "服务停止失败");
                }

                return (true, "服务未在运行");
            }
            catch (Exception ex)
            {
                return (false, $"停止服务失败：{ex.Message}");
            }
        }

        public (bool success, string message) RestartService()
        {
            try
            {
                if (!IsServiceExists())
                {
                    return (false, "服务未安装，请先安装服务");
                }

                var stopResult = StopService();
                if (!stopResult.success)
                {
                    return stopResult;
                }

                Thread.Sleep(2000); // 等待服务完全停止
                return StartService();
            }
            catch (Exception ex)
            {
                return (false, $"重启服务失败：{ex.Message}");
            }
        }
    }
} 