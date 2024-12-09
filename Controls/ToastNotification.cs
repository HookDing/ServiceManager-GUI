using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ServiceManagerGUI.Controls
{
    public class ToastNotification : Panel
    {
        private readonly System.Windows.Forms.Timer fadeTimer = new();
        private float opacity = 1.0f;
        private readonly Label messageLabel;
        private readonly PictureBox iconBox;

        public ToastNotification(string message, MessageType type)
        {
            this.SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            this.BackColor = Color.FromArgb(240, 240, 240);
            this.Size = new Size(300, 60);
            this.Padding = new Padding(10);

            // 创建图标
            iconBox = new PictureBox
            {
                Size = new Size(24, 24),
                SizeMode = PictureBoxSizeMode.CenterImage,
                Image = GetIcon(type),
                Location = new Point(10, (this.Height - 24) / 2)
            };

            // 创建消息标签
            messageLabel = new Label
            {
                Text = message,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Microsoft YaHei", 9f),
                Location = new Point(44, 0),
                Size = new Size(this.Width - 54, this.Height),
                BackColor = Color.Transparent
            };

            this.Controls.Add(iconBox);
            this.Controls.Add(messageLabel);

            // 设置淡出效果
            fadeTimer.Interval = 50;
            fadeTimer.Tick += FadeTimer_Tick;

            // 3秒后开始淡出
            var hideTimer = new System.Windows.Forms.Timer { Interval = 3000 };
            hideTimer.Tick += (s, e) =>
            {
                hideTimer.Stop();
                fadeTimer.Start();
            };
            hideTimer.Start();
        }

        private Image GetIcon(MessageType type)
        {
            return type switch
            {
                MessageType.Success => SystemIcons.Information.ToBitmap(),
                MessageType.Error => SystemIcons.Error.ToBitmap(),
                MessageType.Warning => SystemIcons.Warning.ToBitmap(),
                _ => SystemIcons.Information.ToBitmap()
            };
        }

        private void FadeTimer_Tick(object? sender, EventArgs e)
        {
            opacity -= 0.1f;
            if (opacity <= 0)
            {
                fadeTimer.Stop();
                this.Parent?.Controls.Remove(this);
                this.Dispose();
            }
            else
            {
                this.Invalidate();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            using var brush = new SolidBrush(Color.FromArgb((int)(opacity * 255), BackColor));
            using var path = new GraphicsPath();
            var rect = new Rectangle(0, 0, this.Width - 1, this.Height - 1);
            int radius = 10;
            path.AddArc(rect.X, rect.Y, radius * 2, radius * 2, 180, 90);
            path.AddArc(rect.Right - radius * 2, rect.Y, radius * 2, radius * 2, 270, 90);
            path.AddArc(rect.Right - radius * 2, rect.Bottom - radius * 2, radius * 2, radius * 2, 0, 90);
            path.AddArc(rect.X, rect.Bottom - radius * 2, radius * 2, radius * 2, 90, 90);
            path.CloseFigure();

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.FillPath(brush, path);
        }
    }

    public enum MessageType
    {
        Success,
        Error,
        Warning
    }
} 