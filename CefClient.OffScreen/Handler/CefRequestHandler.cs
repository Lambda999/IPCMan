using CefSharp;
using CefSharp.Handler;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CefClient.Handler
{
    public class CefRequestHandler : RequestHandler
    {
        public readonly JObject _args = null;
        public readonly string userAgent = null;
        private readonly string referer = null;


        public CefRequestHandler(JObject args,string userAgent, string referer)
        {
            this._args = args;
            this.userAgent = userAgent;
            this.referer = referer;
        }

        protected override IResourceRequestHandler GetResourceRequestHandler(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, bool isNavigation, bool isDownload, string requestInitiator, ref bool disableDefaultHandling)
        {
            return new CefResourceRequestHandler(this.userAgent, this.referer);
        }

    }
}
