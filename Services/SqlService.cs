using System.Data;
using System.Data.SqlClient;

namespace ServiceManagerGUI.Services
{
    public class SqlService : IDisposable
    {
        private readonly string connectionString;
        private SqlConnection? connection;
        private readonly LogManager logManager;
        private bool isConnected;
        private readonly object lockObj = new object();

        public SqlService(string connectionString, LogManager logManager)
        {
            this.connectionString = connectionString;
            this.logManager = logManager;
            isConnected = false;
        }

        public async Task ConnectAsync()
        {
            try
            {
                if (connection == null)
                {
                    connection = new SqlConnection(connectionString);
                }

                if (!isConnected)
                {
                    await connection.OpenAsync();
                    isConnected = true;
                    logManager.WriteInformation("数据库连接成功");
                }
            }
            catch (Exception ex)
            {
                logManager.WriteError($"数据库连接失败: {ex.Message}");
                throw;
            }
        }

        public async Task<DataTable> ExecuteQueryAsync(string sql, Dictionary<string, object>? parameters = null)
        {
            lock (lockObj)
            {
                try
                {
                    if (!isConnected)
                    {
                        ConnectAsync().Wait();
                    }

                    using var command = new SqlCommand(sql, connection);
                    if (parameters != null)
                    {
                        foreach (var param in parameters)
                        {
                            command.Parameters.AddWithValue(param.Key, param.Value);
                        }
                    }

                    var dataTable = new DataTable();
                    using var adapter = new SqlDataAdapter(command);
                    adapter.Fill(dataTable);
                    return dataTable;
                }
                catch (Exception ex)
                {
                    logManager.WriteError($"执行SQL查询失败: {ex.Message}");
                    throw;
                }
            }
        }

        public async Task<int> ExecuteNonQueryAsync(string sql, Dictionary<string, object>? parameters = null)
        {
            lock (lockObj)
            {
                try
                {
                    if (!isConnected)
                    {
                        ConnectAsync().Wait();
                    }

                    using var command = new SqlCommand(sql, connection);
                    if (parameters != null)
                    {
                        foreach (var param in parameters)
                        {
                            command.Parameters.AddWithValue(param.Key, param.Value);
                        }
                    }

                    return command.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    logManager.WriteError($"执行SQL非查询操作失败: {ex.Message}");
                    throw;
                }
            }
        }

        public void Dispose()
        {
            if (connection != null)
            {
                if (connection.State == ConnectionState.Open)
                {
                    connection.Close();
                }
                connection.Dispose();
            }
        }
    }
} 