using System.ServiceProcess;

namespace ServiceManagerGUI.Services
{
    public class TestLogService : ServiceBase
    {
        private System.Timers.Timer timer;
        private LogManager logManager;
        private SqlService sqlService;
        private HttpService httpService;
        private const string EventLogSource = "DotNetTestLog";
        private const string EventLogName = "DotNetTestLog";
        private readonly CancellationTokenSource cancellationTokenSource;
        private Task? httpTask;
        private Task? sqlTask;

        public TestLogService()
        {
            ServiceName = "DotNetTestLogService";
            logManager = new LogManager(EventLogSource, EventLogName);
            sqlService = new SqlService("Server=localhost;Database=YourDB;Trusted_Connection=True;", logManager);
            httpService = new HttpService("https://api.example.com", logManager);
            cancellationTokenSource = new CancellationTokenSource();
        }

        protected override void OnStart(string[] args)
        {
            try
            {
                // 原有的定时器线程
                timer = new System.Timers.Timer();
                timer.Interval = 60000; // 每分钟执行一次
                timer.Elapsed += Timer_Elapsed;
                timer.Start();

                // HTTP请求线程
                httpTask = Task.Run(async () =>
                {
                    while (!cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        try
                        {
                            var result = await httpService.GetAsync("/api/test");
                            logManager.WriteInformation($"HTTP请求结果: {result}");
                        }
                        catch (Exception ex)
                        {
                            logManager.WriteError($"HTTP请求线程错误: {ex.Message}");
                        }
                        await Task.Delay(30000, cancellationTokenSource.Token); // 30秒执行一次
                    }
                }, cancellationTokenSource.Token);

                // 数据库连接线程
                sqlTask = Task.Run(async () =>
                {
                    try
                    {
                        await sqlService.ConnectAsync();
                        while (!cancellationTokenSource.Token.IsCancellationRequested)
                        {
                            try
                            {
                                var result = await sqlService.ExecuteQueryAsync("SELECT GETDATE()");
                                logManager.WriteInformation($"数据库连接正常，当前时间: {result.Rows[0][0]}");
                            }
                            catch (Exception ex)
                            {
                                logManager.WriteError($"数据库操作错误: {ex.Message}");
                                await sqlService.ConnectAsync(); // 尝试重新连接
                            }
                            await Task.Delay(60000, cancellationTokenSource.Token); // 每分钟检查一次
                        }
                    }
                    catch (Exception ex)
                    {
                        logManager.WriteError($"数据库连接线程错误: {ex.Message}");
                    }
                }, cancellationTokenSource.Token);

                logManager.WriteServiceStarted();
            }
            catch (Exception ex)
            {
                logManager.WriteError($"启动服务时发生错误: {ex.Message}");
                throw;
            }
        }

        private void Timer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                logManager.WriteTestLog();
            }
            catch (Exception ex)
            {
                logManager.WriteError($"写入测试日志时发生错误: {ex.Message}");
            }
        }

        protected override void OnStop()
        {
            try
            {
                cancellationTokenSource.Cancel();
                timer?.Stop();
                Task.WhenAll(httpTask ?? Task.CompletedTask, sqlTask ?? Task.CompletedTask).Wait(5000);
                logManager.WriteServiceStopped();
            }
            catch (Exception ex)
            {
                logManager.WriteError($"停止服务时发生错误: {ex.Message}");
                throw;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                timer?.Dispose();
                logManager?.Dispose();
                sqlService?.Dispose();
                cancellationTokenSource?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
} 