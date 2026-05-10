using System.Drawing;

namespace MainClient
{
    public sealed class OsrScreenshotPreviewForm : Form
    {
        private const int MaxDisplayedScreenshots = 32;

        private readonly FlowLayoutPanel _screenshotPanel = new()
        {
            AutoScroll = true,
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(8),
            WrapContents = true
        };

        public OsrScreenshotPreviewForm()
        {
            Text = "CefClient.OffScreen 截图预览";
            ClientSize = new Size(1182, 1053);
            ShowInTaskbar = true;
            Controls.Add(_screenshotPanel);
        }

        public void ShowScreenshot(string browserId, string screenshotBase64)
        {
            if (IsDisposed || Disposing)
                return;

            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => ShowScreenshot(browserId, screenshotBase64)));
                return;
            }

            Image screenshot;
            try
            {
                var screenshotBytes = Convert.FromBase64String(screenshotBase64);
                using var stream = new MemoryStream(screenshotBytes);
                using var image = Image.FromStream(stream);
                screenshot = new Bitmap(image);
            }
            catch
            {
                return;
            }

            EnsurePreviewWindowVisible();

            var item = _screenshotPanel.Controls
                .OfType<Panel>()
                .FirstOrDefault(x => string.Equals(x.Name, GetScreenshotItemName(browserId), StringComparison.OrdinalIgnoreCase));

            if (item == null)
            {
                item = CreateScreenshotItem(browserId);
                _screenshotPanel.Controls.Add(item);
                TrimDisplayedScreenshots();
            }

            var title = item.Controls.OfType<Label>().First();
            title.Text = $"{browserId}  {DateTime.Now:HH:mm:ss}";

            var pictureBox = item.Controls.OfType<PictureBox>().First();
            var oldImage = pictureBox.Image;
            pictureBox.Image = screenshot;
            oldImage?.Dispose();
        }

        private void EnsurePreviewWindowVisible()
        {
            if (!Visible)
                Show();

            if (WindowState == FormWindowState.Minimized)
                WindowState = FormWindowState.Normal;
        }


        private void TrimDisplayedScreenshots()
        {
            while (_screenshotPanel.Controls.Count > MaxDisplayedScreenshots)
            {
                var oldest = _screenshotPanel.Controls[0];
                _screenshotPanel.Controls.RemoveAt(0);
                DisposeControlImages(oldest);
                oldest.Dispose();
            }
        }

        private static void DisposeControlImages(Control control)
        {
            foreach (var pictureBox in control.Controls.OfType<PictureBox>())
            {
                pictureBox.Image?.Dispose();
                pictureBox.Image = null;
            }

            foreach (Control child in control.Controls)
                DisposeControlImages(child);
        }

        private static string GetScreenshotItemName(string browserId)
        {
            return $"screenshot_{browserId}";
        }

        private static Panel CreateScreenshotItem(string browserId)
        {
            var item = new Panel
            {
                Name = GetScreenshotItemName(browserId),
                Width = 420,
                Height = 920,
                Margin = new Padding(4),
                BorderStyle = BorderStyle.FixedSingle
            };

            var title = new Label
            {
                AutoEllipsis = true,
                Dock = DockStyle.Top,
                Height = 28,
                Text = browserId,
                TextAlign = ContentAlignment.MiddleCenter
            };

            var pictureBox = new PictureBox
            {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.WhiteSmoke
            };

            item.Controls.Add(pictureBox);
            item.Controls.Add(title);
            return item;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                foreach (Control control in _screenshotPanel.Controls)
                    DisposeControlImages(control);
            }

            base.Dispose(disposing);
        }
    }
}
