namespace CefClient
{
    using CefClient.Handler;
    using CefSharp;
    using CefSharp.OffScreen;
    using System;
    using System.Diagnostics;
    using System.Drawing;
    using System.Text.Json.Nodes;

    public sealed class BrowserSlot : IAsyncDisposable
    {
        private const int DefaultLoadTimeoutMs = 8000;
        private const int DefaultFirstScreenshotDelayMs = 1000;
        private const int DefaultFinalScreenshotDelayMs = 1500;
        private const int DefaultScreenshotTimeoutMs = 3000;
        private const int DefaultTitleTimeoutMs = 1000;
        private const int DefaultInitialLoadTimeoutMs = 5000;

        public string BrowserId { get; }
        private readonly Action<string, Image> _screenshotReady;
        private readonly Action<string> _log;
        private int _disposed;

        public BrowserSlot(
            string browserId,
            Action<string, Image> screenshotReady,
            Action<string> log)
        {
            BrowserId = browserId;
            _screenshotReady = screenshotReady;
            _log = log;
        }

        private async Task<string> GetPageTitleAsync(ChromiumWebBrowser browser, int timeoutMs, CancellationToken cancellationToken)
        {
            if (browser == null || browser.IsDisposed)
                return "";

            var sw = Stopwatch.StartNew();

            while (sw.ElapsedMilliseconds < timeoutMs)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (browser.IsDisposed)
                    return "";

                if (browser.CanExecuteJavascriptInMainFrame)
                {
                    try
                    {
                        var result = await browser.EvaluateScriptAsync("document.title")
                            .WaitAsync(TimeSpan.FromMilliseconds(Math.Min(500, timeoutMs)), cancellationToken);
                        return result.Success ? result.Result?.ToString() ?? "" : "";
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch
                    {
                        // 继续等一下再试，但总等待时间受 timeoutMs 限制。
                    }
                }

                await Task.Delay(100, cancellationToken);
            }

            return "";
        }

