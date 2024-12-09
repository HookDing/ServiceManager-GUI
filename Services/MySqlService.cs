using System.Data;
using System.ServiceProcess;
using MySql.Data.MySqlClient;

namespace ServiceManagerGUI.Services
{
    public class MySqlService : ServiceBase
    {
        private readonly string connectionString;
        private MySqlConnection? connection;
        private readonly LogManager logManager;
        private System.Timers.Timer heartbeatTimer;
        private const string EventLogSource = "MySqlService";
        private const string EventLogName = "MySqlService";
        private readonly object lockObj = new object();
        private bool isConnected;

        public MySqlService()
        {
            ServiceName = "MySqlTestService";
            logManager = new LogManager(EventLogSource, EventLogName);
            connectionString = "Server=localhost;Database=testdb;Uid=root;Pwd=yourpassword;";
            isConnected = false;
        }

        protected override void OnStart(string[] args)
        {
            try
            {
                // 启动心跳检测
                heartbeatTimer = new System.Timers.Timer();
                heartbeatTimer.Interval = 30000; // 每30秒检查一次连接
                heartbeatTimer.Elapsed += HeartbeatTimer_Elapsed;
                heartbeatTimer.Start();

                // 初始连接
                ConnectToDatabase();
                logManager.WriteServiceStarted();
            }
            catch (Exception ex)
            {
                logManager.WriteError($"启动服务失败: {ex.Message}");
                throw;
            }
        }

        private void HeartbeatTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                if (!CheckConnection())
                {
                    logManager.WriteWarning("数据库连接已断开，尝试重新连接");
                    ConnectToDatabase();
                }
            }
            catch (Exception ex)
            {
                logManager.WriteError($"心跳检测失败: {ex.Message}");
            }
        }

        private bool CheckConnection()
        {
            if (connection == null) return false;

            try
            {
                using var cmd = connection.CreateCommand();
                cmd.CommandText = "SELECT 1";
                cmd.ExecuteScalar();
                return true;
            }
            catch
            {
                return false;
            }
        }

        private void ConnectToDatabase()
        {
            lock (lockObj)
            {
                try
                {
                    if (connection == null)
                    {
                        connection = new MySqlConnection(connectionString);
                    }

                    if (!isConnected)
                    {
                        connection.Open();
                        isConnected = true;
                        logManager.WriteInformation("MySQL数据库连接成功");
                    }
                }
                catch (Exception ex)
                {
                    isConnected = false;
                    logManager.WriteError($"MySQL数据库连接失败: {ex.Message}");
                    throw;
                }
            }
        }

        public DataTable ExecuteQuery(string sql, Dictionary<string, object>? parameters = null)
        {
            lock (lockObj)
            {
                try
                {
                    if (!isConnected)
                    {
                        ConnectToDatabase();
                    }

                    using var command = new MySqlCommand(sql, connection);
                    if (parameters != null)
                    {
                        foreach (var param in parameters)
                        {
                            command.Parameters.AddWithValue(param.Key, param.Value);
                        }
                    }

                    var dataTable = new DataTable();
                    using var adapter = new MySqlDataAdapter(command);
                    adapter.Fill(dataTable);
                    logManager.WriteInformation($"执行查询成功: {sql}");
                    return dataTable;
                }
                catch (Exception ex)
                {
                    logManager.WriteError($"执行查询失败: {ex.Message}");
                    throw;
                }
            }
        }

        public int ExecuteNonQuery(string sql, Dictionary<string, object>? parameters = null)
        {
            lock (lockObj)
            {
                try
                {
                    if (!isConnected)
                    {
                        ConnectToDatabase();
                    }

                    using var command = new MySqlCommand(sql, connection);
                    if (parameters != null)
                    {
                        foreach (var param in parameters)
                        {
                            command.Parameters.AddWithValue(param.Key, param.Value);
                        }
                    }

                    int result = command.ExecuteNonQuery();
                    logManager.WriteInformation($"执行非查询操作成功: {sql}");
                    return result;
                }
                catch (Exception ex)
                {
                    logManager.WriteError($"执行非查询操作失败: {ex.Message}");
                    throw;
                }
            }
        }

        protected override void OnStop()
        {
            try
            {
                heartbeatTimer?.Stop();
                if (connection != null)
                {
                    if (connection.State == ConnectionState.Open)
                    {
                        connection.Close();
                    }
                    connection.Dispose();
                }
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
                heartbeatTimer?.Dispose();
                connection?.Dispose();
                logManager?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
} 