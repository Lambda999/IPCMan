

namespace CefClient
{
    using CefSharp;
    using CefSharp.WinForms;
    using Microsoft.VisualBasic;
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


        public async Task<BrowserRunResult> RunAsync(
            JsonNode? payload,
            CancellationToken cancellationToken = default,
            Func<BrowserRunStatus, CancellationToken, Task>? statusChanged = null)
        {
            await Task.Delay(5000);
            var task = payload?["task"];
            var sleepDelayMs = GetSleepDelayMilliseconds(task);
            var url = payload?["url"]?.ToString();
            var referer = payload?["referer"]?.ToString();


            var taskId = payload?["taskId"]?.ToString() ?? string.Empty;
            var consumerId = payload?["consumerId"]?.ToString() ?? "unknown";
            var uvIndex = payload?["uvIndex"]?.ToString() ?? BrowserId;
            var loadTimeoutMs = GetPositiveInt(payload, "loadTimeoutMs", 30000);
            var pvTotal = GetPositiveInt(task, "pv", 1);
            var pvIntervalMs = GetNonNegativeInt(payload, "pvIntervalMs", 1000);
            BrowserRunResult? result = null;
            var completedPv = 0;

            Task PublishLogAsync(string message)
            {
                return PublishStatusAsync(statusChanged, "log", true, message, CancellationToken.None);
            }

            try
            {
                await PublishLogAsync($"RunAsync start. taskId={taskId}, consumerId={consumerId}, uvIndex={uvIndex}, url={url}, pvTotal={pvTotal}");

                await PublishStatusAsync(statusChanged, "start", true, "browser started", cancellationToken, BuildRunData(
                    url,
                    referer,
                    sleepDelayMs,
                    taskId: taskId,
                    consumerId: consumerId,
                    uvIndex: uvIndex,
                    loadTimeoutMs: loadTimeoutMs,
                    pvTotal: pvTotal,
                    completedPv: completedPv,
                    pvIntervalMs: pvIntervalMs));

                if (string.IsNullOrWhiteSpace(url))
                {
                    await PublishLogAsync("url 不能为空");

                    result = new BrowserRunResult
                    {
                        BrowserId = BrowserId,
                        Success = false,
                        Message = "url 不能为空",
                        Data = BuildRunData(url, referer, sleepDelayMs, taskId: taskId, consumerId: consumerId, uvIndex: uvIndex, loadTimeoutMs: loadTimeoutMs, pvTotal: pvTotal, completedPv: completedPv, pvIntervalMs: pvIntervalMs)
                    };

                    await PublishStatusAsync(statusChanged, "error", false, result.Message, cancellationToken, result.Data);
                    return result;
                }

                WaitForNavigationAsyncResponse? lastLoadResponse = null;
                var lastLoadTimedOut = false;
                var finalLoadCompleted = false;

                var refererHeaders = BuildRefererHeaders(referer);

                for (var pvIndex = 1; pvIndex <= pvTotal; pvIndex++)
                {
                    await PublishLogAsync($"PV {pvIndex}/{pvTotal} loading. url={url}, referer={referer}, timeoutMs={loadTimeoutMs}");

                    var navigationTask = Browser.WaitForNavigationAsync(
                        TimeSpan.FromMilliseconds(loadTimeoutMs),
                        cancellationToken);

                    if (refererHeaders != null)
                    {
                        LoadUrl(Browser, url, "GET", refererHeaders);
                    }
                    else
                    {
                        Browser.Load(url);
                    }

                    WaitForNavigationAsyncResponse? loadResponse = null;
                    var loadTimedOut = false;
                    try
                    {
                        loadResponse = await navigationTask;
                    }
                    catch (TimeoutException)
                    {
                        loadTimedOut = true;
                        await PublishLogAsync($"PV {pvIndex}/{pvTotal} navigation timeout after {loadTimeoutMs}ms. url={url}");
                        TryStopBrowser(Browser);
                    }

                    var loadFailed = loadResponse != null && loadResponse.ErrorCode != CefErrorCode.None;
                    var loadCompleted = loadResponse != null && !loadTimedOut && loadResponse.ErrorCode == CefErrorCode.None;
                    lastLoadResponse = loadResponse;
                    lastLoadTimedOut = loadTimedOut;
                    finalLoadCompleted = loadCompleted;
                    completedPv = pvIndex;

                    var pvData = BuildRunData(
                        url,
                        referer,
                        sleepDelayMs,
                        taskId: taskId,
                        consumerId: consumerId,
                        uvIndex: uvIndex,
                        loadCompleted: loadCompleted,
                        loadTimedOut: loadTimedOut,
                        loadErrorCode: loadResponse?.ErrorCode.ToString() ?? string.Empty,
                        httpStatusCode: loadResponse?.HttpStatusCode ?? -1,
                        loadTimeoutMs: loadTimeoutMs,
                        pvIndex: pvIndex,
                        pvTotal: pvTotal,
                        completedPv: completedPv,
                        pvIntervalMs: pvIntervalMs);




                    if (loadFailed)
                    {
                        await PublishLogAsync($"PV {pvIndex}/{pvTotal} load failed. error={loadResponse?.ErrorCode}, httpStatus={loadResponse?.HttpStatusCode}");

                        result = new BrowserRunResult
                        {
                            BrowserId = BrowserId,
                            Success = false,
                            Message = "页面加载失败",
                            Data = pvData
                        };

                        await PublishStatusAsync(statusChanged, "error", false, $"第 {pvIndex}/{pvTotal} 次 PV 页面加载失败,{loadResponse?.ErrorCode}", cancellationToken, pvData);



                        return result;
                    }

                    await PublishLogAsync($"PV {pvIndex}/{pvTotal} completed. loadCompleted={loadCompleted}, timedOut={loadTimedOut}, httpStatus={loadResponse?.HttpStatusCode ?? -1}");
                    await PublishStatusAsync(statusChanged, "pv", true, loadCompleted ? $"pv {pvIndex}/{pvTotal} opened" : $"pv {pvIndex}/{pvTotal} 页面加载较慢，已按超时继续", cancellationToken, pvData);
                    await Task.Delay(TimeSpan.FromSeconds(90));
                    if (pvIndex < pvTotal && pvIntervalMs > 0)
                    {
                        await Task.Delay(TimeSpan.FromMilliseconds(pvIntervalMs), cancellationToken);
                    }
                }

                var dspData = BuildRunData(
                    url,
                    referer,
                    sleepDelayMs,
                    taskId: taskId,
                    consumerId: consumerId,
                    uvIndex: uvIndex,
                    loadCompleted: finalLoadCompleted,
                    loadTimedOut: lastLoadTimedOut,
                    loadErrorCode: lastLoadResponse?.ErrorCode.ToString() ?? string.Empty,
                    httpStatusCode: lastLoadResponse?.HttpStatusCode ?? -1,
                    loadTimeoutMs: loadTimeoutMs,
                    pvTotal: pvTotal,
                    completedPv: completedPv,
                    pvIntervalMs: pvIntervalMs);

                await PublishStatusAsync(statusChanged, "dsp", true, finalLoadCompleted ? "page opened" : "页面加载较慢，已按超时继续", cancellationToken, dspData);

                var title = await GetPageTitleAsync(Browser);
                var successData = BuildRunData(
                    url,
                    referer,
                    sleepDelayMs,
                    title,
                    taskId,
                    consumerId,
                    uvIndex,
                    finalLoadCompleted,
                    lastLoadTimedOut,
                    lastLoadResponse?.ErrorCode.ToString() ?? string.Empty,
                    lastLoadResponse?.HttpStatusCode ?? -1,
                    loadTimeoutMs,
                    pvTotal: pvTotal,
                    completedPv: completedPv,
                    pvIntervalMs: pvIntervalMs);

                await PublishLogAsync($"RunAsync success. title={title}, completedPv={completedPv}/{pvTotal}, finalLoadCompleted={finalLoadCompleted}");
                await PublishStatusAsync(statusChanged, "success", true, "执行成功", cancellationToken, successData);

                result = new BrowserRunResult
                {
                    BrowserId = BrowserId,
                    Success = true,
                    Message = finalLoadCompleted ? "执行成功" : "页面加载较慢，已按超时继续",
                    Data = successData
                };

                return result;
            }
            catch (OperationCanceledException)
            {
                result = new BrowserRunResult
                {
                    BrowserId = BrowserId,
                    Success = false,
                    Message = "取消",
                    Data = BuildRunData(url, referer, sleepDelayMs, taskId: taskId, consumerId: consumerId, uvIndex: uvIndex, loadTimeoutMs: loadTimeoutMs, pvTotal: pvTotal, completedPv: completedPv, pvIntervalMs: pvIntervalMs)
                };

                await PublishLogAsync("RunAsync canceled");
                await PublishStatusAsync(statusChanged, "error", false, "取消", CancellationToken.None, result.Data);
                return result;
            }
            catch (Exception ex)
            {
                result = new BrowserRunResult
                {
                    BrowserId = BrowserId,
                    Success = false,
                    Message = ex.Message,
                    Data = BuildRunData(url, referer, sleepDelayMs, taskId: taskId, consumerId: consumerId, uvIndex: uvIndex, loadTimeoutMs: loadTimeoutMs, pvTotal: pvTotal, completedPv: completedPv, pvIntervalMs: pvIntervalMs)
                };

                await PublishLogAsync($"RunAsync exception: {ex.Message}");
                await PublishStatusAsync(statusChanged, "error", false, ex.Message, CancellationToken.None, result.Data);
                return result;
            }
            finally
            {
                if (sleepDelayMs > 0)
                {
                    await Task.Delay(sleepDelayMs, CancellationToken.None);
                }

                await PublishStatusAsync(statusChanged, "complete", true, "RunAsync complete", CancellationToken.None, BuildRunData(
                    url,
                    referer,
                    sleepDelayMs,
                    taskId: taskId,
                    consumerId: consumerId,
                    uvIndex: uvIndex,
                    loadTimeoutMs: loadTimeoutMs,
                    pvTotal: pvTotal,
                    completedPv: completedPv,
                    pvIntervalMs: pvIntervalMs));
            }
        }


