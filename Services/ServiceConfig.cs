namespace ServiceManagerGUI.Services
{
    public class ServiceConfig
    {
        public string ServiceName { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public string ExecutablePath { get; set; }

        public ServiceConfig(string serviceName, string displayName, string description, string executablePath)
        {
            ServiceName = serviceName;
            DisplayName = displayName;
            Description = description;
            ExecutablePath = executablePath;
        }
    }
} 