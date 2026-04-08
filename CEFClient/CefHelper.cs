
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
        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        EventHandler<LoadingStateChangedEventArgs>? loadingHandler = null;
        EventHandler<LoadErrorEventArgs>? loadErrorHandler = null;
        CancellationTokenSource? timeoutCts = null;
        CancellationTokenRegistration reg = default;

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
            if (e.Frame.IsMain)
            {
                Cleanup();
                tcs.TrySetResult(false);
            }
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
        browser.Load(url);

        return tcs.Task;
    }
}
