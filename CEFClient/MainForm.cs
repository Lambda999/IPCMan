using CefClient.Common;
using CefSharp;
using CefSharp.WinForms;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CefClient
{
    public partial class MainForm : Form
    {
        private readonly FlowLayoutPanel _hostPanel;
        private readonly ConcurrentDictionary<string, BrowserSlot> _slots = new();
        public MainForm()
        {
            InitializeComponent();
            _hostPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                WrapContents = true,
                FlowDirection = FlowDirection.LeftToRight,
            };

            Controls.Add(_hostPanel);
        }

        private void MainForm_Load(object sender, EventArgs e)
        {

        }


        public async Task<bool> CreateBrowserAsync(
            string taskId,
            string browserId,
            System.Text.Json.Nodes.JsonNode? payload,
            CancellationToken cancellationToken = default)
        {


            var device = payload?["device"];
            var sw = device?["sw"]?.GetValue<int>() ?? 1080;
            var sh = device?["sh"]?.GetValue<int>() ?? 1920;
            var devProfile = AndroidViewportMatcher.Match(sw, sh);

            var cachePath = CefCachePaths.GetBrowserCachePath(browserId);

            var requestContext = new RequestContext(new RequestContextSettings
            {
                CachePath = cachePath,
                PersistUserPreferences = false,
                PersistSessionCookies = false,
            });

            var slot = await UiInvokeAsync(() =>
            {
                var panel = new Panel
                {
                    Width = 360,
                    Height = 720,
                    Margin = new Padding(5),
                    BorderStyle = BorderStyle.FixedSingle
                };

                var title = new Label
                {
                    AutoEllipsis = true,
                    Dock = DockStyle.Top,
                    Height = 28,
                    Text = $"CefClient {browserId}",
                    TextAlign = ContentAlignment.MiddleCenter
                };

                var browser = new ChromiumWebBrowser("about:blank", requestContext)
                {
                    Dock = DockStyle.Fill
                };
                panel.Controls.Add(browser);
                panel.Controls.Add(title);
                _hostPanel.Controls.Add(panel);

                return new BrowserSlot(browserId, panel, browser, requestContext, cachePath, devProfile, _hostPanel);
            }, cancellationToken);

            if (!_slots.TryAdd(browserId, slot))
            {
                await slot.DisposeAsync();
                if (!_slots.TryGetValue(browserId, out var addedByOtherThread))
                    return false;

                return await addedByOtherThread.WaitForInitializedAsync(cancellationToken: cancellationToken);
            }

            if (await slot.WaitForInitializedAsync(cancellationToken: cancellationToken))
                return true;

            if (_slots.TryRemove(browserId, out var failedSlot))
            {
                await failedSlot.DisposeAsync();
            }

            return false;
        }



        public async Task<BrowserRunResult> RunBrowserAsync(
            string browserId,
            System.Text.Json.Nodes.JsonNode? payload,
            CancellationToken cancellationToken = default,
            Func<BrowserRunStatus, CancellationToken, Task>? statusChanged = null)
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

            return await slot.RunAsync(payload, cancellationToken, statusChanged);
        }


        public async Task RemoveBrowserFastAsync(string browserId)
        {
            if (_slots.TryRemove(browserId, out var slot))
            {
                await UiInvokeAsync(() =>
                {
                    if (_hostPanel.Controls.Contains(slot.HostPanel))
                        _hostPanel.Controls.Remove(slot.HostPanel);
                });

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
        private async Task<bool> TryUiInvokeAsync(Action action, CancellationToken cancellationToken = default)
        {
            try
            {
                await UiInvokeAsync(action, cancellationToken);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
