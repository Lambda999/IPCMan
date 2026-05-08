using CefSharp;
using CefSharp.OffScreen;

namespace CefClient;

public enum PageLoadResult
{
    Completed,
    Failed,
    TimedOut
}

public static class CefHelper
{
    public static Task<PageLoadResult> LoadUrlAndWaitAsync(
        ChromiumWebBrowser browser,
        string url,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        var tcs = new TaskCompletionSource<PageLoadResult>(TaskCreationOptions.RunContinuationsAsynchronously);

        EventHandler<LoadingStateChangedEventArgs>? loadingHandler = null;
        EventHandler<LoadErrorEventArgs>? loadErrorHandler = null;
        CancellationTokenSource? timeoutCts = null;
        CancellationTokenRegistration timeoutReg = default;
        CancellationTokenRegistration cancelReg = default;
        var cleaned = 0;

        void Cleanup()
        {
            if (Interlocked.Exchange(ref cleaned, 1) != 0)
                return;

            if (loadingHandler != null)
                browser.LoadingStateChanged -= loadingHandler;

            if (loadErrorHandler != null)
                browser.LoadError -= loadErrorHandler;

            timeoutReg.Dispose();
            cancelReg.Dispose();
            timeoutCts?.Dispose();
        }

        loadingHandler = (s, e) =>
        {
            if (!e.IsLoading)
            {
                Cleanup();
                tcs.TrySetResult(PageLoadResult.Completed);
            }
        };

        loadErrorHandler = (s, e) =>
        {
            // 只关心主框架失败
            if (e.Frame.IsMain)
            {
                Cleanup();
                tcs.TrySetResult(PageLoadResult.Failed);
            }
        };

        browser.LoadingStateChanged += loadingHandler;
        browser.LoadError += loadErrorHandler;

        timeoutCts = new CancellationTokenSource(timeout);
        timeoutReg = timeoutCts.Token.Register(() =>
        {
            try
            {
                if (!browser.IsDisposed)
                    browser.Stop();
            }
            catch
            {
            }

            Cleanup();
            tcs.TrySetResult(PageLoadResult.TimedOut);
        });

        cancelReg = cancellationToken.Register(() =>
        {
            try
            {
                if (!browser.IsDisposed)
                    browser.Stop();
            }
            catch
            {
            }

            Cleanup();
            tcs.TrySetCanceled(cancellationToken);
        });

        // 关键：订阅完事件后再导航，避免错过事件
        browser.Load(url);

        return tcs.Task;
    }
}