        public async Task<BrowserRunResult> RunAsync(JsonNode? payload, CancellationToken cancellationToken = default)
        {
            var url = payload?["url"]?.ToString();
            if (string.IsNullOrWhiteSpace(url))
            {
                return new BrowserRunResult
                {
                    BrowserId = BrowserId,
                    Success = false,
                    Message = "url 不能为空"
                };
            }

            url = "chrome://version/";
            var taskId = payload?["taskId"]?.ToString() ?? BrowserId;
            var consumerId = payload?["consumerId"]?.ToString() ?? "unknown";
            var uvIndex = payload?["uvIndex"]?.ToString() ?? BrowserId;
            var cachePath = System.IO.Path.GetFullPath(CefCachePaths.GetUvCachePath(taskId, consumerId, uvIndex, BrowserId));
            Directory.CreateDirectory(cachePath);

            var os = GetNullableInt(payload, "os") ?? 0;
            var device = payload?["device"];
            var sw = GetNullableInt(device, "sw") ?? 412;
            var sh = GetNullableInt(device, "sh") ?? 915;

            var browserSettings = new BrowserSettings
            {
                WindowlessFrameRate = 15,
            };

            var requestContextSettings = new RequestContextSettings
            {
                PersistSessionCookies = true,
                CachePath = cachePath,

            };

            try
            {
                using var requestContext = new RequestContext(requestContextSettings);

                //requestContext.set
                //browser.RequestContext = RequestContext.Configure().WithCachePath('PathHere').Create();

                using var browser = new ChromiumWebBrowser("about:blank", browserSettings, requestContext)
                {
                    Size = new Size(sw, sh),
                    //RequestHandler = new ExternalProtocolRequestHandler(message => _log($"{BrowserId}: {message}"))
                };
                await browser.WaitForInitialLoadAsync()
                    .WaitAsync(TimeSpan.FromMilliseconds(DefaultInitialLoadTimeoutMs), cancellationToken);
                await ConfigureProxyAsync(requestContext, payload, cancellationToken);
                using var devToolsClient = browser.GetDevToolsClient();

                if (GetBool(payload, "clearStorage", false))
                {
                    await devToolsClient.Storage.ClearDataForOriginAsync("*", "cache_storage,cookies,local_storage");
                }

                await devToolsClient.Emulation.SetTouchEmulationEnabledAsync(true, Random.Shared.Next(4, 6));
                await devToolsClient.Emulation.SetDeviceMetricsOverrideAsync(width: sw, height: sh, deviceScaleFactor: 1, mobile: true);
                await devToolsClient.Emulation.SetUserAgentOverrideAsync(userAgent: payload?["userAgent"]?.ToString() ?? string.Empty, platform: os == 1 ? "Android" : "iPhone");

                var loadTimeoutMs = GetPositiveInt(payload, "loadTimeoutMs", DefaultLoadTimeoutMs);
                var firstScreenshotDelayMs = GetPositiveInt(payload, "firstScreenshotDelayMs", DefaultFirstScreenshotDelayMs);
                var finalScreenshotDelayMs = GetNonNegativeInt(payload, "finalScreenshotDelayMs", DefaultFinalScreenshotDelayMs);
                var screenshotTimeoutMs = GetPositiveInt(payload, "screenshotTimeoutMs", DefaultScreenshotTimeoutMs);
                var titleTimeoutMs = GetPositiveInt(payload, "titleTimeoutMs", DefaultTitleTimeoutMs);

                var navigationTask = browser.WaitForNavigationAsync(
                    TimeSpan.FromMilliseconds(loadTimeoutMs),
                    cancellationToken);
                browser.Load(url);

                // OSR 模式每次 runBrowser 都是一次性浏览：创建、浏览、截图、返回后由 using 自动释放。
                await Task.Delay(TimeSpan.FromMilliseconds(firstScreenshotDelayMs), cancellationToken);
 
                WaitForNavigationAsyncResponse? loadResponse = null;
                var loadTimedOut = false;
                try
                {
                    loadResponse = await navigationTask;
                }
                catch (TimeoutException)
                {
                    loadTimedOut = true;
                    TryStopBrowser(browser);
                }

                var loadFailed = loadResponse != null && loadResponse.ErrorCode != CefErrorCode.None;
                if (loadFailed)
                {
                    return new BrowserRunResult
                    {
                        BrowserId = BrowserId,
                        Success = false,
                        Message = "页面加载失败",
                        Data = new JsonObject
                        {
                            ["url"] = url,
                            ["cachePath"] = cachePath,
                            ["loadErrorCode"] = loadResponse?.ErrorCode.ToString() ?? string.Empty,
                            ["httpStatusCode"] = loadResponse?.HttpStatusCode ?? -1,
                            ["screenshotShown"] = false,
                            ["osrOneShot"] = true,
                            ["disposedByRunAsync"] = true
                        }
                    };
                }

                var title = await GetPageTitleAsync(browser, titleTimeoutMs, cancellationToken);
                await Task.Delay(TimeSpan.FromMilliseconds(finalScreenshotDelayMs), cancellationToken);
                var screenshotShown = await TryCaptureAndShowScreenshotAsync(browser, screenshotTimeoutMs, cancellationToken);
                var loadCompleted = loadResponse != null && !loadTimedOut && loadResponse.ErrorCode == CefErrorCode.None;

                return new BrowserRunResult
                {
                    BrowserId = BrowserId,
                    Success = true,
                    Message = loadCompleted ? $"执行成功:{device}" : "页面加载较慢，已按超时继续",
                    Data = new JsonObject
                    {
                        ["title"] = title ?? "",
                        ["url"] = url,
                        ["loadCompleted"] = loadCompleted,
                        ["loadTimedOut"] = loadTimedOut,
                        ["loadErrorCode"] = loadResponse?.ErrorCode.ToString() ?? string.Empty,
                        ["httpStatusCode"] = loadResponse?.HttpStatusCode ?? -1,
                        ["loadTimeoutMs"] = loadTimeoutMs,
                        ["cachePath"] = cachePath,
                        ["screenshotShown"] = screenshotShown,
                        ["osrOneShot"] = true,
                        ["disposedByRunAsync"] = true
                    }
                };
            }
            catch (OperationCanceledException)
            {
                return new BrowserRunResult
                {
                    BrowserId = BrowserId,
                    Success = false,
                    Message = "取消"
                };
            }
            catch (Exception ex)
            {
                return new BrowserRunResult
                {
                    BrowserId = BrowserId,
                    Success = false,
                    Message = ex.Message
                };
            }
        }




