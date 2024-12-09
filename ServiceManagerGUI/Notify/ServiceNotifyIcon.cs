using ServiceManagerGUI.Services;

namespace ServiceManagerGUI.Notify
{
    public class ServiceNotifyIcon : IDisposable
    {
        private readonly NotifyIcon notifyIcon;
        private readonly ContextMenuStrip contextMenu;
        private readonly ServiceManager serviceManager;
        private readonly Form parentForm;

        public ServiceNotifyIcon(ServiceManager serviceManager, Form parentForm)
        {
            this.serviceManager = serviceManager;
            this.parentForm = parentForm;
            
            contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("启动服务", null, OnStartService);
            contextMenu.Items.Add("停止服务", null, OnStopService);
            contextMenu.Items.Add("重启服务", null, OnRestartService);
            contextMenu.Items.Add("-"); // 分隔线
            contextMenu.Items.Add("退出", null, OnExit);

            notifyIcon = new NotifyIcon
            {
                Icon = SystemIcons.Application,
                Text = "服务管理器",
                ContextMenuStrip = contextMenu,
                Visible = true
            };

            notifyIcon.DoubleClick += NotifyIcon_DoubleClick;
        }

        public void ShowBalloonTip(string title, string message, ToolTipIcon icon)
        {
            notifyIcon.ShowBalloonTip(3000, title, message, icon);
        }

        public void StartService()
        {
            var result = serviceManager.StartService();
            ShowBalloonTip(result.success ? "成功" : "错误", result.message, 
                result.success ? ToolTipIcon.Info : ToolTipIcon.Error);
        }

        public void StopService()
        {
            var result = serviceManager.StopService();
            ShowBalloonTip(result.success ? "成功" : "错误", result.message, 
                result.success ? ToolTipIcon.Info : ToolTipIcon.Error);
        }

        public void RestartService()
        {
            var result = serviceManager.RestartService();
            ShowBalloonTip(result.success ? "成功" : "错误", result.message, 
                result.success ? ToolTipIcon.Info : ToolTipIcon.Error);
        }

        private void OnStartService(object? sender, EventArgs e) => StartService();
        private void OnStopService(object? sender, EventArgs e) => StopService();
        private void OnRestartService(object? sender, EventArgs e) => RestartService();

        private void NotifyIcon_DoubleClick(object? sender, EventArgs e)
        {
            parentForm.Show();
            parentForm.WindowState = FormWindowState.Normal;
            parentForm.Activate();
        }

        private void OnExit(object? sender, EventArgs e)
        {
            notifyIcon.Visible = false;
            Application.Exit();
        }

        public void Dispose()
        {
            notifyIcon.Dispose();
            contextMenu.Dispose();
        }
    }
} 