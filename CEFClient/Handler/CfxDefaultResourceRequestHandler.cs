using CefSharp;
using CefSharp.Handler;
using System.Text.Json.Nodes;

namespace CefClient.Handler
{
    public class CfxDefaultResourceRequestHandler : ResourceRequestHandler
    {
        public readonly JsonObject _args = null;

        public CfxDefaultResourceRequestHandler(JsonObject args)
        {
            this._args = args;
        }

        protected override CefReturnValue OnBeforeResourceLoad(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, IRequestCallback callback)
        {
            var ua = this._args.SelectToken("dev.ua").Value<string>();
            var headers = request.Headers;
            headers["User-Agent"] = ua;
            request.Headers = headers;
            return CefReturnValue.Continue;
        }
    }
}
