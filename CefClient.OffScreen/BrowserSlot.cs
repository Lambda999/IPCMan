namespace CefClient
{
    using CefSharp;
    using CefSharp.OffScreen;
    using System.Diagnostics;
    using System.Drawing;
    using System.Text.Json.Nodes;

    public sealed class BrowserSlot : IAsyncDisposable
    {
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

        private async Task<string> GetPageTitleAsync(ChromiumWebBrowser browser, int timeoutMs = 3000)
        {
            if (browser == null || browser.IsDisposed)
                return "";

            var sw = Stopwatch.StartNew();

            while (sw.ElapsedMilliseconds < timeoutMs)
            {
                if (browser.IsDisposed)
                    return "";

                if (browser.CanExecuteJavascriptInMainFrame)
                {
                    try
                    {
                        var result = await browser.EvaluateScriptAsync("document.title");
                        return result.Success ? result.Result?.ToString() ?? "" : "";
                    }
                    catch
                    {
                        // 继续等一下再试
                    }
                }

                await Task.Delay(100);
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

                var ok = await CefHelper.LoadUrlAndWaitAsync(
                    Browser,
                    url,
                    TimeSpan.FromSeconds(30),
                    cancellationToken);

                if (!ok)
                {
                    return new BrowserRunResult
                    {
                        BrowserId = BrowserId,
                        Success = false,
                        Message = "页面加载超时"
                    };
                }

                var title = await GetPageTitleAsync(Browser);

                // 页面加载完成后尽快先截一张，避免等到任务结束即将回收浏览器时窗口才更新，
                // 导致用户几乎看不到 MainForm 上的预览图。后面保留结束前截图用于刷新动态页面最终状态。
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
                var firstScreenshotShown = await TryCaptureAndShowScreenshotAsync(cancellationToken);

                await Task.Delay(TimeSpan.FromSeconds(14), cancellationToken);
                var finalScreenshotShown = await TryCaptureAndShowScreenshotAsync(cancellationToken);
                var screenshotShown = firstScreenshotShown || finalScreenshotShown;

                var screenshotShown = await TryCaptureAndShowScreenshotAsync(cancellationToken);

                return new BrowserRunResult
                {
                    BrowserId = BrowserId,
                    Success = true,
                    Message = "执行成功",
                    Data = new JsonObject
                    {
                        ["title"] = title ?? "",
                        ["url"] = url,
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


        private async Task<bool> TryCaptureAndShowScreenshotAsync(CancellationToken cancellationToken)
        {
            if (Browser.IsDisposed)
                return false;

            try
            {
                using var bitmap = await Browser.ScreenshotAsync(ignoreExistingScreenshot: true);
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
