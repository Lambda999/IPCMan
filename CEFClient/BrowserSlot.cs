

namespace CefClient
{
    using CefSharp;
    using CefSharp.WinForms;
    using System.Collections.Specialized;
    using System.Diagnostics;
    using System.Globalization;
    using System.Net;
    using System.Text.Json.Nodes;

    public sealed class BrowserSlot : IAsyncDisposable
    {
        public string BrowserId { get; }
        public Panel HostPanel { get; }
        public ChromiumWebBrowser Browser { get; }
        public IRequestContext RequestContext { get; }
        public string CachePath { get; }
        private readonly Control _parent;
        private int _disposed;

        public BrowserSlot(
            string browserId,
            Panel hostPanel,
            ChromiumWebBrowser browser,
            IRequestContext requestContext,
            string cachePath,
            Control parent)
        {
            BrowserId = browserId;
            HostPanel = hostPanel;
            Browser = browser;
            RequestContext = requestContext;
            CachePath = cachePath;
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
            var task = payload?["task"];
            var sleepDelayMs = GetSleepDelayMilliseconds(task);
            var url = payload?["url"]?.ToString();
            var referer = GetString(payload, "referer");
            if (string.IsNullOrWhiteSpace(referer))
                referer = GetString(task, "referer");

            try
            {
                if (string.IsNullOrWhiteSpace(url))
                {
                    return new BrowserRunResult
                    {
                        BrowserId = BrowserId,
                        Success = false,
                        Message = "url 不能为空",
                        Data = BuildRunData(url, referer, sleepDelayMs)
                    };
                }

                var userAgent = GetString(payload, "userAgent");
                if (string.IsNullOrWhiteSpace(userAgent))
                    userAgent = GetString(payload?["device"], "ua");

                var navigationHeaders = BuildNavigationHeaders(userAgent, referer);
                var ok = await CefHelper.LoadUrlAndWaitAsync(
                    Browser,
                    url,
                    TimeSpan.FromSeconds(30),
                    cancellationToken,
                    browser => LoadUrl(browser, url, "GET", navigationHeaders));

                if (!ok)
                {
                    return new BrowserRunResult
                    {
                        BrowserId = BrowserId,
                        Success = false,
                        Message = "页面加载超时",
                        Data = BuildRunData(url, referer, sleepDelayMs)
                    };
                }

                var title = await GetPageTitleAsync(Browser);

                return new BrowserRunResult
                {
                    BrowserId = BrowserId,
                    Success = true,
                    Message = "执行成功",
                    Data = BuildRunData(url, referer, sleepDelayMs, title)
                };
            }
            catch (OperationCanceledException)
            {
                return new BrowserRunResult
                {
                    BrowserId = BrowserId,
                    Success = false,
                    Message = "取消",
                    Data = BuildRunData(url, referer, sleepDelayMs)
                };
            }
            catch (Exception ex)
            {
                return new BrowserRunResult
                {
                    BrowserId = BrowserId,
                    Success = false,
                    Message = ex.Message,
                    Data = BuildRunData(url, referer, sleepDelayMs)
                };
            }
            finally
            {
                if (sleepDelayMs > 0)
                {
                    await Task.Delay(sleepDelayMs, CancellationToken.None);
                }
            }
        }

        private JsonObject BuildRunData(string? url, string? referer, int sleepDelayMs, string? title = null)
        {
            var data = new JsonObject
            {
                ["url"] = url ?? string.Empty,
                ["referer"] = referer ?? string.Empty,
                ["cachePath"] = CachePath,
                ["sleepDelayMs"] = sleepDelayMs
            };

            if (title != null)
                data["title"] = title;

            return data;
        }

        private static WebHeaderCollection? BuildNavigationHeaders(string? userAgent, string? referer)
        {
            if (string.IsNullOrWhiteSpace(userAgent) && string.IsNullOrWhiteSpace(referer))
                return null;

            var headers = new WebHeaderCollection();
            if (!string.IsNullOrWhiteSpace(userAgent))
                headers[HttpRequestHeader.UserAgent] = userAgent;
            if (!string.IsNullOrWhiteSpace(referer))
                headers[HttpRequestHeader.Referer] = referer;

            return headers;
        }

        public static void LoadUrl(
            ChromiumWebBrowser browser,
            string? url,
            string requestMethod = "GET",
            WebHeaderCollection? headers = null,
            byte[]? postDataBytes = null)
        {
            if (browser == null || browser.IsDisposed || string.IsNullOrWhiteSpace(url))
                return;

            using var frame = browser.GetMainFrame();
            var initializePostData = string.Equals(requestMethod, "POST", StringComparison.OrdinalIgnoreCase);
            var request = frame.CreateRequest(initializePostData: initializePostData);
            if (initializePostData && postDataBytes is { Length: > 0 })
            {
                request.InitializePostData();
                request.PostData.AddData(postDataBytes);
            }

            request.Url = url;
            request.Method = string.IsNullOrWhiteSpace(requestMethod) ? "GET" : requestMethod;

            if (headers != null && headers.HasKeys())
            {
                var originHeaders = request.Headers ?? new NameValueCollection();
                foreach (string keyName in headers.AllKeys)
                {
                    originHeaders.Set(keyName, headers[keyName]);
                }

                var refererValue = headers[HttpRequestHeader.Referer];
                if (!string.IsNullOrWhiteSpace(refererValue))
                {
                    request.SetReferrer(refererValue, ReferrerPolicy.NeverClearReferrer);
                }

                request.Headers = originHeaders;
            }

            frame.LoadRequest(request);
        }

        private static int GetSleepDelayMilliseconds(JsonNode? task)
        {
            var sleepText = GetNodeText(task?["sleep"]);
            if (string.IsNullOrWhiteSpace(sleepText))
                return 0;

            int seconds;
            var rangeParts = sleepText.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (rangeParts.Length == 2 &&
                int.TryParse(rangeParts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var minSeconds) &&
                int.TryParse(rangeParts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var maxSeconds))
            {
                if (maxSeconds < minSeconds)
                {
                    (minSeconds, maxSeconds) = (maxSeconds, minSeconds);
                }

                if (maxSeconds <= 0)
                    return 0;

                minSeconds = Math.Max(0, minSeconds);
                var exclusiveMaxSeconds = maxSeconds == int.MaxValue ? int.MaxValue : maxSeconds + 1;
                seconds = Random.Shared.Next(minSeconds, exclusiveMaxSeconds);
            }
            else if (!int.TryParse(sleepText, NumberStyles.Integer, CultureInfo.InvariantCulture, out seconds))
            {
                return 0;
            }

            if (seconds <= 0)
                return 0;

            return seconds > int.MaxValue / 1000 ? int.MaxValue : seconds * 1000;
        }

        private static string GetString(JsonNode? payload, string name, string defaultValue = "")
        {
            var node = payload?[name];
            if (node == null)
                return defaultValue;

            try
            {
                if (node is JsonArray array)
                    return array.FirstOrDefault()?.GetValue<string>() ?? defaultValue;

                return node.GetValue<string>() ?? defaultValue;
            }
            catch
            {
                return node.ToString();
            }
        }

        private static string GetNodeText(JsonNode? node)
        {
            if (node == null)
                return string.Empty;

            try
            {
                return node.GetValue<string>() ?? string.Empty;
            }
            catch
            {
                return node.ToString();
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
