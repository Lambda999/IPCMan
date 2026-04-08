using CefSharp;
using CefSharp.Handler;
using Newtonsoft.Json.Linq;

namespace CefClient.Handler
{
    public class CfxDefaultResourceRequestHandler : ResourceRequestHandler
    {
        public readonly JObject _args = null;

        public CfxDefaultResourceRequestHandler(JObject args)
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
