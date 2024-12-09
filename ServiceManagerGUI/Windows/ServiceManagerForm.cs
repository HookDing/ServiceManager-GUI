using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using System.ServiceProcess;
using ServiceManagerGUI.Services;
using ServiceManagerGUI.Notify;
using ServiceManagerGUI.Controls;

namespace ServiceManagerGUI.Windows
{
    public partial class ServiceManagerForm : Form
    {
        private const string ServiceName = "DotNetTestLogService";
        private readonly Dictionary<string, ServiceManager> serviceManagers = new();
        private readonly Dictionary<string, NotifyIcon> notifyIcons = new();
        private readonly List<ServiceConfig> serviceConfigs;
        private DataGridView gridServices = null!;
        private Panel buttonPanel = null!;
        private System.Windows.Forms.Timer statusTimer = null!;
        private Panel overlayPanel = null!;

        public ServiceManagerForm()
        {
            InitializeComponent();
            
            // 初始化服务配置
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            serviceConfigs = new List<ServiceConfig>
            {
                new ServiceConfig(
                    "DotNetTestLogService",
                    "测试日志服务",
                    "用于测试的Windows服务",
                    Path.Combine(baseDir, "ServiceManagerGUI.exe")
                ),
                new ServiceConfig(
                    "MySqlTestService",
                    "MySQL测试服务",
                    "用于测试MySQL连接的Windows服务",
                    Path.Combine(baseDir, "ServiceManagerGUI.exe")
                )
            };

            InitializeFormComponents();
            InitializeServices();

            // 注册事件处理程序
            this.FormClosing += Form1_FormClosing;
            Application.ApplicationExit += Application_ApplicationExit;
            
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = true;
        }

        private void InitializeFormComponents()
        {
            this.Text = "Windows 服务管理器";
            this.Size = new Size(1000, 600);  // 增加窗口尺寸
            this.BackColor = Color.WhiteSmoke;  // 设置背景色

            // 创建表格
            gridServices = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                GridColor = Color.LightGray,
                RowHeadersVisible = false,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                EnableHeadersVisualStyles = false,
                ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None,
                AllowUserToOrderColumns = false  // 禁止用户拖动列改变顺序
            };

            // 设置表格样式
            gridServices.DefaultCellStyle.SelectionBackColor = Color.FromArgb(210, 220, 230);
            gridServices.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(240, 240, 240);
            gridServices.ColumnHeadersDefaultCellStyle.ForeColor = Color.Black;
            gridServices.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(240, 240, 240);
            gridServices.ColumnHeadersDefaultCellStyle.Padding = new Padding(10);
            gridServices.ColumnHeadersHeight = 40;
            gridServices.RowTemplate.Height = 35;

            // 添加列
            var columns = new DataGridViewColumn[]
            {
                new DataGridViewTextBoxColumn 
                { 
                    Name = "ServiceName", 
                    HeaderText = "服务名称", 
                    Width = 200,
                    DefaultCellStyle = new DataGridViewCellStyle { Padding = new Padding(10, 0, 10, 0) },
                    SortMode = DataGridViewColumnSortMode.NotSortable  // 禁用排序
                },
                new DataGridViewTextBoxColumn 
                { 
                    Name = "DisplayName", 
                    HeaderText = "显示名称", 
                    Width = 200,
                    DefaultCellStyle = new DataGridViewCellStyle { Padding = new Padding(10, 0, 10, 0) },
                    SortMode = DataGridViewColumnSortMode.NotSortable  // 禁用排序
                },
                new DataGridViewTextBoxColumn 
                { 
                    Name = "Description", 
                    HeaderText = "描述", 
                    Width = 300,
                    DefaultCellStyle = new DataGridViewCellStyle { Padding = new Padding(10, 0, 10, 0) },
                    SortMode = DataGridViewColumnSortMode.NotSortable  // 禁用排序
                },
                new DataGridViewTextBoxColumn 
                { 
                    Name = "Status", 
                    HeaderText = "状态", 
                    Width = 100,
                    DefaultCellStyle = new DataGridViewCellStyle 
                    { 
                        Padding = new Padding(10, 0, 10, 0),
                        ForeColor = Color.DarkGray
                    },
                    SortMode = DataGridViewColumnSortMode.NotSortable  // 禁用排序
                }
            };

