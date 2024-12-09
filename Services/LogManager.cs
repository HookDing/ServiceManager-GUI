using System.Diagnostics;

namespace ServiceManagerGUI.Services
{
    public class LogManager : IDisposable
    {
        private readonly EventLog eventLog;
        private readonly string source;
        private readonly string logName;

        public LogManager(string source, string logName)
        {
            this.source = source;
            this.logName = logName;

            try
            {
                if (!EventLog.SourceExists(source))
                {
                    EventLog.CreateEventSource(source, logName);
                }

                eventLog = new EventLog
                {
                    Source = source,
                    Log = logName
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"初始化事件日志失败: {ex.Message}", ex);
            }
        }

        public void WriteInformation(string message)
        {
            try
            {
                eventLog.WriteEntry(message, EventLogEntryType.Information);
            }
            catch (Exception ex)
            {
                throw new Exception($"写入信息日志失败: {ex.Message}", ex);
            }
        }

        public void WriteWarning(string message)
        {
            try
            {
                eventLog.WriteEntry(message, EventLogEntryType.Warning);
            }
            catch (Exception ex)
            {
                throw new Exception($"写入警告日志失败: {ex.Message}", ex);
            }
        }

        public void WriteError(string message)
        {
            try
            {
                eventLog.WriteEntry(message, EventLogEntryType.Error);
            }
            catch (Exception ex)
            {
                throw new Exception($"写入错误日志失败: {ex.Message}", ex);
            }
        }

        public void WriteServiceStarted()
        {
            WriteInformation("服务已启动");
        }

        public void WriteServiceStopped()
        {
            WriteInformation("服务已停止");
        }

        public void WriteTestLog()
        {
            WriteInformation($"测试日志 - {DateTime.Now}");
        }

        public void Dispose()
        {
            eventLog?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
} 