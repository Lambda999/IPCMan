namespace CefClient
{
    using CefSharp;
    using CefSharp.OffScreen;
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

        public string BrowserId { get; }
        public ChromiumWebBrowser Browser { get; }
        public IRequestContext RequestContext { get; }
        private readonly Action<string, Image> _screenshotReady;
        private int _disposed;

        public BrowserSlot(
            string browserId,
            ChromiumWebBrowser browser,
            IRequestContext requestContext,
            Action<string, Image> screenshotReady)
        {
            BrowserId = browserId;
            Browser = browser;
            RequestContext = requestContext;
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
            try
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

                var loadTimeoutMs = GetPositiveInt(payload, "loadTimeoutMs", DefaultLoadTimeoutMs);
                var firstScreenshotDelayMs = GetPositiveInt(payload, "firstScreenshotDelayMs", DefaultFirstScreenshotDelayMs);
                var finalScreenshotDelayMs = GetNonNegativeInt(payload, "finalScreenshotDelayMs", DefaultFinalScreenshotDelayMs);
                var screenshotTimeoutMs = GetPositiveInt(payload, "screenshotTimeoutMs", DefaultScreenshotTimeoutMs);
                var titleTimeoutMs = GetPositiveInt(payload, "titleTimeoutMs", DefaultTitleTimeoutMs);

                var loadTask = CefHelper.LoadUrlAndWaitAsync(
                    Browser,
                    url,
                    TimeSpan.FromMilliseconds(loadTimeoutMs),
                    cancellationToken);

                // 导航发起后先按短延迟截首屏，不再等完整 load 结束；慢页面也能很快在 MainForm 看到画面。
                await Task.Delay(TimeSpan.FromMilliseconds(firstScreenshotDelayMs), cancellationToken);
                var firstScreenshotShown = await TryCaptureAndShowScreenshotAsync(screenshotTimeoutMs, cancellationToken);

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
                            ["screenshotShown"] = firstScreenshotShown
                        }
                    };
                }

                // 即使页面一直挂起，也按短超时继续截图并返回，避免一个慢页面阻塞后续 UV/任务。
                var title = await GetPageTitleAsync(Browser, titleTimeoutMs, cancellationToken);

                var finalScreenshotShown = false;
                if (finalScreenshotDelayMs > 0)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(finalScreenshotDelayMs), cancellationToken);
                    finalScreenshotShown = await TryCaptureAndShowScreenshotAsync(screenshotTimeoutMs, cancellationToken);
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
                        ["finalScreenshotShown"] = finalScreenshotShown
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

        private async Task<bool> TryCaptureAndShowScreenshotAsync(int timeoutMs, CancellationToken cancellationToken)
        {
            if (Browser.IsDisposed)
                return false;

            try
            {
                using var bitmap = await Browser.ScreenshotAsync(ignoreExistingScreenshot: true)
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
        /// 离屏浏览器没有 WinForms 承载控件，这里保留原调用点以保持外部流程不变。
        /// </summary>
        public Task DetachFromUiAsync()
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// 真正做重释放，建议后台调用。
        /// </summary>
        public async Task DisposeHeavyAsync()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
                return;

            try
            {
                Browser.Stop();
            }
            catch
            {
            }

            try
            {
                Browser.Dispose();
            }
            catch
            {
            }

            await Task.CompletedTask;
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
