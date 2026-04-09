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
        public Func<string, CancellationToken, Task>? LogAsync { get; set; }
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
            LogInfo("MainForm loaded.");
        }


        public async Task<bool> CreateBrowserAsyncv2(string browserId, System.Text.Json.Nodes.JsonNode? payload, CancellationToken cancellationToken = default)
        {
            if (_slots.ContainsKey(browserId))
            {
                LogInfo($"CreateBrowserAsync skipped, browser already exists. browserId={browserId}");
                return true;
            }

            LogInfo($"CreateBrowserAsync started. browserId={browserId},payload={payload?.ToString()}");


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



                var browser = new ChromiumWebBrowser("about:blank", requestContext)
                {
                    Dock = DockStyle.None,
                    Location = new Point(0, 0),
                    Size = new Size(cssWidth, cssHeight)
                };
                var ua = payload?["dev"]?["ua"]?.ToString();
                var os = payload?["os"]?.GetValue<int>() ?? 1;
                this.Text = os.ToString();



                browser.IsBrowserInitializedChanged += (s, args) =>
                {
                    Task.Run(async () =>
                    {

                        using (DevToolsClient DTC = browser.GetBrowser().GetDevToolsClient())
                        {
                            if (new int[] { 1, 2 }.Contains(os))
                            {

                                await DTC.Emulation.SetScrollbarsHiddenAsync(true);
                                await DTC.Emulation.SetUserAgentOverrideAsync(userAgent: ua, platform: (os == 1 ? "Android" : "iPhone"));
                                await DTC.Emulation.SetDeviceMetricsOverrideAsync(
                                    width: cssWidth,
                                    height: cssHeight,
                                    deviceScaleFactor: deviceScale,
                                    mobile: true, scale: 1.0, screenWidth: sw, screenHeight: sh);
                                //await DTC.Emulation.SetAutoDarkModeOverrideAsync(true);
                                await DTC.Emulation.SetTouchEmulationEnabledAsync(true, 5);

                            }
                        }
                    });
                };

                panel.Controls.Add(browser);
                _hostPanel.Controls.Add(panel);
                _hostPanel.Controls.SetChildIndex(panel, 0);

                return new BrowserSlot(browserId, panel, browser, requestContext, _hostPanel);
            }, cancellationToken);


            var added = _slots.TryAdd(browserId, slot);
            LogInfo($"CreateBrowserAsync finished. browserId={browserId}, success={added}");
            return added;
        }



        private async Task ConfigureMobileEmulationAsync(
        ChromiumWebBrowser browser,
        System.Text.Json.Nodes.JsonNode? payload,
        DeviceProfileResult profileResult,
        CancellationToken cancellationToken = default)
        {


            var ua = payload?["dev"]?["ua"]?.ToString() ?? string.Empty;
            var os = payload?["os"]?.GetValue<int>() ?? 1;
            var sw = payload?["dev"]?["sw"]?.GetValue<int>() ?? 1080;
            var sh = payload?["dev"]?["sh"]?.GetValue<int>() ?? 1920;

            string platform;
            if (os == 2)
            {
                platform = "iPhone";
            }
            else
            {
                platform = "Android";
            }

            var deviceScale = profileResult.DeviceScaleFactor;
            int cssWidth = profileResult.CssWidth;
            int cssHeight = profileResult.CssHeight;

            cancellationToken.ThrowIfCancellationRequested();

            if (browser.IsDisposed || browser.Disposing)
                throw new ObjectDisposedException(nameof(ChromiumWebBrowser));

            var cefBrowser = browser.GetBrowser();
            if (cefBrowser == null)
                throw new InvalidOperationException("CEF browser is not available.");

            using var dtc = cefBrowser.GetDevToolsClient();

            // 顺序上通常先 UA，再 metrics，再 touch
            if (os == 1 || os == 2)
            {
                await dtc.Emulation.SetUserAgentOverrideAsync(
                    userAgent: ua,
                    platform: platform);

                cancellationToken.ThrowIfCancellationRequested();

                await dtc.Emulation.SetDeviceMetricsOverrideAsync(
                    width: cssWidth,
                    height: cssHeight,
                    deviceScaleFactor: deviceScale,
                    mobile: true,
                    scale: 1.0,
                    // 这里用逻辑尺寸更稳，不直接塞物理像素
                    screenWidth: cssWidth,
                    screenHeight: cssHeight);

                cancellationToken.ThrowIfCancellationRequested();

                await dtc.Emulation.SetTouchEmulationEnabledAsync(true, 5);

                cancellationToken.ThrowIfCancellationRequested();

                await dtc.Emulation.SetScrollbarsHiddenAsync(true);
            }

            LogInfo($"ConfigureMobileEmulationAsync done. css={cssWidth}x{cssHeight}, dpr={deviceScale}, os={os}, ua={ua}");
        }


        public async Task<bool> CreateBrowserAsync(
        string browserId,
        System.Text.Json.Nodes.JsonNode? payload,
        CancellationToken cancellationToken = default)
        {
            if (_slots.ContainsKey(browserId))
            {
                LogInfo($"CreateBrowserAsync skipped, browser already exists. browserId={browserId}");
                return true;
            }

            LogInfo($"CreateBrowserAsync started. browserId={browserId}, payload={payload?.ToJsonString()}");

            BrowserSlot? slot = null;
            ChromiumWebBrowser? browser = null;
            var sw = payload?["dev"]?["sw"]?.GetValue<int>() ?? 1080;
            var sh = payload?["dev"]?["sh"]?.GetValue<int>() ?? 1920;
            var ua = payload?["dev"]?["ua"]?.ToString() ?? string.Empty;
            var os = payload?["os"]?.GetValue<int>() ?? 1;
            DeviceProfileResult profileResult;
            // 1=Android, 2=iPhone
            if (os == 2)
            {
                // 如果你已经有 IPhoneViewportMatcher，就用它
                profileResult = iPhoneViewportMatcher.Match(sw, sh);
            }
            else
            {
                profileResult = AndroidViewportMatcher.Match(sw, sh);
            }
            var deviceScale = profileResult.DeviceScaleFactor;
            int cssWidth = profileResult.CssWidth;
            int cssHeight = profileResult.CssHeight;
            try
            {
                slot = await UiInvokeAsync(() =>
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
                        Width = 500,
                        Height = 960,
                        BorderStyle = BorderStyle.None
                    };


                    var createdBrowser = new ChromiumWebBrowser("about:blank", requestContext)
                    {
                        Dock = DockStyle.None,
                        Location = new Point(0, 0),
                        Size = new Size(cssWidth + 48, cssHeight),
                        RequestHandler = new AppSchemeBlockRequestHandler()
                    };

                    panel.Controls.Add(createdBrowser);
                    _hostPanel.Controls.Add(panel);
                    _hostPanel.Controls.SetChildIndex(panel, 0);

                    browser = createdBrowser;

                    return new BrowserSlot(browserId, panel, createdBrowser, requestContext, _hostPanel);
                }, cancellationToken);

                browser = slot.Browser;

                await browser.WaitForInitialLoadAsync();

                browser.FrameLoadEnd += (a,b) =>
                {
                    if(b.Frame.IsMain)
                    {
                        browser.ShowDevTools();
                    }


                };
                // 等浏览器真正初始化完成
                //await WaitForBrowserInitializedAsync(browser, cancellationToken);

                // 再做移动端模拟设置
                await ConfigureMobileEmulationAsync(browser, payload, profileResult, cancellationToken);

                var added = _slots.TryAdd(browserId, slot);
                if (!added)
                {
                    LogInfo($"CreateBrowserAsync TryAdd failed, browser already exists. browserId={browserId}");
                    //await SafeDisposeSlotAsync(slot);
                    return false;
                }

                LogInfo($"CreateBrowserAsync finished. browserId={browserId}, success={added}");
                return true;
            }
            catch (OperationCanceledException)
            {
                LogInfo($"CreateBrowserAsync canceled. browserId={browserId}");
                if (slot != null)
                {
                    //await SafeDisposeSlotAsync(slot);
                }
                return false;
            }
            catch (Exception ex)
            {
                LogInfo($"CreateBrowserAsync failed. browserId={browserId}, ex={ex}");
                if (slot != null)
                {
                    //await SafeDisposeSlotAsync(slot);
                }
                return false;
            }
        }






        public async Task<BrowserRunResult> RunBrowserAsync(
            string browserId,
            System.Text.Json.Nodes.JsonNode? payload,
            CancellationToken cancellationToken = default)
        {
            if (!_slots.TryGetValue(browserId, out var slot))
            {
                LogInfo($"RunBrowserAsync failed, browserId not found. browserId={browserId}");
                return new BrowserRunResult
                {
                    BrowserId = browserId,
                    Success = false,
                    Message = "browserId 不存在"
                };
            }
            LogInfo($"RunBrowserAsync started. browserId={browserId}");
            return await slot.RunAsync(payload, cancellationToken);
        }


        public async Task RemoveBrowserFastAsync(string browserId)
        {
            if (_slots.TryRemove(browserId, out var slot))
            {
                LogInfo($"RemoveBrowserFastAsync started. browserId={browserId}");
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
                        LogInfo($"RemoveBrowserFastAsync finished. browserId={browserId}");
                    }
                    catch
                    {
                        LogInfo($"RemoveBrowserFastAsync dispose failed. browserId={browserId}");
                    }
                });
            }
            else
            {
                LogInfo($"RemoveBrowserFastAsync skipped, browser not found. browserId={browserId}");
            }
        }

        public async Task RemoveAllBrowsersAsync()
        {
            LogInfo("RemoveAllBrowsersAsync started.");
            foreach (var kv in _slots.ToArray())
            {
                if (_slots.TryRemove(kv.Key, out var slot))
                {
                    await slot.DisposeAsync();
                }
            }
            LogInfo("RemoveAllBrowsersAsync finished.");
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
                    LogInfo($"UiInvokeAsync execute failed: {ex.Message}");
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
                LogInfo($"UiInvokeAsync execute failed: {ex.Message}");
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
                    LogInfo($"UiInvokeAsync<T> execute failed: {ex.Message}");
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
                LogInfo($"UiInvokeAsync<T> invoke failed: {ex.Message}");
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

        public async Task SendLogAsync(string message, CancellationToken cancellationToken = default)
        {
            try
            {
                if (LogAsync != null)
                {
                    await LogAsync.Invoke(message, cancellationToken);
                }
            }
            catch
            {
            }
        }

        private void LogInfo(string message)
        {
            _ = SendLogAsync(message);
        }
    }
}
