using CefSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CefClient.Common
{
    public class CefSharpHelper
    {
        /// <summary>
        /// 获取DOM在页面的绝对位置
        /// </summary>
        /// <param name="browser"></param>
        /// <param name="selector"></param>
        /// <returns></returns>
        public static void GetElementRect(IWebBrowser browser, string selector, Action<List<int>, string> callback)
        {
            if (browser.Address.StartsWith("about:blank"))
            {
                return;
            }

            var script = @"function getElementRect() {
                                var element =document.querySelector(""{0}"");
                                var width = element.offsetWidth;
                                var height = element.offsetHeight;
                                var position = element.getBoundingClientRect();
                                var x = position.left;
                                var y = position.top;
                                return [x,y,width,height];
                            }
                            getElementRect();
                        ".Replace("{0}", selector);
            var task = browser.GetMainFrame().EvaluateScriptAsync(script);
            var response = task.Result;
            if (response.Success)
            {
                List<object> location = (List<object>)response.Result;
                callback(location.Select(s => { return Convert.ToInt32(s); }).ToList(), null);
            }
            else
            {
                var frames = browser.GetBrowser().GetFrameNames();
                if (frames.Count > 1)
                {
                    List<Task<JavascriptResponse>> alltasks = new List<Task<JavascriptResponse>>();
                    for (int i = 1; i < frames.Count; i++)
                    {
                        var f = browser.GetBrowser().GetFrameByName(frames[i]);
                        if (!f.Url.Equals("about:blank"))
                        {
                            f.EvaluateScriptAsync(script).ContinueWith((t, frame) =>
                            {
                                if (t.Result.Success)
                                {
                                    List<object> location = (List<object>)t.Result.Result;
                                    callback(location.Select(s => { return Convert.ToInt32(s); }).ToList(), frame.ToString());
                                }
                            }, frames[i]);
                        }
                    }
                }
            }
        }




        public static void GetElementRect(IWebBrowser browser, string selector, int index, Action<List<int>, string> callback)
        {
            if (browser.Address.StartsWith("about:blank"))
            {
                return;
            }

            var script = @"function getElementRect() {
                                var element =document.querySelectorAll(""{0}"")[{1}];
                                var width = element.offsetWidth;
                                var height = element.offsetHeight;
                                var position = element.getBoundingClientRect();
                                var x = position.left;
                                var y = position.top;
                                return [x,y,width,height];
                            }
                            getElementRect();
                        ".Replace("{0}", selector).Replace("{1}", index.ToString());
            var task = browser.GetMainFrame().EvaluateScriptAsync(script);
            var response = task.Result;
            if (response.Success)
            {
                List<object> location = (List<object>)response.Result;
                callback(location.Select(s => { return Convert.ToInt32(s); }).ToList(), null);
            }
            else
            {
                var frames = browser.GetBrowser().GetFrameNames();
                if (frames.Count > 1)
                {
                    List<Task<JavascriptResponse>> alltasks = new List<Task<JavascriptResponse>>();
                    for (int i = 1; i < frames.Count; i++)
                    {
                        var f = browser.GetBrowser().GetFrameByName(frames[i]);
                        if (!f.Url.Equals("about:blank"))
                        {
                            f.EvaluateScriptAsync(script).ContinueWith((t, frame) =>
                            {
                                if (t.Result.Success)
                                {
                                    List<object> location = (List<object>)t.Result.Result;
                                    callback(location.Select(s => { return Convert.ToInt32(s); }).ToList(), frame.ToString());
                                }
                            }, frames[i]);
                        }
                    }
                }
            }
        }

        public static List<int> GetElementRect(IWebBrowser browser, string selector)
        {

            var script = @"function getElementRect() {
                                var element ={0};
                                var width = element.offsetWidth;
                                var height = element.offsetHeight;
                                var position = element.getBoundingClientRect();
                                var x = position.left;
                                var y = position.top;
                                return [x,y,width,height];
                            }
                            getElementRect();
                        ".Replace("{0}", selector);
            var task = browser.GetMainFrame().EvaluateScriptAsync(script);
            var response = task.Result;
            if (response.Success)
            {
                List<object> location = (List<object>)response.Result;
                return location.Select(s => { return Convert.ToInt32(s); }).ToList();
            }
            else
            {
                var frames = browser.GetBrowser().GetFrameNames();
                if (frames.Count > 1)
                {
                    task = browser.GetBrowser().GetFrameByName(frames[1]).EvaluateScriptAsync(script);
                    response = task.Result;
                    if (response.Success)
                    {
                        List<object> location = (List<object>)response.Result;
                        return location.Select(s => { return Convert.ToInt32(s); }).ToList();
                    }
                }
            }
            return null;
        }

        public static List<int> GetElementRectv2(IWebBrowser browser, string element)
        {

            var script = @"function getElementRect() {
                                var element = {0};
                                var width = element.offsetWidth;
                                var height = element.offsetHeight;
                                var position = element.getBoundingClientRect();
                                var x = position.left;
                                var y = position.top;
                                return [x,y,width,height];
                            }
                            getElementRect();
                        ".Replace("{0}", element);

            var task = browser.GetMainFrame().EvaluateScriptAsync(script);
            var response = task.Result;
            if (response.Success)
            {
                List<object> location = (List<object>)response.Result;
                return location.Select(s => { return Convert.ToInt32(s); }).ToList();
            }
            else
            {
                var frames = browser.GetBrowser().GetFrameNames();
                if (frames.Count > 1)
                {
                    task = browser.GetBrowser().GetFrameByName(frames[1]).EvaluateScriptAsync(script);
                    response = task.Result;
                    if (response.Success)
                    {
                        List<object> location = (List<object>)response.Result;
                        return location.Select(s => { return Convert.ToInt32(s); }).ToList();
                    }
                }
            }
            return null;
        }

        public static List<int> GetElementRect(IWebBrowser browser, string selector, int index = 0)
        {
            var script = @"function getElementRect() {
                                var element =document.querySelectorAll(""{0}"")[{1}];
                                var width = element.offsetWidth;
                                var height = element.offsetHeight;
                                var position = element.getBoundingClientRect();
                                var x = position.left;
                                var y = position.top;
                                return [x,y,width,height];
                            }
                            getElementRect();
                        ".Replace("{0}", selector).Replace("{1}", index.ToString());

            var task = browser.GetMainFrame().EvaluateScriptAsync(script);
            var response = task.Result;
            if (response.Success)
            {
                List<object> location = (List<object>)response.Result;
                return location.Select(s => { return Convert.ToInt32(s); }).ToList();
            }
            else
            {
                var frames = browser.GetBrowser().GetFrameNames();
                if (frames.Count > 1)
                {
                    task = browser.GetBrowser().GetFrameByName(frames[1]).EvaluateScriptAsync(script);
                    response = task.Result;
                    if (response.Success)
                    {
                        List<object> location = (List<object>)response.Result;
                        return location.Select(s => { return Convert.ToInt32(s); }).ToList();
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// 发送鼠标点击消息
        /// </summary>
        /// <param name="host"></param>
        /// <param name="pt"></param>
        /// <param name="rx"></param>
        /// <param name="ry"></param>
        public static void SendMouseClickEvent(IBrowserHost host, Point pt, int rx = 0, int ry = 0)
        {
            int dx = pt.X + rx;
            int dy = pt.Y + ry;
            host.SendMouseClickEvent(dx, dy, MouseButtonType.Left, false, 1, CefEventFlags.None);
            System.Threading.Thread.Sleep(new Random().Next(20, 30));
            host.SendMouseClickEvent(dx, dy, MouseButtonType.Left, true, 1, CefEventFlags.None);
        }
        /// <summary>
        /// 发送鼠标移动消息
        /// </summary>
        /// <param name="host"></param>
        /// <param name="pt"></param>
        /// <param name="rx"></param>
        /// <param name="ry"></param>
        public static void SendMouseMoveEvent(IBrowserHost host, int rx = 0, int ry = 0)
        {
            host.SendMouseMoveEvent(rx, ry, false, new CefEventFlags());//移动鼠标
        }

        public static void SendMouseWheelEvent(IBrowserHost host, int x, int y, int deltaX, int deltaY)
        {
            host.SendMouseWheelEvent(x, y, deltaX, deltaY, CefEventFlags.None);
        }


    }
}