        private JsonObject BuildRunData(
            string? url,
            string? referer,
            int sleepDelayMs,
            string? title = null,
            string? taskId = null,
            string? consumerId = null,
            string? uvIndex = null,
            bool? loadCompleted = null,
            bool? loadTimedOut = null,
            string? loadErrorCode = null,
            int? httpStatusCode = null,
            int? loadTimeoutMs = null,
            int? pvIndex = null,
            int? pvTotal = null,
            int? completedPv = null,
            int? pvIntervalMs = null)
        {
            var data = new JsonObject
            {
                ["url"] = url ?? string.Empty,
                ["referer"] = referer ?? string.Empty,
                ["cachePath"] = CachePath,
                ["sleepDelayMs"] = sleepDelayMs
            };

            if (!string.IsNullOrWhiteSpace(taskId))
                data["taskId"] = taskId;
            if (!string.IsNullOrWhiteSpace(consumerId))
                data["consumerId"] = consumerId;
            if (!string.IsNullOrWhiteSpace(uvIndex))
                data["uvIndex"] = uvIndex;
            if (title != null)
                data["title"] = title;
            if (loadCompleted.HasValue)
                data["loadCompleted"] = loadCompleted.Value;
            if (loadTimedOut.HasValue)
                data["loadTimedOut"] = loadTimedOut.Value;
            if (loadErrorCode != null)
                data["loadErrorCode"] = loadErrorCode;
            if (httpStatusCode.HasValue)
                data["httpStatusCode"] = httpStatusCode.Value;
            if (loadTimeoutMs.HasValue)
                data["loadTimeoutMs"] = loadTimeoutMs.Value;
            if (pvIndex.HasValue)
                data["pvIndex"] = pvIndex.Value;
            if (pvTotal.HasValue)
                data["pvTotal"] = pvTotal.Value;
            if (completedPv.HasValue)
                data["completedPv"] = completedPv.Value;
            if (pvIntervalMs.HasValue)
                data["pvIntervalMs"] = pvIntervalMs.Value;

            return data;
        }

