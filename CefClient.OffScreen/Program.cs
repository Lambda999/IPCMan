using CefSharp;
using CefSharp.OffScreen;

namespace CefClient
{
    public class Program
    {
        [STAThread]
        public static int Main(string[] args)
        {
            var pipeName = args
            .FirstOrDefault(x => x.StartsWith("--pipe-name=", StringComparison.OrdinalIgnoreCase))
            ?.Substring("--pipe-name=".Length);


            var defaultSubprocessPath = Path.Combine(AppContext.BaseDirectory, "CefSharp.BrowserSubprocess.exe");

            var rootCachePath = CefCachePaths.RootCachePath;
            ApplicationConfiguration.Initialize();
            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);

            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

            Application.ThreadException += (sender, e) =>
            {
                // TODO: 这里接你的日志
            };

            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                // TODO: 这里接你的日志
            };

            AppDomain.CurrentDomain.FirstChanceException += (sender, e) =>
            {
                // 如无必要，不建议这里做重日志
            };

            TaskScheduler.UnobservedTaskException += (sender, e) =>
            {
                e.SetObserved();
            };

            Directory.CreateDirectory(rootCachePath);
            //CefSharpSettings.RuntimeStyle = CefRuntimeStyle.Chrome;
            CefSharpSettings.SubprocessExitIfParentProcessClosed = true;
            Cef.EnableWaitForBrowsersToClose();

            var settings = new CefSharp.OffScreen.CefSettings
            {

                BrowserSubprocessPath = defaultSubprocessPath,
                //RootCachePath = System.IO.Path.GetFullPath(CefCachePaths.RootCachePath),
                //CachePath = CefCachePaths.RootCachePath,
                PersistSessionCookies = false,
                PersistUserPreferences= false,
                WindowlessRenderingEnabled = true,

            };

            ///--disable-chrome-runtime
            /////incognito
            //settings.CefCommandLineArgs.Add("disable-chrome-runtime");
            //settings.CefCommandLineArgs.Add("incognito");
            settings.CefCommandLineArgs.Add("enable-media-stream");
            settings.CefCommandLineArgs.Add("use-fake-ui-for-media-stream");
            settings.CefCommandLineArgs.Add("enable-usermedia-screen-capturing");
            settings.SetOffScreenRenderingBestPerformanceArgs();

            Cef.Initialize(settings, performDependencyCheck: true, browserProcessHandler: null);

            Application.ApplicationExit += (sender, e) =>
            {
                if (Cef.IsInitialized)
                {
                    Cef.Shutdown();
                }
            };

            var mainForm = new MainForm();

            // 带管道参数：由主进程调度
            if (!string.IsNullOrWhiteSpace(pipeName))
            {
                var pipeHost = new PipeHostService(pipeName, mainForm);
                var appContext = new CefClientAppContext(mainForm, pipeHost);

                appContext.Start();
                Application.Run(appContext);
                return 0;
            }

            // 不带管道参数：本地直接调试运行
            Application.Run(mainForm);
            return 0;
        }
    }
}