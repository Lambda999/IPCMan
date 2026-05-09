
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using CefClient.Handler.Event;
using CefSharp;
using CefSharp.Handler;
using CefSharp.ResponseFilter;
using System.Text.Json.Nodes;

namespace CefClient.Handler
{
    public class CfxResourceRequestHandler : ResourceRequestHandler
    {
        public readonly JsonObject _args = null;
        private string _localCacheFilePath = string.Empty;
        private bool IsLocalCacheFileExist => System.IO.File.Exists(_localCacheFilePath);
        public CfxResourceRequestHandler(JsonObject args)
        {
            this._args = args;
        }
        protected override IResourceHandler GetResourceHandler(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request)
        {
            if (!request.Url.StartsWith("devtools:"))
            {
                if (request.ResourceType == ResourceType.Image)
                {

                    try
                    {
                        _localCacheFilePath = CacheFileHelper.CalculateResourceFileName(request.Url, request.ResourceType);
                        if (string.IsNullOrWhiteSpace(_localCacheFilePath))
                        {
                            return null;
                        }
                    }
                    catch
                    {
                        return null;
                    }
                    if (!IsLocalCacheFileExist)
                    {
                        return null;
                    }
                    return new CfxResourceHandler(_localCacheFilePath);

                }
            }
            return base.GetResourceHandler(chromiumWebBrowser, browser, frame, request);

        }
        protected override IResponseFilter GetResourceResponseFilter(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, IResponse response)
        {
            if (!request.Url.StartsWith("devtools:"))
            {
                if (request.ResourceType == ResourceType.Image)
                {
                    if (!IsLocalCacheFileExist)
                    {
                        return new CfxResponseFilter() { LocalCacheFilePath = _localCacheFilePath };
                    }
                }
            }
            return null;
        }

        protected override CefReturnValue OnBeforeResourceLoad(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, IRequestCallback callback)
        {

            var ua = this._args["dev"]?["ua"]?.GetValue<string>() ?? string.Empty;
            var headers = request.Headers;
            headers["User-Agent"] = ua;
            request.Headers = headers;
            return CefReturnValue.Continue;
        }
    }
}
