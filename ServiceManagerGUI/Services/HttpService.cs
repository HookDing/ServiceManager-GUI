using System.Net.Http;
using System.Text.Json;

namespace ServiceManagerGUI.Services
{
    public class HttpService
    {
        private readonly HttpClient httpClient;
        private readonly LogManager logManager;
        private readonly string baseUrl;

        public HttpService(string baseUrl, LogManager logManager)
        {
            this.baseUrl = baseUrl;
            this.logManager = logManager;
            httpClient = new HttpClient();
        }

        public async Task<string> GetAsync(string endpoint)
        {
            try
            {
                var response = await httpClient.GetAsync($"{baseUrl}{endpoint}");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                logManager.WriteError($"HTTP GET请求失败: {ex.Message}");
                throw;
            }
        }
    }
} 