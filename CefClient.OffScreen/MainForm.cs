using CefSharp;
using CefSharp.OffScreen;
using System.Collections.Concurrent;
using System.Drawing;
using System.Text.Json.Nodes;
using System.Windows.Forms;

namespace CefClient
{
    public partial class MainForm : Form
    {
        private static readonly Size BrowserViewportSize = new(420, 920);
        private readonly ConcurrentDictionary<string, BrowserSlot> _slots = new();

        public event Action<string>? BrowserLog;
        public event Func<string, BrowserRunStatus, CancellationToken, Task>? BrowserStatus;

        public MainForm()
        {
            InitializeComponent();
            ShowInTaskbar = true;
            //WindowState = FormWindowState.Minimized;
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            screenshotPanel.Dock = DockStyle.Fill;
            screenshotPanel.AutoScroll = true;
            screenshotPanel.WrapContents = true;
            screenshotPanel.FlowDirection = FlowDirection.LeftToRight;

            // BeginInvoke(new Action(Hide));
        }

        public Task<bool> CreateBrowserAsync(string browserId, CancellationToken cancellationToken = default)
        {
            // OSR 模式不预创建浏览器；每次 RunBrowserAsync 都会创建一次性 BrowserSlot 并在 RunAsync 内释放。
            return Task.FromResult(true);
        }

        public async Task<BrowserRunResult> RunBrowserAsync(
            string browserId,
            JsonNode? payload,
            CancellationToken cancellationToken = default)
        {
            ShowBrowserPlaceholder(browserId);
            var slot = new BrowserSlot(browserId, ShowBrowserScreenshot, WriteBrowserLog, PublishBrowserStatusAsync);
            return await slot.RunAsync(payload, cancellationToken);
        }


        private void WriteBrowserLog(string message)
        {
            BrowserLog?.Invoke(message);
        }

        private Task PublishBrowserStatusAsync(BrowserRunStatus status, CancellationToken cancellationToken)
        {
            return BrowserStatus?.Invoke(status.BrowserId, status, cancellationToken) ?? Task.CompletedTask;
        }

        private void ShowBrowserPlaceholder(string browserId)
        {
            _ = UiInvokeAsync(() =>
            {
                EnsurePreviewWindowVisible();

                var item = screenshotPanel.Controls
                    .OfType<Panel>()
                    .FirstOrDefault(x => string.Equals(x.Name, GetScreenshotItemName(browserId), StringComparison.OrdinalIgnoreCase));

                if (item == null)
                    screenshotPanel.Controls.Add(CreateScreenshotItem(browserId));
            });
        }

        private void ShowBrowserScreenshot(string browserId, Image screenshot)
        {
            if (IsDisposed || Disposing)
            {
                screenshot.Dispose();
                return;
            }

            _ = UiInvokeAsync(() =>
            {
                if (IsDisposed || Disposing)
                {
                    screenshot.Dispose();
                    return;
                }

                EnsurePreviewWindowVisible();

                var item = screenshotPanel.Controls
                    .OfType<Panel>()
                    .FirstOrDefault(x => string.Equals(x.Name, GetScreenshotItemName(browserId), StringComparison.OrdinalIgnoreCase));

                if (item == null)
                {
                    item = CreateScreenshotItem(browserId);
                    screenshotPanel.Controls.Add(item);
                }

                var title = item.Controls.OfType<Label>().First();
                title.Text = $"{browserId}  {DateTime.Now:HH:mm:ss}";

                var pictureBox = item.Controls.OfType<PictureBox>().First();
                var oldImage = pictureBox.Image;
                pictureBox.Image = screenshot;
                oldImage?.Dispose();
            }).ContinueWith(task =>
            {
                if (task.IsFaulted || task.IsCanceled)
                    screenshot.Dispose();
            }, TaskScheduler.Default);
        }


        private void EnsurePreviewWindowVisible()
        {
            if (!Visible)
                Show();

            if (WindowState == FormWindowState.Minimized)
                WindowState = FormWindowState.Normal;

           // BringToFront();
            //Activate();
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
                //Width = 420,
                //Height= 920,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.WhiteSmoke
            };

            item.Controls.Add(pictureBox);
            item.Controls.Add(title);
            return item;
        }

        public async Task RemoveBrowserFastAsync(string browserId)
        {
            if (_slots.TryRemove(browserId, out var slot))
            {
                await slot.DetachFromUiAsync();

                _ = Task.Run(async () =>
                {
                    try
                    {
                        await Task.Delay(200);
                        await slot.DisposeHeavyAsync();
                    }
                    catch
                    {
                    }
                });
            }
        }

        public async Task RemoveAllBrowsersAsync()
        {
            var disposeTasks = new List<Task>();
            foreach (var kv in _slots.ToArray())
            {
                if (_slots.TryRemove(kv.Key, out var slot))
                {
                    disposeTasks.Add(Task.Run(async () =>
                    {
                        try
                        {
                            await slot.DisposeHeavyAsync();
                        }
                        catch
                        {
                        }
                    }));
                }
            }

            if (disposeTasks.Count == 0)
                return;

            var all = Task.WhenAll(disposeTasks);
            var finished = await Task.WhenAny(all, Task.Delay(TimeSpan.FromSeconds(2)));
            if (finished == all)
                await all;
        }

        private Task UiInvokeAsync(Action action, CancellationToken cancellationToken = default)
        {
            var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            if (IsDisposed || Disposing)
            {
                tcs.TrySetException(new ObjectDisposedException(nameof(MainForm)));
                return tcs.Task;
            }

            if (cancellationToken.IsCancellationRequested)
            {
                tcs.TrySetCanceled(cancellationToken);
                return tcs.Task;
            }

            void Execute()
            {
                try
                {
                    if (IsDisposed || Disposing)
                    {
                        tcs.TrySetException(new ObjectDisposedException(nameof(MainForm)));
                        return;
                    }

                    if (cancellationToken.IsCancellationRequested)
                    {
                        tcs.TrySetCanceled(cancellationToken);
                        return;
                    }

                    action();
                    tcs.TrySetResult();
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
            }

            try
            {
                if (InvokeRequired)
                {
                    BeginInvoke((Action)Execute);
                }
                else
                {
                    Execute();
                }
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }

            return tcs.Task;
        }

        private Task<T> UiInvokeAsync<T>(Func<T> func, CancellationToken cancellationToken = default)
        {
            var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);

            if (IsDisposed || Disposing)
            {
                tcs.TrySetException(new ObjectDisposedException(nameof(MainForm)));
                return tcs.Task;
            }

            if (cancellationToken.IsCancellationRequested)
            {
                tcs.TrySetCanceled(cancellationToken);
                return tcs.Task;
            }

            void Execute()
            {
                try
                {
                    if (IsDisposed || Disposing)
                    {
                        tcs.TrySetException(new ObjectDisposedException(nameof(MainForm)));
                        return;
                    }

                    if (cancellationToken.IsCancellationRequested)
                    {
                        tcs.TrySetCanceled(cancellationToken);
                        return;
                    }

                    var result = func();
                    tcs.TrySetResult(result);
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
            }

            try
            {
                if (InvokeRequired)
                {
                    BeginInvoke((Action)Execute);
                }
                else
                {
                    Execute();
                }
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }

            return tcs.Task;
        }
    }
}
