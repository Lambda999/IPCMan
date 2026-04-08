

namespace CefClient
{
    using CefSharp;
    using CefSharp.WinForms;
    using System.Diagnostics;
    using System.Text.Json.Nodes;

    public sealed class BrowserSlot : IAsyncDisposable
    {
        public string BrowserId { get; }
        public Panel HostPanel { get; }
        public ChromiumWebBrowser Browser { get; }
        public IRequestContext RequestContext { get; }
        private readonly Control _parent;
        private int _disposed;

        public BrowserSlot(
            string browserId,
            Panel hostPanel,
            ChromiumWebBrowser browser,
            IRequestContext requestContext,
            Control parent)
        {
            BrowserId = browserId;
            HostPanel = hostPanel;
            Browser = browser;
            RequestContext = requestContext;
            _parent = parent;
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

                await Task.Delay(TimeSpan.FromSeconds(15));

                return new BrowserRunResult
                {
                    BrowserId = BrowserId,
                    Success = true,
                    Message = "执行成功",
                    Data = new JsonObject
                    {
                        ["title"] = title ?? "",
                        ["url"] = url
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



        /// <summary>
        /// 只从界面移除，不做重释放
        /// </summary>
        public Task DetachFromUiAsync()
        {
            try
            {
                if (_parent.Controls.Contains(HostPanel))
                    _parent.Controls.Remove(HostPanel);
            }
            catch
            {
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// 真正做重释放，建议后台调用
        /// </summary>
        public async Task DisposeHeavyAsync()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
                return;

            try
            {
                if (HostPanel.Controls.Contains(Browser))
                    HostPanel.Controls.Remove(Browser);
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

            try
            {
                HostPanel.Dispose();
            }
            catch
            {
            }

            // IRequestContext 一般不用你手动 Dispose
            // 如果你当前版本支持并且你确认需要，也可以自己补

            await Task.CompletedTask;
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                if (_parent.Controls.Contains(HostPanel))
                    _parent.Controls.Remove(HostPanel);
            }
            catch { }

            try
            {
                HostPanel.Controls.Remove(Browser);
            }
            catch { }

            try
            {
                Browser.Dispose();
            }
            catch { }

            try
            {
                HostPanel.Dispose();
            }
            catch { }

            await Task.CompletedTask;
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
