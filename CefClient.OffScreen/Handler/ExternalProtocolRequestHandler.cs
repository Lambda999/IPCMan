using CefSharp;
using CefSharp.Handler;

namespace CefClient.Handler
{
    /// <summary>
    /// OSR 只允许正常网页导航。遇到 mailto/tel/baiduboxapp/intent 等外部协议时取消本次导航，
    /// 保持当前页面不变，并输出日志。
    /// </summary>
    public sealed class ExternalProtocolRequestHandler : RequestHandler
    {
        private readonly Action<string> _log;

        public ExternalProtocolRequestHandler(Action<string> log)
        {
            _log = log;
        }

        protected override bool OnBeforeBrowse(
            IWebBrowser chromiumWebBrowser,
            IBrowser browser,
            IFrame frame,
            IRequest request,
            bool userGesture,
            bool isRedirect)
        {
            var url = request.Url ?? string.Empty;
            if (IsAllowedNavigation(url))
                return false;

            _log($"Blocked external protocol navigation. url={url}, resourceType={request.ResourceType}, userGesture={userGesture}, isRedirect={isRedirect}");
            return true;
        }

        private static bool IsAllowedNavigation(string url)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
                return true;

            return string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
                || string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
                || string.Equals(uri.Scheme, "about", StringComparison.OrdinalIgnoreCase);
        }
    }
}
