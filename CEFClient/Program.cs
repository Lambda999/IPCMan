using CefSharp;
using CefSharp.WinForms;

namespace CefClient
{
    public class Program
    {
        [STAThread]
        public static int Main(string[] args)
        {
            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);

            var exitCode = CefSharp.BrowserSubprocess.SelfHost.Main(args);
            if (exitCode >= 0)
            {
                return exitCode;
            }

            var pipeName = args
                .FirstOrDefault(x => x.StartsWith("--pipe-name=", StringComparison.OrdinalIgnoreCase))
                ?.Substring("--pipe-name=".Length);

            Directory.CreateDirectory(CefCachePaths.RootCachePath);

            var settings = new CefSettings
            {
                BrowserSubprocessPath = System.Diagnostics.Process.GetCurrentProcess().MainModule!.FileName,
                //RootCachePath = CefCachePaths.RootCachePath,
                //CachePath = CefCachePaths.DefaultCachePath,
                PersistSessionCookies = false,
                //PersistUserPreferences = true
            };

            settings.CefCommandLineArgs.Add("enable-media-stream");
            settings.CefCommandLineArgs.Add("use-fake-ui-for-media-stream");
            settings.CefCommandLineArgs.Add("enable-usermedia-screen-capturing");

            Cef.Initialize(settings, performDependencyCheck: false);

            ApplicationConfiguration.Initialize();
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

            Application.ApplicationExit += (sender, e) =>
            {
                if (Cef.IsInitialized ?? false)
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