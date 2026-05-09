namespace CefClient
{
    using CefSharp;
    using CefSharp.OffScreen;
    using System;
    using System.Diagnostics;
    using System.Drawing;
    using System.Text.Json.Nodes;

    public sealed class BrowserSlot : IAsyncDisposable
    {
        private const int DefaultLoadTimeoutMs = 8000;
        private const int DefaultFirstScreenshotDelayMs = 500;
        private const int DefaultFinalScreenshotDelayMs = 1500;
        private const int DefaultScreenshotTimeoutMs = 1500;
        private const int DefaultTitleTimeoutMs = 1000;
        private const int DefaultBrowserInitTimeoutMs = 5000;

        public string BrowserId { get; }
        private readonly Action<string, Image> _screenshotReady;
        private int _disposed;

        public BrowserSlot(string browserId, Action<string, Image> screenshotReady)
        {
            BrowserId = browserId;
            _screenshotReady = screenshotReady;
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

            var consumerId = payload?["consumerId"]?.ToString() ?? "unknown";
            var uvIndex = payload?["uvIndex"]?.ToString() ?? BrowserId;
            var cachePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "User Data", consumerId, uvIndex);
            Directory.CreateDirectory(cachePath);

            var os = GetNullableInt(payload, "os") ?? 0;
            var device = payload?["device"];
            var sw = GetNullableInt(device, "sw") ?? 412;
            var sh = GetNullableInt(device, "sh") ?? 915;

            var browserSettings = new BrowserSettings
            {
                WindowlessFrameRate = 1,
            };

            var requestContextSettings = new RequestContextSettings
            {
                PersistSessionCookies = true,
                CachePath = cachePath
            };

            try
            {
                using var requestContext = new RequestContext(requestContextSettings);
                using var browser = new ChromiumWebBrowser("about:blank", browserSettings, requestContext)
                {
                    Size = new Size(sw, sh)
                };
                await WaitForBrowserInitializedAsync(browser, DefaultBrowserInitTimeoutMs, cancellationToken);
                using var devToolsClient = browser.GetDevToolsClient();

                await devToolsClient.Storage.ClearDataForOriginAsync("*", "cache_storage,cookies,local_storage");
                await devToolsClient.Emulation.SetTouchEmulationEnabledAsync(true, Random.Shared.Next(4, 6));
                await devToolsClient.Emulation.SetDeviceMetricsOverrideAsync(width: sw, height: sh, deviceScaleFactor: 1, mobile: true);
                await devToolsClient.Emulation.SetUserAgentOverrideAsync(userAgent: payload?["userAgent"]?.ToString() ?? string.Empty, platform: os == 1 ? "Android" : "iPhone");

                var loadTimeoutMs = GetPositiveInt(payload, "loadTimeoutMs", DefaultLoadTimeoutMs);
                var firstScreenshotDelayMs = GetPositiveInt(payload, "firstScreenshotDelayMs", DefaultFirstScreenshotDelayMs);
                var finalScreenshotDelayMs = GetNonNegativeInt(payload, "finalScreenshotDelayMs", DefaultFinalScreenshotDelayMs);
                var screenshotTimeoutMs = GetPositiveInt(payload, "screenshotTimeoutMs", DefaultScreenshotTimeoutMs);
                var titleTimeoutMs = GetPositiveInt(payload, "titleTimeoutMs", DefaultTitleTimeoutMs);

                var loadTask = CefHelper.LoadUrlAndWaitAsync(
                    browser,
                    url,
                    TimeSpan.FromMilliseconds(loadTimeoutMs),
                    cancellationToken);

                // OSR 模式每次 runBrowser 都是一次性浏览：创建、浏览、截图、返回后由 using 自动释放。
                await Task.Delay(TimeSpan.FromMilliseconds(firstScreenshotDelayMs), cancellationToken);
                var firstScreenshotShown = await TryCaptureAndShowScreenshotAsync(browser, screenshotTimeoutMs, cancellationToken);

                var loadResult = await loadTask;
                if (loadResult == PageLoadResult.Failed)
                {
                    return new BrowserRunResult
                    {
                        BrowserId = BrowserId,
                        Success = false,
                        Message = "页面加载失败",
                        Data = new JsonObject
                        {
                            ["url"] = url,
                            ["firstScreenshotShown"] = firstScreenshotShown,
                            ["screenshotShown"] = firstScreenshotShown,
                            ["osrOneShot"] = true,
                            ["disposedByRunAsync"] = true
                        }
                    };
                }

                var title = await GetPageTitleAsync(browser, titleTimeoutMs, cancellationToken);

                var finalScreenshotShown = false;
                if (finalScreenshotDelayMs > 0)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(finalScreenshotDelayMs), cancellationToken);
                    finalScreenshotShown = await TryCaptureAndShowScreenshotAsync(browser, screenshotTimeoutMs, cancellationToken);
                }

                var screenshotShown = firstScreenshotShown || finalScreenshotShown;
                var loadCompleted = loadResult == PageLoadResult.Completed;

                return new BrowserRunResult
                {
                    BrowserId = BrowserId,
                    Success = true,
                    Message = loadCompleted ? "执行成功" : "页面加载较慢，已按超时继续",
                    Data = new JsonObject
                    {
                        ["title"] = title ?? "",
                        ["url"] = url,
                        ["loadCompleted"] = loadCompleted,
                        ["loadTimedOut"] = loadResult == PageLoadResult.TimedOut,
                        ["loadTimeoutMs"] = loadTimeoutMs,
                        ["screenshotShown"] = screenshotShown,
                        ["firstScreenshotShown"] = firstScreenshotShown,
                        ["finalScreenshotShown"] = finalScreenshotShown,
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


        private static async Task WaitForBrowserInitializedAsync(ChromiumWebBrowser browser, int timeoutMs, CancellationToken cancellationToken)
        {
            if (browser.IsDisposed)
                throw new ObjectDisposedException(nameof(ChromiumWebBrowser));

            if (browser.IsBrowserInitialized)
                return;

            var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            EventHandler? handler = null;

            handler = (_, _) =>
            {
                if (browser.IsBrowserInitialized)
                    tcs.TrySetResult();
            };

            browser.IsBrowserInitializedChanged += handler;

            try
            {
                if (browser.IsBrowserInitialized)
                    return;

                using var timeoutCts = new CancellationTokenSource(TimeSpan.FromMilliseconds(timeoutMs));
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
                await tcs.Task.WaitAsync(linkedCts.Token);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                throw new TimeoutException("离屏浏览器初始化超时");
            }
            finally
            {
                browser.IsBrowserInitializedChanged -= handler;
            }
        }

        private async Task<bool> TryCaptureAndShowScreenshotAsync(ChromiumWebBrowser browser, int timeoutMs, CancellationToken cancellationToken)
        {
            if (browser.IsDisposed)
                return false;

            try
            {
                using var bitmap = await browser.ScreenshotAsync(ignoreExistingScreenshot: true)
                    .WaitAsync(TimeSpan.FromMilliseconds(timeoutMs), cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();

                if (bitmap == null)
                    return false;

                _screenshotReady(BrowserId, new Bitmap(bitmap));
                return true;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                return false;
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
