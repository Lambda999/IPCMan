using CefClient.Handler;
using CefClient.Viewport;
using CefSharp;
using CefSharp.DevTools;
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
                FlowDirection = FlowDirection.LeftToRight
            };

            Controls.Add(_hostPanel);
        }

        private void MainForm_Load(object sender, EventArgs e)
        {

        }


        public async Task<bool> CreateBrowserAsync(string browserId, System.Text.Json.Nodes.JsonNode? payload, CancellationToken cancellationToken = default)
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

                var panel = new Panel
                {
                    Width = 420,
                    Height = 920,
                    Margin = new Padding(5),
                    BorderStyle = BorderStyle.None
                };

                var sw = payload?["dev"]?["sw"]?.GetValue<int>() ?? 1080;
                var sh = payload?["dev"]?["sh"]?.GetValue<int>() ?? 1920;

                var profileResult = AndroidViewportMatcher.Match(sw, sh);
                var deviceScale = profileResult.DeviceScaleFactor;
                int cssWidth = profileResult.CssWidth;
                int cssHeight = profileResult.CssHeight;
                panel.Width = cssWidth + 5;
                panel.Height = cssHeight + 5;

                var browser = new ChromiumWebBrowser("about:blank", requestContext)
                {
                    Dock = DockStyle.None,
                    Location = new Point(0, 0),
                    Size = new Size(cssWidth, cssHeight)
                };
                var ua = payload?["dev"]?["ua"]?.ToString();
                var os = payload?["os"]?.GetValue<int>() ?? 1;
                this.Text = ua;

                //var proxy_server = _args["proxy_server"].ToString();


                //browser.RenderProcessMessageHandler = new RenderProcessMessageHandler(sw, sh, ua, os);
                //browser.LifeSpanHandler = new CfxLifeSpanHandler();
                //browser.JsDialogHandler = new CfxJsDialogHandler();
                browser.IsBrowserInitializedChanged += (s, args) =>
                {
                    #region 代理设置
                    //user:password@ip:port
                    //if (Convert.ToBoolean(this._args["IsProxyMode"].ToString()) && !string.IsNullOrEmpty(proxy_server))
                    //{
                    //    var context = browser.GetBrowser().GetHost().RequestContext;
                    //    var v = new Dictionary<string, object>();
                    //    v["mode"] = "fixed_servers";
                    //    v["server"] = proxy_server;
                    //    bool success = context.SetPreference("proxy", v, out string error);
                    //}
                    #endregion

                    Task.Run(async () =>
                    {

                        using (DevToolsClient DTC = browser.GetBrowser().GetDevToolsClient())
                        {
                            if (new int[] { 1, 2 }.Contains(os))
                            {

                                await DTC.Emulation.SetScrollbarsHiddenAsync(true);
                                await DTC.Emulation.SetDeviceMetricsOverrideAsync(
                                    width: cssWidth,
                                    height: cssHeight,
                                    deviceScaleFactor: deviceScale,
                                    mobile: true, scale: 1.0, screenWidth: sw, screenHeight: sh);
                                await DTC.Emulation.SetUserAgentOverrideAsync(userAgent: ua, platform: (os == 1 ? "Android" : "iPhone"));

                                ///await DTC.Emulation.SetAutoDarkModeOverrideAsync(true);
                                //await DTC.Emulation.SetTouchEmulationEnabledAsync(true, 10);
                                //await DTC.Emulation.SetDeviceMetricsOverrideAsync(0, 0, 2.0, true);
                            }
                            else
                            {
                                //await DTC.Emulation.SetUserAgentOverrideAsync(userAgent: ua);
                            }
                        }
                    });
                };







                panel.Controls.Add(browser);
                _hostPanel.Controls.Add(panel);
                _hostPanel.Controls.SetChildIndex(panel, 0);

                return new BrowserSlot(browserId, panel, browser, requestContext, _hostPanel);
            }, cancellationToken);

            return _slots.TryAdd(browserId, slot);
        }



        public async Task<BrowserRunResult> RunBrowserAsync(
            string browserId,
            System.Text.Json.Nodes.JsonNode? payload,
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
