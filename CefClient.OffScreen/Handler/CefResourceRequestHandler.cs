using CefSharp;
using CefSharp.Handler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CefClient.Handler
{
    public class CefResourceRequestHandler : ResourceRequestHandler
    {
        private readonly string _userAgent = null;
        private readonly string _referer = null;
        public CefResourceRequestHandler(string userAgent, string referer)
        {
            this._userAgent = userAgent;
            this._referer = referer;
        }

        protected override CefReturnValue OnBeforeResourceLoad(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, IRequestCallback callback)
        {
            var headers = request.Headers;
            //headers.Remove("Upgrade-Insecure-Requests");
            headers["User-Agent"] = _userAgent;
            //headers["Accept"] = "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9";
            //headers["Accept-Encoding"] = "gzip, deflate";
            //headers["Accept-Language"] = "zh-CN,zh;q=0.9";
 

            request.Headers = headers;
          //  return base.OnBeforeResourceLoad(chromiumWebBrowser, browser, frame, request, callback);


            //request.SetHeaderByName("user-agent", this._userAgent, true);
            //return CefReturnValue.Continue;

            //Console.WriteLine(request.Url);
            //if (!string.IsNullOrWhiteSpace(_userAgent))
            //{
            //    var headers = request.Headers;
            //    if (!string.IsNullOrWhiteSpace(_userAgent))
            //    {
            //        headers["User-Agent"] = _userAgent;
            //    }
            //    request.Headers = headers;
            //}

            //if (!string.IsNullOrWhiteSpace(_referer))
            //{
            //    request.SetReferrer(_referer, ReferrerPolicy.Origin);
            //}
            //return base.OnBeforeResourceLoad(chromiumWebBrowser, browser, frame, request, callback);
            //return CefReturnValue.Continue;

             return base.OnBeforeResourceLoad(chromiumWebBrowser, browser, frame, request, callback);
        }
    }
}