        private async Task ConfigureProxyAsync(IRequestContext requestContext, JsonNode? payload, CancellationToken cancellationToken)
        {
            var proxyServer = payload?["proxy_server"]?.ToString();
            if (string.IsNullOrWhiteSpace(proxyServer))
                proxyServer = payload?["proxyServer"]?.ToString();

            var isProxyMode = GetBool(payload, "isProxyMode", false) || !string.IsNullOrWhiteSpace(proxyServer);
            if (!isProxyMode || string.IsNullOrWhiteSpace(proxyServer))
                return;

            await Cef.UIThreadTaskFactory.StartNew(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                var preferences = new Dictionary<string, object>
                {
                    ["mode"] = "fixed_servers",
                    ["server"] = proxyServer
                };

                var success = requestContext.SetPreference("proxy", preferences, out var error);
                _log($"{BrowserId}: Set proxy server={proxyServer}, success={success}, error={error}");
            });
        }

        private static void TryStopBrowser(ChromiumWebBrowser browser)
        {
            try
            {
                if (!browser.IsDisposed)
                    browser.Stop();
            }
            catch
            {
            }
        }

        private async Task<bool> TryCaptureAndShowScreenshotAsync(ChromiumWebBrowser browser, int timeoutMs, CancellationToken cancellationToken)
        {
            if (browser.IsDisposed)
                return false;

            try
            {
                var screenshotBytes = await browser.CaptureScreenshotAsync()
                    .WaitAsync(TimeSpan.FromMilliseconds(timeoutMs), cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();

                if (screenshotBytes == null || screenshotBytes.Length == 0)
                    return false;

                using var stream = new MemoryStream(screenshotBytes);
                using var image = Image.FromStream(stream);
                image.Save($"test_{Guid.NewGuid()}.png");
                _screenshotReady(BrowserId, new Bitmap(image));
                return true;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _log($"{BrowserId}: screenshot capture failed: {ex.Message}");
                return false;
            }
        }

        private static bool GetBool(JsonNode? payload, string name, bool defaultValue)
        {
            try
            {
                return payload?[name]?.GetValue<bool>() ?? defaultValue;
            }
            catch
            {
                return defaultValue;
            }
        }

        private static int GetPositiveInt(JsonNode? payload, string name, int defaultValue)
        {
            var value = GetNullableInt(payload, name);
            return value.HasValue && value.Value > 0 ? value.Value : defaultValue;
        }

        private static int GetNonNegativeInt(JsonNode? payload, string name, int defaultValue)
        {
            var value = GetNullableInt(payload, name);
            return value.HasValue && value.Value >= 0 ? value.Value : defaultValue;
        }

        private static int? GetNullableInt(JsonNode? payload, string name)
        {
            try
            {
                return payload?[name]?.GetValue<int>();
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// OSR 一次性浏览器不挂 UI，这里保留原调用点以兼容外部流程。
        /// </summary>
        public Task DetachFromUiAsync()
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// OSR 浏览器实例已在 RunAsync 内通过 using 释放，这里只做幂等标记。
        /// </summary>
        public Task DisposeHeavyAsync()
        {
            Interlocked.Exchange(ref _disposed, 1);
            return Task.CompletedTask;
        }

        public async ValueTask DisposeAsync()
        {
            await DisposeHeavyAsync();
        }
    }

    public sealed class BrowserRunResult
    {
        public string BrowserId { get; set; } = "";
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public JsonNode? Data { get; set; }
    }
}
