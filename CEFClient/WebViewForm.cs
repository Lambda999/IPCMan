using CefClient.Common;
using CefClient.Event;
using CefClient.Handler;
using CefSharp;
using CefSharp.DevTools;
using CefSharp.WinForms;
using System.Text.Json.Nodes;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Policy;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CefClient
{
    public partial class WebViewForm : Form
    {
        private const string caption = "曝光浏览器";
        private ChromiumWebBrowser chromiumWebBrowser = null;
        private readonly JsonObject _args;
        private bool isHiddenMode = true;

        #region  LogWrite

        public event EventHandler<WebViewLogEventArgs> OnLogEventArgs;
        private void LogWriteLine(string msg)
        {
            OnLogEventArgs?.Invoke(this, new WebViewLogEventArgs(msg));
        }
        #endregion

        public void LoadUrlWithHeader(ChromiumWebBrowser browser, string url, WebHeaderCollection headers = null, string requestMethod = "GET", byte[] postDataBytes = null)
        {
            using (var frame = browser.GetMainFrame())
            {
                var initializePostData = requestMethod.ToLower() == "post";
                var request = frame.CreateRequest(initializePostData: initializePostData);
                if (initializePostData)
                {
                    request.InitializePostData();
                    request.PostData.AddData(postDataBytes);
                }
                request.Url = url;
                request.Method = requestMethod;
                if (headers != null)
                {
                    var originHeader = request.Headers ?? new NameValueCollection();
                    foreach (string keyName in headers.AllKeys)
                    {
                        originHeader.Set(keyName, headers[keyName]);
                    }
                    var refererValue = headers[HttpRequestHeader.Referer];
                    if (!string.IsNullOrEmpty(refererValue))
                    {
                        request.SetReferrer(refererValue, ReferrerPolicy.NeverClearReferrer);
                    }
                    request.Headers = originHeader;
                }
                frame.LoadRequest(request);
            }
        }

        private Task LoadPageAsync(ChromiumWebBrowser browser, string address = null, int timeout = 10)
        {
            return browser.LoadUrlAsync(address).TimeoutAfter(TimeSpan.FromSeconds(timeout));
        }

        private ChromiumWebBrowser CreateChromiumWebBrowser(string address = null)
        {
            var browserSettings = new BrowserSettings()
            {
                //FileAccessFromFileUrls = CefState.Enabled,
                //UniversalAccessFromFileUrls = CefState.Enabled,
            };

            var disableLoadImage = Convert.ToBoolean(_args["DisableLoadImage"].ToString());
            if (disableLoadImage)
            {
                browserSettings.ImageLoading = CefState.Disabled;
            }
            var cacheIndex = _args["cacheIndex"].ToString();
            var cachePath = CefCachePaths.GetLegacyCachePath(cacheIndex);
            if (!System.IO.Directory.Exists(cachePath))
            {
                System.IO.Directory.CreateDirectory(cachePath);
            }
            //LogWriteLine($"cachePath={cachePath}");
            var requestContextSettings = new RequestContextSettings
            {
                CachePath = cachePath,
                PersistUserPreferences = true,
                //PersistSessionCookies = true,
                //PersistUserPreferences = true,
                // IgnoreCertificateErrors = true,
            };
            //if (_args.ContainsKey("EnableUserData"))
            //{
            //    var enableUserData = Convert.ToBoolean(_args["EnableUserData"].ToString());
            //    if (enableUserData)
            //    {

            //        LogWriteLine($"cachePath={cachePath}");
            //        requestContextSettings.CachePath = cachePath;
            //        requestContextSettings.PersistSessionCookies = true;
            //    }
            //}
            var requestContext = new RequestContext(requestContextSettings);
            var browser = new ChromiumWebBrowser(address ?? "about:blank", requestContext)
            {
                BrowserSettings = browserSettings
            };
            var sw = 1080;
            var sh = new Random().Next(1920, 2244);
            if (_args.ContainsKey("dev"))
            {
                if (!string.IsNullOrWhiteSpace(_args["dev"]["sw"]?.ToString()))
                {
                    sw = Convert.ToInt32(_args["dev"]["sw"].ToString());
                }
                if (!string.IsNullOrWhiteSpace(_args["dev"]["sh"]?.ToString()))
                {
                    sh = Convert.ToInt32(_args["dev"]["sh"].ToString());
                }
            }
            var os = 0;
            if (_args.ContainsKey("os"))
            {
                os = Int32.Parse(_args["os"].ToString());
            }

            var ua = _args["dev"]["ua"].ToString();
            var proxy_server = _args["proxy_server"].ToString();

            browser.Location = new Point(0, 0);
            if (new int[] { 1, 2 }.Contains(os))
            {
                browser.Size = new System.Drawing.Size(450, 920);
            }
            else
            {
                browser.Size = new System.Drawing.Size(1920, 1080);
            }
            browser.RenderProcessMessageHandler = new RenderProcessMessageHandler(sw, sh, ua, os);
            browser.LifeSpanHandler = new CfxLifeSpanHandler();
            browser.JsDialogHandler = new CfxJsDialogHandler();
            browser.IsBrowserInitializedChanged += (s, args) =>
            {
                #region 代理设置
                //user:password@ip:port
                if (Convert.ToBoolean(this._args["IsProxyMode"].ToString()) && !string.IsNullOrEmpty(proxy_server))
                {
                    var context = browser.GetBrowser().GetHost().RequestContext;
                    var v = new Dictionary<string, object>();
                    v["mode"] = "fixed_servers";
                    v["server"] = proxy_server;
                    bool success = context.SetPreference("proxy", v, out string error);
                }
                #endregion

                Task.Run(async () =>
                {

                    using (DevToolsClient DTC = browser.GetBrowser().GetDevToolsClient())
                    {
                        if (new int[] { 1, 2 }.Contains(os))
                        {
                            await DTC.Emulation.SetUserAgentOverrideAsync(userAgent: ua, platform: (os == 1 ? "Android" : "iPhone"));
                            await DTC.Emulation.SetScrollbarsHiddenAsync(true);
                            ///await DTC.Emulation.SetAutoDarkModeOverrideAsync(true);
                            await DTC.Emulation.SetTouchEmulationEnabledAsync(true, 10);
                            await DTC.Emulation.SetDeviceMetricsOverrideAsync(0, 0, 2.0, true);
                        }
                        else
                        {
                            await DTC.Emulation.SetUserAgentOverrideAsync(userAgent: ua);
                        }
                    }
                });
            };
            //browser.FrameLoadEnd += (sender, args) =>
            //{
            //    if (args.Frame.IsMain)
            //    {
            //        //args.Browser.ShowDevTools();
            //        args.Frame.ExecuteJavaScriptAsync("console.log('MainFrame finished loading')");
            //    }
            //};
            return browser;
        }
        char[] delimiters = { '\r', '\n' };
        public WebViewForm(JsonObject args)
        {
            this._args = args;

            InitializeComponent();
            var task = _args["task"]?.AsObject() ?? new JsonObject();
            this.chromiumWebBrowser = CreateChromiumWebBrowser();
            this.Controls.Add(this.chromiumWebBrowser);
            this.chromiumWebBrowser.Dock = DockStyle.None;
            LogWriteLine($"Size:{this.chromiumWebBrowser.Size}");
            var requestHandler = new CfxRequestHandler(this._args);
            this.chromiumWebBrowser.RequestHandler = requestHandler;

            Task.Factory.StartNew(async () =>
            {
                #region sleep
                var sleepDelayMs = GetSleepDelayMilliseconds(task, Random.Shared.Next(10, 15) * 1000);
                #endregion



                try
                {
                    string current_url = string.Empty;
                    List<string> urls = new List<string>();
                    List<string> referers = new List<string>();
                    if (_args["url"] is JsonArray)
                        urls.AddRange(_args["url"].Select(s => s.ToString()));
                    else
                        urls.Add(_args["url"].GetValue<string>());

                    if (_args["referer"] is JsonArray)
                        referers.AddRange(_args["referer"].Select(s => s.ToString()));
                    else
                    {
                        if (!string.IsNullOrWhiteSpace(_args["referer"].GetValue<string>()))
                        {
                            referers.Add(_args["referer"].GetValue<string>());
                        }

                    }

                    await this.chromiumWebBrowser.WaitForInitialLoadAsync();
                    await Task.Delay(200);
                    if (referers.Count() > 0)
                    {
                        #region 来源处理
                        for (int ii = 0; ii < referers.Count(); ii++)
                        {
                            var referer = referers[ii];
                            await chromiumWebBrowser.LoadUrlAsync(referer);
                            LogWriteLine($"曝光来源:{referer}");
                            if (urls.Count >= ii)
                                LoadUrlWithHeader(
                                    chromiumWebBrowser, urls[ii],
                                    new WebHeaderCollection
                                    {
                                        { "Referer", referer }
                                    });
                            LogWriteLine($"曝光网址:{urls[ii]}");
                        }
                        #endregion
                    }
                    else
                    {

                        #region 曝光网址
                        for (int ii = 0; ii < urls.Count(); ii++)
                        {
                            try
                            {
                                await LoadPageAsync(this.chromiumWebBrowser, urls[ii], 30);
                            }
                            catch (TimeoutException ex)
                            {
                                Debug.WriteLine(ex.Message);
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine(ex.Message);
                            }
                        }
                        #endregion
                    }


                    var onceClick = Convert.ToBoolean(_args["onceClick"].ToString());

                    if (onceClick)
                    {

                        var gdpr_js = task["gdpr_js"].ToString();
                        if (!string.IsNullOrWhiteSpace(gdpr_js))
                        {
                            LogWriteLine($"处理GDPR");
                            try
                            {
                                if (!string.IsNullOrWhiteSpace(task["gdpr_delay"].ToString()) && Int32.TryParse(task["gdpr_delay"].ToString(), out int gdpr_delay) && gdpr_delay > 0)
                                {
                                    await Task.Delay(gdpr_delay + new Random().Next(100, 1000));
                                }
                                else
                                {
                                    await Task.Delay(new Random().Next(3000, 5000));
                                }
                                var gdpr_script = System.Web.HttpUtility.UrlDecode(gdpr_js);
                                var gdpr_rect = CefSharpHelper.GetElementRect(this.chromiumWebBrowser, gdpr_script);
                                if (gdpr_rect != null)
                                {
                                    var gdpr_pt = new Point(gdpr_rect[0], gdpr_rect[1]);
                                    CefSharpHelper.SendMouseClickEvent(this.chromiumWebBrowser.GetBrowserHost(), gdpr_pt, new Random().Next(5, 20), new Random().Next(5, 10));
                                    await Task.Delay(new Random().Next(800, 1200));
                                }
                            }
                            catch (Exception)
                            {

                            }

                        }


                        #region 单次点击
                        LogWriteLine($"准备点击");
                        await Task.Delay(new Random().Next(5000, 8000));
                        int scrollCount = new Random().Next(1, 3);
                        int totalOffsetY = 0;
                        LogWriteLine($"随机滑动{scrollCount}次");

                        for (int i = 1; i <= scrollCount; i++)
                        {
                            int y = new Random().Next(300, 500);
                            totalOffsetY += y;
                            CefSharpHelper.SendMouseWheelEvent(this.chromiumWebBrowser.GetBrowserHost(), 50, 100, 0, -y);
                            await Task.Delay(new Random().Next(300, 500));
                        }
                        CefSharpHelper.SendMouseWheelEvent(this.chromiumWebBrowser.GetBrowserHost(), new Random().Next(50, 100), new Random().Next(50, 100), 0, totalOffsetY);

                        await Task.Delay(new Random().Next(3000, 5000));

                        if (task != null && task.ContainsKey("jstext") && !string.IsNullOrWhiteSpace(task["jstext"].ToString()))
                        {
                            var jstext = System.Web.HttpUtility.UrlDecode(task["jstext"].ToString());
                            LogWriteLine($"查找位置:{jstext}");
                            JavascriptResponse response = null;
                            int try_redo_count = 1;
                        redo_frame:
                            var frames = this.chromiumWebBrowser.GetBrowser().GetFrameNames();
                            if (frames.Count > 1)
                            {
                                LogWriteLine($"FRAMES:{frames.Count}");
                                if (frames.Count < 3 && try_redo_count++ < 3)
                                {
                                    await Task.Delay(new Random().Next(2000, 3000));
                                    goto redo_frame;
                                }
                                for (int i = 0; i < frames.Count; i++)
                                {
                                    var f = this.chromiumWebBrowser.GetBrowser().GetFrameByName(frames[i]);
                                    if (!f.Url.Equals("about:blank") && (f.Url.StartsWith("http://") || f.Url.StartsWith("https://") || f.Url.StartsWith("://") || f.Url.StartsWith("//")))
                                    {
                                        response = await f.EvaluateScriptAsync(jstext);
                                        if (response.Success && response.Result != null)
                                        {
                                            LogWriteLine($"查找结果,成功:{response.Result}");
                                            break;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                response = await this.chromiumWebBrowser.EvaluateScriptAsync(jstext);
                            }

                            if (response != null && response.Success && !string.IsNullOrWhiteSpace(response.Result?.ToString()))
                            {
                                try
                                {
                                    List<object> tagParams = (List<object>)response.Result;
                                    LogWriteLine($"tagParams:{tagParams.Count},{string.Join(",", tagParams)}");
                                    var target_query = tagParams[4]?.ToString();


                                    var target_express = tagParams[5]?.ToString();
                                    var target_selector = target_query.Replace("{selector}", target_express);
                                    await Task.Delay(1000);
                                    var rect = new List<int>() { Convert.ToInt32(tagParams[0]), Convert.ToInt32(tagParams[1]) };
                                    LogWriteLine($"rect:{rect.Count},{string.Join(",", rect)}");

                                    int _y = new Random().Next(0, rect[1] / 3);

                                    CefSharpHelper.SendMouseWheelEvent(this.chromiumWebBrowser.GetBrowserHost(), new Random().Next(50, 100), new Random().Next(50, 100), 0, -_y);
                                    await Task.Delay(1000);
                                    var pt = new Point(rect[0], rect[1]);
                                    current_url = this.chromiumWebBrowser.Address;
                                    LogWriteLine($"开始点击:{this.chromiumWebBrowser.GetMainFrame().Url},{pt.ToString()}");
                                    if (!string.IsNullOrWhiteSpace(target_query) && !target_query.Equals("fixed"))
                                    {
                                        CefSharpHelper.SendMouseClickEvent(this.chromiumWebBrowser.GetBrowserHost(), pt, new Random().Next(10, 100), new Random().Next(10, 50));
                                    }
                                    else
                                    {
                                        CefSharpHelper.SendMouseClickEvent(this.chromiumWebBrowser.GetBrowserHost(), pt, new Random().Next(10, 200), new Random().Next(10, 50));
                                    }
                                    LogWriteLine($"点击结束");

                                }
                                catch (Exception ex)
                                {
                                    LogWriteLine($"执行结果,失败:{ex.Message}");

                                }

                            }
                            else
                            {
                                LogWriteLine($"查找结果,失败:{response.Success},{response.Message},{response.Result}");
                            }
                        }
                        #endregion
                    }
                    else
                    {
                        #region 点击动作
                        var click_jump = Convert.ToBoolean(_args["click_jump"].ToString());
                        if (click_jump)
                        {

                            var gdpr_js = task["gdpr_js"].ToString();
                            if (!string.IsNullOrWhiteSpace(gdpr_js))
                            {
                                LogWriteLine($"处理GDPR");
                                try
                                {
                                    if (!string.IsNullOrWhiteSpace(task["gdpr_delay"].ToString()) && Int32.TryParse(task["gdpr_delay"].ToString(), out int gdpr_delay) && gdpr_delay > 0)
                                    {
                                        await Task.Delay(gdpr_delay + new Random().Next(100, 1000));
                                    }
                                    else
                                    {
                                        await Task.Delay(new Random().Next(3000, 5000));
                                    }
                                    var gdpr_script = System.Web.HttpUtility.UrlDecode(gdpr_js);
                                    var gdpr_rect = CefSharpHelper.GetElementRect(this.chromiumWebBrowser, gdpr_script);
                                    if (gdpr_rect != null)
                                    {
                                        var gdpr_pt = new Point(gdpr_rect[0], gdpr_rect[1]);
                                        CefSharpHelper.SendMouseClickEvent(this.chromiumWebBrowser.GetBrowserHost(), gdpr_pt, new Random().Next(5, 20), new Random().Next(5, 10));
                                        await Task.Delay(new Random().Next(800, 1200));
                                    }
                                }
                                catch (Exception)
                                {


                                }
                            }


                            requestHandler.UseLocalCache = true;
                            LogWriteLine($"随机点击开始");
                            if (task != null && task.ContainsKey("jstext") && !string.IsNullOrWhiteSpace(task["jstext"].ToString()))
                            {
                                var jstext = System.Web.HttpUtility.UrlDecode(task["jstext"].ToString());
                                int scrollCount = new Random().Next(3, 10);
                                LogWriteLine($"随机滑动{scrollCount}次");
                                for (int i = 1; i <= scrollCount; i++)
                                {
                                    CefSharpHelper.SendMouseWheelEvent(this.chromiumWebBrowser.GetBrowserHost(), 50, 100, 0, -new Random().Next(550, 850));
                                    await Task.Delay(new Random().Next(300, 500));
                                }
                                LogWriteLine($"查找链接:{jstext}");
                                JavascriptResponse response = null;
                                var frames = this.chromiumWebBrowser.GetBrowser().GetFrameNames();
                                if (frames.Count > 1)
                                {
                                    for (int i = 0; i < frames.Count; i++)
                                    {
                                        var f = this.chromiumWebBrowser.GetBrowser().GetFrameByName(frames[i]);
                                        if (!f.Url.Equals("about:blank") && (f.Url.StartsWith("http://") || f.Url.StartsWith("https://") || f.Url.StartsWith("://") || f.Url.StartsWith("//")))
                                        {
                                            response = await f.EvaluateScriptAsync(jstext);
                                            if (response.Success && response.Result != null)
                                            {
                                                LogWriteLine($"查找结果:{response.Result}");
                                                break;
                                            }
                                        }
                                    }
                                }
                                else
                                {

                                    response = await this.chromiumWebBrowser.EvaluateScriptAsync(jstext);
                                }
                                if (response != null && response.Success && !string.IsNullOrWhiteSpace(response.Result?.ToString()))
                                {
                                    List<object> tagParams = (List<object>)response.Result;
                                    var target_query = tagParams[4]?.ToString();
                                    var target_express = tagParams[5]?.ToString();
                                    var target_selector = target_query.Replace("{selector}", target_express);
                                    await Task.Delay(1000);
                                    var redo_click_count = 5;
                                    var redo_click_index = 1;
                                redo_click:
                                    var rect = CefSharpHelper.GetElementRect(this.chromiumWebBrowser, target_selector);
                                    if (rect != null)
                                    {

                                        var pt = new Point(rect[0], rect[1]);
                                        current_url = this.chromiumWebBrowser.Address;
                                        LogWriteLine($"点击开始:{this.chromiumWebBrowser.GetMainFrame().Url},{pt.ToString()}");
                                        CefSharpHelper.SendMouseClickEvent(this.chromiumWebBrowser.GetBrowserHost(), pt, new Random().Next(5, 20), new Random().Next(5, 10));
                                        await Task.Delay(new Random().Next(2000, 3000));

                                    }
                                    var target_url = this.chromiumWebBrowser.Address;
                                    if (target_url.Equals(current_url))
                                    {
                                        if (redo_click_index < redo_click_count)
                                        {
                                            redo_click_index++;
                                            LogWriteLine($"没点中,再来一次");
                                            CefSharpHelper.SendMouseWheelEvent(this.chromiumWebBrowser.GetBrowserHost(), 50, 100, 0, -new Random().Next(100, 550));
                                            await Task.Delay(new Random().Next(1000, 2000));
                                            goto redo_click;
                                        }
                                    }
                                    await Task.Delay(2000);

                                    scrollCount = new Random().Next(3, 10);
                                    LogWriteLine($"随机滑动{scrollCount}次");
                                    for (int i = 1; i <= scrollCount; i++)
                                    {
                                        CefSharpHelper.SendMouseWheelEvent(this.chromiumWebBrowser.GetBrowserHost(), 50, 100, 0, -new Random().Next(550, 850));
                                        await Task.Delay(new Random().Next(300, 500));
                                    }
                                    LogWriteLine($"点击结束:{target_url}");
                                }

                            }
                            else
                            {

                                int scrollCount = new Random().Next(3, 10);
                                LogWriteLine($"随机滑动{scrollCount}次");
                                for (int i = 1; i <= scrollCount; i++)
                                {
                                    CefSharpHelper.SendMouseWheelEvent(this.chromiumWebBrowser.GetBrowserHost(), 50, 100, 0, -new Random().Next(350, 850));
                                    await Task.Delay(new Random().Next(300, 500));
                                }
                                var redo_click_count = 3;
                                var redo_click_index = 1;
                            redo_click:
                                var pt = new Point(new Random().Next(30, 300), new Random().Next(50, 600));
                                current_url = this.chromiumWebBrowser.Address;
                                LogWriteLine($"点击开始:{this.chromiumWebBrowser.GetMainFrame().Url},{pt.ToString()}");
                                CefSharpHelper.SendMouseClickEvent(this.chromiumWebBrowser.GetBrowserHost(), pt);
                                await Task.Delay(new Random().Next(2000, 5000));
                                var target_url = this.chromiumWebBrowser.Address;
                                if (target_url.Equals(current_url))
                                {
                                    if (redo_click_index < redo_click_count)
                                    {
                                        redo_click_index++;
                                        LogWriteLine($"没点中,再来一次");
                                        CefSharpHelper.SendMouseWheelEvent(this.chromiumWebBrowser.GetBrowserHost(), 50, 100, 0, -new Random().Next(200, 500));
                                        goto redo_click;
                                    }
                                }
                                LogWriteLine($"点击结束:{target_url}");
                            }
                        }
                        else
                        {
                            LogWriteLine($"本次不点击");
                        }
                        #endregion
                    }

                    if (task.ContainsKey("pv") && int.TryParse(task["pv"].ToString(), out int pv) && pv > 1)
                    {
                        for (int i = 1; i < pv; i++)
                        {
                            await Task.Delay(sleepDelayMs);
                            for (int ii = 0; ii < urls.Count(); ii++)
                            {
                                try
                                {
                                    await LoadPageAsync(this.chromiumWebBrowser, urls[ii], 30);
                                }
                                catch (TimeoutException ex)
                                {
                                    Debug.WriteLine(ex.Message);
                                }
                                catch (Exception ex)
                                {
                                    Debug.WriteLine(ex.Message);
                                }
                            }
                        }
                    }
                    await TaskDelay(sleepDelayMs, "关闭浏览器");
                }
                catch (Exception ex)
                {
                    LogWriteLine(ex.Message);
                }
                finally
                {
                    await TaskEnd();
                }


            });

            this.isHiddenMode = Convert.ToBoolean(this._args["IsHiddenMode"].ToString());
            if (this.isHiddenMode)
            {
                this.WindowState = FormWindowState.Minimized;
                this.ShowInTaskbar = false;
                SetVisibleCore(false);
            }
            else
            {
                this.ShowInTaskbar = true;
            }
        }
        protected override void SetVisibleCore(bool value)
        {
            base.SetVisibleCore(value);
        }

        private static int GetSleepDelayMilliseconds(JsonObject task, int defaultValue = 0)
        {
            if (!task.ContainsKey("sleep"))
                return defaultValue;

            var sleepText = GetNodeText(task["sleep"]);
            if (string.IsNullOrWhiteSpace(sleepText))
                return defaultValue;

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
                return defaultValue;
            }

            if (seconds <= 0)
                return 0;

            return seconds > int.MaxValue / 1000 ? int.MaxValue : seconds * 1000;
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

        private async Task TaskDelay(int delayMilliseconds, string text = "结束")
        {
            if (delayMilliseconds <= 0)
                return;

            var remainingSeconds = (int)Math.Ceiling(delayMilliseconds / 1000d);
            this.BeginInvoke(new MethodInvoker(() =>
            {
                this.Text = $"{caption},{remainingSeconds}秒后{text}.";
            }));

            await Task.Delay(delayMilliseconds);
        }




        private async Task TaskEnd()
        {
            LogWriteLine($"执行任务完成");
            var cookieManager = this.chromiumWebBrowser.GetBrowser().GetHost().RequestContext.GetCookieManager(null);
            await cookieManager.DeleteCookiesAsync();
            this.BeginInvoke(new MethodInvoker(() =>
            {
                this.Close();
            }));
        }

        private void WebViewForm_Load(object sender, EventArgs e)
        {

        }
    }
}
