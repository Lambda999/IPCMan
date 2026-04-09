using CefSharp;
using CefSharp.Handler;

namespace CefClient.Handler;

public sealed class AppSchemeBlockRequestHandler : RequestHandler
{
    protected override bool OnBeforeBrowse(
        IWebBrowser chromiumWebBrowser,
        IBrowser browser,
        IFrame frame,
        IRequest request,
        bool userGesture,
        bool isRedirect)
    {
        if (!frame.IsMain)
            return base.OnBeforeBrowse(chromiumWebBrowser, browser, frame, request, userGesture, isRedirect);

        var url = request.Url;
        if (string.IsNullOrWhiteSpace(url))
            return base.OnBeforeBrowse(chromiumWebBrowser, browser, frame, request, userGesture, isRedirect);

        if (TryNormalizeToAllowedUrl(url, out var normalizedUrl))
        {
            if (!string.Equals(normalizedUrl, url, StringComparison.OrdinalIgnoreCase))
            {
                chromiumWebBrowser.Load(normalizedUrl);
                return true;
            }

            return base.OnBeforeBrowse(chromiumWebBrowser, browser, frame, request, userGesture, isRedirect);
        }

        // 阻止拉起外部 App 协议，避免出现 unknown scheme 错误页
        return true;
    }

    private static bool TryNormalizeToAllowedUrl(string? candidate, out string normalizedUrl)
    {
        normalizedUrl = string.Empty;
        if (string.IsNullOrWhiteSpace(candidate))
            return false;

        if (!Uri.TryCreate(candidate, UriKind.Absolute, out var uri))
            return false;

        if (IsAllowedScheme(uri.Scheme))
        {
            normalizedUrl = candidate;
            return true;
        }

        if (!TryExtractHttpUrlFromUnknownScheme(candidate, out var extractedUrl))
            return false;

        normalizedUrl = extractedUrl;
        return true;
    }

    private static bool TryExtractHttpUrlFromUnknownScheme(string? candidate, out string httpUrl)
    {
        httpUrl = string.Empty;
        if (string.IsNullOrWhiteSpace(candidate))
            return false;

        if (!Uri.TryCreate(candidate, UriKind.Absolute, out var uri))
            return false;

        if (IsHttpLikeScheme(uri.Scheme))
            return false;

        var query = uri.Query;
        if (string.IsNullOrWhiteSpace(query))
            return false;

        var span = query.AsSpan();
        if (span.Length > 0 && span[0] == '?')
            span = span[1..];

        foreach (var part in span.ToString().Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var idx = part.IndexOf('=');
            if (idx <= 0)
                continue;

            var key = part[..idx];
            if (!key.Equals("url", StringComparison.OrdinalIgnoreCase)
                && !key.Equals("target", StringComparison.OrdinalIgnoreCase)
                && !key.Equals("targeturl", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var value = part[(idx + 1)..];
            var decoded = Uri.UnescapeDataString(value.Replace('+', ' '));
            if (!Uri.TryCreate(decoded, UriKind.Absolute, out var decodedUri))
                continue;

            if (!IsHttpLikeScheme(decodedUri.Scheme))
                continue;

            httpUrl = decoded;
            return true;
        }

        return false;
    }

    private static bool IsAllowedScheme(string? scheme)
    {
        return string.Equals(scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
            || string.Equals(scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
            || string.Equals(scheme, "about", StringComparison.OrdinalIgnoreCase)
            || string.Equals(scheme, "data", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsHttpLikeScheme(string? scheme)
    {
        return string.Equals(scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
            || string.Equals(scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);
    }
}