        private static WebHeaderCollection? BuildRefererHeaders(string? referer)
        {
            if (string.IsNullOrWhiteSpace(referer))
                return null;

            return new WebHeaderCollection
            {
                [HttpRequestHeader.Referer] = referer
            };
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
                    request.SetReferrer(refererValue, ReferrerPolicy.Origin);
                }

                request.Headers = originHeaders;
            }

            frame.LoadRequest(request);
        }


        private async Task PublishStatusAsync(
            Func<BrowserRunStatus, CancellationToken, Task>? statusChanged,
            string stage,
            bool success,
            string message,
            CancellationToken cancellationToken,
            JsonNode? data = null)
        {
            if (statusChanged == null)
                return;

            var statusData = data?.DeepClone() as JsonObject ?? new JsonObject();
            statusData["stage"] = stage;
            statusData["browserId"] = BrowserId;

            try
            {
                await statusChanged(new BrowserRunStatus
                {
                    BrowserId = BrowserId,
                    Stage = stage,
                    Success = success,
                    Message = message,
                    Data = statusData
                }, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"{BrowserId}: publish status failed. stage={stage}, msg={ex.Message}");
            }
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

    public sealed class BrowserRunStatus
    {
        public string BrowserId { get; set; } = "";
        public string Stage { get; set; } = "";
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public JsonNode? Data { get; set; }
    }

    public sealed class BrowserRunResult
    {
        public string BrowserId { get; set; } = "";
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public JsonNode? Data { get; set; }
    }
}