            gridServices.Columns.AddRange(columns);

            // 添加单元格格式化事件
            gridServices.CellFormatting += GridServices_CellFormatting;

            // 创建按钮面板
            buttonPanel = new Panel
            {
                Dock = DockStyle.Right,
                Width = 180,
                Padding = new Padding(15),
                BackColor = Color.White
            };

            // 创建按钮样式
            var buttonStyle = new Action<Button>((btn) =>
            {
                btn.Dock = DockStyle.Top;
                btn.Height = 45;
                btn.Margin = new Padding(0, 0, 0, 15);
                btn.FlatStyle = FlatStyle.Flat;
                btn.BackColor = Color.FromArgb(240, 240, 240);
                btn.FlatAppearance.BorderColor = Color.FromArgb(200, 200, 200);
                btn.Font = new Font(btn.Font.FontFamily, 9f);
                btn.Cursor = Cursors.Hand;  // 默认为手型光标

                // 启用状态的颜色
                btn.MouseEnter += (s, e) => 
                {
                    if (btn.Enabled)
                    {
                        btn.BackColor = Color.FromArgb(220, 220, 220);
                        btn.Cursor = Cursors.Hand;
                    }
                    else
                    {
                        btn.Cursor = Cursors.No;  // 禁用状态显示禁止光标
                    }
                };
                btn.MouseLeave += (s, e) => 
                {
                    if (btn.Enabled)
                    {
                        btn.BackColor = Color.FromArgb(240, 240, 240);
                    }
                    btn.Cursor = Cursors.Hand;  // 离开时恢复手型光标
                };

                // 禁用状态的颜色和光标
                btn.EnabledChanged += (s, e) =>
                {
                    if (btn.Enabled)
                    {
                        btn.BackColor = Color.FromArgb(240, 240, 240);
                        btn.ForeColor = Color.Black;
                        btn.Cursor = Cursors.Hand;
                    }
                    else
                    {
                        btn.BackColor = Color.FromArgb(245, 245, 245);
                        btn.ForeColor = Color.FromArgb(180, 180, 180);
                        btn.Cursor = btn.ClientRectangle.Contains(btn.PointToClient(Cursor.Position)) 
                            ? Cursors.No 
                            : Cursors.Hand;
                    }
                };
            });

            var btnInstall = new Button { Text = "安装服务" };
            buttonStyle(btnInstall);
            btnInstall.Click += BtnInstall_Click;

            var btnUninstall = new Button { Text = "卸载服务" };
            buttonStyle(btnUninstall);
            btnUninstall.Click += BtnUninstall_Click;

            var btnStart = new Button { Text = "启动服务" };
            buttonStyle(btnStart);
            btnStart.Click += BtnStart_Click;

            var btnStop = new Button { Text = "停止服务" };
            buttonStyle(btnStop);
            btnStop.Click += BtnStop_Click;

            var btnRestart = new Button { Text = "重启服务" };
            buttonStyle(btnRestart);
            btnRestart.Click += BtnRestart_Click;

            buttonPanel.Controls.AddRange(new Control[] { btnInstall, btnUninstall, btnStart, btnStop, btnRestart });

            // 创建主面板
            var mainPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(15)
            };
            mainPanel.Controls.Add(gridServices);

            this.Controls.AddRange(new Control[] { buttonPanel, mainPanel });

            // 注册选择变更事件
            gridServices.SelectionChanged += GridServices_SelectionChanged;

