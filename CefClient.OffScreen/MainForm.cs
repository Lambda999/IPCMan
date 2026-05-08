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

        public MainForm()
        {
            InitializeComponent();
            ShowInTaskbar = true;
            //WindowState = FormWindowState.Minimized;
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
           // BeginInvoke(new Action(Hide));
        }

        public async Task<bool> CreateBrowserAsync(string browserId, CancellationToken cancellationToken = default)
        {
            if (_slots.ContainsKey(browserId))
                return true;

            var slot = await UiInvokeAsync(() =>
            {
                var cacheRoot = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "CefSharp",
                    "TaskSlots",
                    $"proc_{Environment.ProcessId}");

                Directory.CreateDirectory(cacheRoot);

                var cachePath = Path.Combine(cacheRoot, browserId);
                Directory.CreateDirectory(cachePath);

                var requestContext = new RequestContext(new RequestContextSettings
                {
                    CachePath = cachePath,
                    PersistSessionCookies = false,
                });

                var browser = new ChromiumWebBrowser(
                    "about:blank",
                    requestContext: requestContext)
                {
                    Size = BrowserViewportSize
                };

                return new BrowserSlot(browserId, browser, requestContext);
            }, cancellationToken);

            return _slots.TryAdd(browserId, slot);
        }

        public async Task<BrowserRunResult> RunBrowserAsync(
            string browserId,
            JsonNode? payload,
            CancellationToken cancellationToken = default)
        {
            if (!_slots.TryGetValue(browserId, out var slot))
            {
                return new BrowserRunResult
                {
                    BrowserId = browserId,
                    Success = false,
                    Message = "browserId 不存在"
                };
            }

            return await slot.RunAsync(payload, cancellationToken);
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
            foreach (var kv in _slots.ToArray())
            {
                if (_slots.TryRemove(kv.Key, out var slot))
                {
                    await slot.DisposeAsync();
                }
            }
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
