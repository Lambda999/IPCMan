using CefSharp;
using CefSharp.WinForms;

namespace CefClient;

public static class CefHelper
{
    public static Task<bool> LoadUrlAndWaitAsync(
        ChromiumWebBrowser browser,
        string url,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        if (!TryNormalizeToAllowedUrl(url, out var firstUrl))
            return Task.FromResult(false);

        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        EventHandler<LoadingStateChangedEventArgs>? loadingHandler = null;
        EventHandler<LoadErrorEventArgs>? loadErrorHandler = null;
        CancellationTokenSource? timeoutCts = null;
        CancellationTokenRegistration reg = default;
        var recoveredFromUnknownScheme = false;

        void Cleanup()
        {
            if (loadingHandler != null)
                browser.LoadingStateChanged -= loadingHandler;

            if (loadErrorHandler != null)
                browser.LoadError -= loadErrorHandler;

            reg.Dispose();
            timeoutCts?.Dispose();
        }

        loadingHandler = (s, e) =>
        {
            if (!e.IsLoading)
            {
                Cleanup();
                tcs.TrySetResult(true);
            }
        };

        loadErrorHandler = (s, e) =>
        {
            // 只关心主框架失败
            if (!e.Frame.IsMain)
                return;

            if (!recoveredFromUnknownScheme
                && TryNormalizeToAllowedUrl(e.FailedUrl, out var fallbackUrl)
                && !string.Equals(fallbackUrl, e.FailedUrl, StringComparison.OrdinalIgnoreCase))
            {
                recoveredFromUnknownScheme = true;
                browser.Load(fallbackUrl);
                return;
            }

            Cleanup();
            tcs.TrySetResult(false);
        };

        browser.LoadingStateChanged += loadingHandler;
        browser.LoadError += loadErrorHandler;

        timeoutCts = new CancellationTokenSource(timeout);
        timeoutCts.Token.Register(() =>
        {
            Cleanup();
            tcs.TrySetResult(false);
        });

        reg = cancellationToken.Register(() =>
        {
            Cleanup();
            tcs.TrySetCanceled(cancellationToken);
        });

        // 关键：订阅完事件后再导航，避免错过事件
        browser.Load(firstUrl);

        return tcs.Task;
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