            // 修改蒙版面板
            overlayPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(128, 255, 255, 255),
                Visible = false
            };

            var loadingPanel = new Panel
            {
                Size = new Size(120, 120),
                BackColor = Color.FromArgb(240, 240, 240),
                Location = new Point(
                    (this.ClientSize.Width - 120) / 2,
                    (this.ClientSize.Height - 120) / 2
                )
            };

            var progressBar = new ProgressBar
            {
                Style = ProgressBarStyle.Marquee,
                MarqueeAnimationSpeed = 30,
                Size = new Size(80, 10),
                Location = new Point(20, 70)
            };

            var loadingLabel = new Label
            {
                Text = "正在处理...",
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(120, 30),
                Location = new Point(0, 30),
                Font = new Font(this.Font.FontFamily, 10f),
                BackColor = Color.Transparent
            };

            loadingPanel.Controls.AddRange(new Control[] { progressBar, loadingLabel });
            overlayPanel.Controls.Add(loadingPanel);
            this.Controls.Add(overlayPanel);
        }

        private void InitializeServices()
        {
            foreach (var config in serviceConfigs)
            {
                var serviceManager = new ServiceManager(
                    config.ServiceName,
                    config.ExecutablePath,
                    config.DisplayName
                );

                var notifyIcon = new NotifyIcon
                {
                    Icon = SystemIcons.Application,
                    Text = config.DisplayName,
                    Visible = true
                };

                // 添加单击事件
                notifyIcon.Click += NotifyIcon_Click;
                
                serviceManagers[config.ServiceName] = serviceManager;
                notifyIcons[config.ServiceName] = notifyIcon;

                // 添加到表格
                gridServices.Rows.Add(
                    config.ServiceName,
                    config.DisplayName,
                    config.Description,
                    GetServiceStatus(serviceManager)
                );
            }

            // 启动状态检查定时器
            statusTimer = new System.Windows.Forms.Timer
            {
                Interval = 1000
            };
            statusTimer.Tick += (s, e) => UpdateServiceStatuses();
            statusTimer.Start();
        }

        private string GetServiceStatus(ServiceManager serviceManager)
        {
            if (!serviceManager.IsServiceExists())
                return "⚪ 未安装";  // 白色
            if (serviceManager.IsServiceRunning())
                return "🟢 运行中";  // 绿色
            return "🔴 已停止";      // 红色
        }

        private void UpdateServiceStatuses()
        {
            foreach (DataGridViewRow row in gridServices.Rows)
            {
                string serviceName = row.Cells["ServiceName"].Value.ToString();
                var serviceManager = serviceManagers[serviceName];
                string status = GetServiceStatus(serviceManager);
                
                // 只有当状态发生变化时才更新
                if (row.Cells["Status"].Value?.ToString() != status)
                {
                    row.Cells["Status"].Value = status;
                    // 触发单元格格式化
                    gridServices.InvalidateCell(row.Cells["Status"]);
                }
            }
            UpdateButtonStates();
        }

        private void UpdateButtonStates()
        {
            if (gridServices == null || buttonPanel == null) return;

            if (gridServices.SelectedRows.Count == 0)
            {
                foreach (Control control in buttonPanel.Controls)
                {
                    if (control is Button button)
                        button.Enabled = false;
                }
                return;
            }

            var row = gridServices.SelectedRows[0];
            var serviceName = row.Cells["ServiceName"].Value?.ToString();
            if (serviceName == null) return;

            if (serviceManagers.TryGetValue(serviceName, out var serviceManager))
            {
                bool serviceExists = serviceManager.IsServiceExists();
                bool isRunning = serviceManager.IsServiceRunning();

                foreach (Control control in buttonPanel.Controls)
                {
                    if (control is Button button)
                    {
                        switch (button.Text)
                        {
                            case "安装服务":
                                button.Enabled = !serviceExists;
                                break;
                            case "卸载服务":
                                button.Enabled = serviceExists && !isRunning;
                                break;
                            case "启动服务":
                                button.Enabled = serviceExists && !isRunning;
                                break;
                            case "停止服务":
                                button.Enabled = serviceExists && isRunning;
                                break;
                            case "重启服务":
                                button.Enabled = serviceExists && isRunning;
                                break;
                        }
                    }
                }
            }
        }

        private void GridServices_SelectionChanged(object sender, EventArgs e)
        {
            UpdateButtonStates();
        }

        private string? GetSelectedServiceName()
        {
            if (gridServices?.SelectedRows.Count > 0)
            {
                var value = gridServices.SelectedRows[0].Cells["ServiceName"].Value;
                return value?.ToString();
            }
            return null;
        }

        private async void BtnInstall_Click(object? sender, EventArgs e)
        {
            string? serviceName = GetSelectedServiceName();
            if (string.IsNullOrEmpty(serviceName)) return;

            await ShowOverlayAsync(async () =>
            {
                await Task.Run(() =>
                {
                    var result = serviceManagers[serviceName].InstallService();
                    this.Invoke(() =>
                    {
                        ShowNotification(serviceName, result.success ? "成功" : "错误",
                            result.message, result.success ? ToolTipIcon.Info : ToolTipIcon.Error);
                    });
                });
            });
        }

        private async void BtnUninstall_Click(object? sender, EventArgs e)
        {
            string? serviceName = GetSelectedServiceName();
            if (string.IsNullOrEmpty(serviceName)) return;

            if (serviceManagers[serviceName].IsServiceRunning())
            {
                ShowNotification(serviceName, "警告", "服务正在运行，请先停止服务后再卸载！", ToolTipIcon.Warning);
                return;
            }

            await ShowOverlayAsync(async () =>
            {
                await Task.Run(() =>
                {
                    var result = serviceManagers[serviceName].UninstallService();
                    this.Invoke(() =>
                    {
                        ShowNotification(serviceName, result.success ? "成功" : "错误",
                            result.message, result.success ? ToolTipIcon.Info : ToolTipIcon.Error);
                    });
                });
            });
        }

        private async void BtnStart_Click(object sender, EventArgs e)
        {
            string? serviceName = GetSelectedServiceName();
            if (string.IsNullOrEmpty(serviceName)) return;

            await ShowOverlayAsync(async () =>
            {
                await Task.Run(() =>
                {
                    var result = serviceManagers[serviceName].StartService();
                    this.Invoke(() =>
                    {
                        ShowNotification(serviceName, result.success ? "成功" : "错误",
                            result.message, result.success ? ToolTipIcon.Info : ToolTipIcon.Error);
                    });
                });
            });
        }

        private async void BtnStop_Click(object sender, EventArgs e)
        {
            string? serviceName = GetSelectedServiceName();
            if (string.IsNullOrEmpty(serviceName)) return;

            await ShowOverlayAsync(async () =>
            {
                await Task.Run(() =>
                {
                    var result = serviceManagers[serviceName].StopService();
                    this.Invoke(() =>
                    {
                        ShowNotification(serviceName, result.success ? "成功" : "错误",
                            result.message, result.success ? ToolTipIcon.Info : ToolTipIcon.Error);
                    });
                });
            });
        }

        private async void BtnRestart_Click(object sender, EventArgs e)
        {
            string? serviceName = GetSelectedServiceName();
            if (string.IsNullOrEmpty(serviceName)) return;

            await ShowOverlayAsync(async () =>
            {
                await Task.Run(() =>
                {
                    var result = serviceManagers[serviceName].RestartService();
                    this.Invoke(() =>
                    {
                        ShowNotification(serviceName, result.success ? "成功" : "错误",
                            result.message, result.success ? ToolTipIcon.Info : ToolTipIcon.Error);
                    });
                });
            });
        }

        private void ShowNotification(string serviceName, string title, string message, ToolTipIcon icon)
        {
            this.Invoke(() =>
            {
                var type = icon switch
                {
                    ToolTipIcon.Error => MessageType.Error,
                    ToolTipIcon.Warning => MessageType.Warning,
                    _ => MessageType.Success
                };

                var toast = new ToastNotification(message, type)
                {
                    Location = new Point(
                        (this.ClientSize.Width - 300) / 2,
                        this.ClientSize.Height - 80
                    )
                };

                this.Controls.Add(toast);
                toast.BringToFront();
            });
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                bool anyRunning = serviceManagers.Values.Any(sm => sm.IsServiceRunning());
                if (anyRunning)
                {
                    e.Cancel = true;
                    this.Hide();
                    ShowNotification(serviceConfigs[0].ServiceName, "提示", "程序已最小化到系统托盘", ToolTipIcon.Info);
                }
                else
                {
                    CleanupResources();
                    Application.Exit();
                }
            }
            else if (e.CloseReason == CloseReason.WindowsShutDown)
            {
                CleanupResources();
            }
        }

        private void Application_ApplicationExit(object sender, EventArgs e)
        {
            CleanupResources();
        }

        private void CleanupResources()
        {
            // 停止定时器
            if (statusTimer != null)
            {
                statusTimer.Stop();
                statusTimer.Dispose();
            }

            // 先隐藏所有通知图标
            foreach (var notifyIcon in notifyIcons.Values)
            {
                if (notifyIcon != null)
                {
                    notifyIcon.Visible = false;
                }
            }

            // 等待一小段时间确保图标隐藏
            Thread.Sleep(100);

            // 然后清理资源
            foreach (var notifyIcon in notifyIcons.Values)
            {
                if (notifyIcon != null)
                {
                    try
                    {
                        notifyIcon.Icon?.Dispose();
                        notifyIcon.Dispose();
                    }
                    catch { }
                }
            }
            notifyIcons.Clear();

            // 清理其他控件
            gridServices?.Dispose();
            buttonPanel?.Dispose();
        }

        private void ExitApplication()
        {
            CleanupResources();
            Application.Exit();
        }

        private void OnExit(object sender, EventArgs e)
        {
            ExitApplication();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (this.WindowState == FormWindowState.Minimized)
            {
                this.Hide();
            }
        }

        private void NotifyIcon_Click(object sender, EventArgs e)
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
            this.Activate();
        }

        private void GridServices_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.ColumnIndex == gridServices.Columns["Status"].Index && e.Value != null)
            {
                string status = e.Value.ToString();
                var row = gridServices.Rows[e.RowIndex];
                bool isSelected = row.Selected;

                // 设置状态颜色
                if (status.Contains("未安装"))
                {
                    e.CellStyle.ForeColor = Color.Gray;
                }
                else if (status.Contains("运行中"))
                {
                    e.CellStyle.ForeColor = Color.Green;
                }
                else if (status.Contains("已停止"))
                {
                    e.CellStyle.ForeColor = Color.Red;
                }

                // 选中时只改变背景色，保持文字颜色
                if (isSelected)
                {
                    e.CellStyle.SelectionForeColor = e.CellStyle.ForeColor;
                    e.CellStyle.SelectionBackColor = Color.FromArgb(210, 220, 230);
                }
            }
            else if (e.RowIndex >= 0)  // 对于其他列
            {
                var row = gridServices.Rows[e.RowIndex];
                if (row.Selected)
                {
                    e.CellStyle.SelectionForeColor = Color.Black;  // 保持文字颜色
                    e.CellStyle.SelectionBackColor = Color.FromArgb(210, 220, 230);
                }
            }
        }

        // 修改 OnLoad 方法
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
        }

        private async Task ShowOverlayAsync(Func<Task> action)
        {
            // 在 UI 线程上禁用控件
            this.Invoke(() =>
            {
                foreach (Control control in this.Controls)
                {
                    if (control != overlayPanel)
                    {
                        control.Enabled = false;
                    }
                }

                if (overlayPanel != null)
                {
                    overlayPanel.Visible = true;
                    overlayPanel.BringToFront();
                }
            });

            try
            {
                await action();
            }
            finally
            {
                // 在 UI 线程上恢复控件状态
                this.Invoke(() =>
                {
                    foreach (Control control in this.Controls)
                    {
                        if (control != overlayPanel)
                        {
                            control.Enabled = true;
                        }
                    }

                    if (overlayPanel != null)
                    {
                        overlayPanel.Visible = false;
                    }

                    UpdateButtonStates();
                });
            }
        }

        private void SetOverlayText(string text)
        {
            if (overlayPanel.Controls[0] is Label label)
            {
                label.Text = text;
            }
        }
    }
} 