using CefSharp;
using CefSharp.Handler;
using System.Text.Json.Nodes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CefClient.Handler
{
    public class CfxRequestHandler : RequestHandler
    {
        public readonly JsonObject _args = null;
        public bool UseLocalCache = false;
        public CfxRequestHandler(JsonObject args)
        {
            this._args = args;
        }
        protected override IResourceRequestHandler GetResourceRequestHandler(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, bool isNavigation, bool isDownload, string requestInitiator, ref bool disableDefaultHandling)
        {

            if (UseLocalCache && request.ResourceType == ResourceType.Image && request.ResourceType == ResourceType.Stylesheet)
            {
                return new CfxResourceRequestHandler(this._args);
            }
            return new CfxDefaultResourceRequestHandler(this._args);
        }

    }
}
