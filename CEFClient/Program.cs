using CefSharp;
using CefSharp.WinForms;

namespace CefClient
{
    public class Program
    {
        private static readonly object LifecycleLogLock = new();
        private static readonly string LifecycleLogPath = Path.Combine(AppContext.BaseDirectory, "cefclient.lifecycle.log");

        private static void LogLifecycle(string message)
        {
            var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [PID:{Environment.ProcessId}] {message}{Environment.NewLine}";
            try
            {
                lock (LifecycleLogLock)
                {
                    File.AppendAllText(LifecycleLogPath, line);
                }
            }
            catch
            {
            }

            try
            {
                System.Diagnostics.Debug.WriteLine(line);
            }
            catch
            {
            }
        }
        [STAThread]
        public static int Main(string[] args)
        {
            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
            LogLifecycle($"Main start. args={string.Join(" ", args)}");

            var pipeName = args
                .FirstOrDefault(x => x.StartsWith("--pipe-name=", StringComparison.OrdinalIgnoreCase))
                ?.Substring("--pipe-name=".Length);

            var settings = new CefSettings();

            var defaultSubprocessPath = Path.Combine(AppContext.BaseDirectory, "CefSharp.BrowserSubprocess.exe");
            if (File.Exists(defaultSubprocessPath))
            {
                settings.BrowserSubprocessPath = defaultSubprocessPath;
                LogLifecycle($"Use standalone subprocess: {defaultSubprocessPath}");
            }
            else
            {
                LogLifecycle("Standalone subprocess missing, fallback to SelfHost.");
                // 兼容旧部署：找不到独立子进程时，回退到 SelfHost
                var exitCode = CefSharp.BrowserSubprocess.SelfHost.Main(args);
                if (exitCode >= 0)
                {
                    LogLifecycle($"SelfHost subprocess exit. code={exitCode}");
                    return exitCode;
                }

                settings.BrowserSubprocessPath = System.Diagnostics.Process.GetCurrentProcess().MainModule!.FileName;
                LogLifecycle($"Use self-host subprocess path: {settings.BrowserSubprocessPath}");
            }

            var rootCachePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "CefSharp");
            Directory.CreateDirectory(rootCachePath);
            settings.RootCachePath = rootCachePath;
            LogLifecycle($"Set RootCachePath={settings.RootCachePath}");

            settings.CefCommandLineArgs.Add("enable-media-stream");
            settings.CefCommandLineArgs.Add("use-fake-ui-for-media-stream");
            settings.CefCommandLineArgs.Add("enable-usermedia-screen-capturing");

            LogLifecycle($"Cef.Initialize begin. BrowserSubprocessPath={settings.BrowserSubprocessPath}");
            Cef.Initialize(settings, performDependencyCheck: false);
            LogLifecycle("Cef.Initialize completed.");
            ApplicationConfiguration.Initialize();
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            var mainForm = new MainForm();

            Application.ThreadException += (sender, e) =>
            {
                // TODO: 这里接你的日志
                LogLifecycle($"ThreadException: {e.Exception}");
                _ = mainForm.SendLogAsync($"ThreadException: {e.Exception.Message}");
            };

            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                // TODO: 这里接你的日志
                if (e.ExceptionObject is Exception ex)
                {
                    LogLifecycle($"UnhandledException: {ex}");
                    _ = mainForm.SendLogAsync($"UnhandledException: {ex.Message}");
                }
            };

            AppDomain.CurrentDomain.FirstChanceException += (sender, e) =>
            {
                // 如无必要，不建议这里做重日志
            };

            TaskScheduler.UnobservedTaskException += (sender, e) =>
            {
                LogLifecycle($"UnobservedTaskException: {e.Exception}");
                e.SetObserved();
            };

            mainForm.FormClosed += (_, _) =>
            {
                LogLifecycle("MainForm closed.");
            };

            Application.ThreadExit += (sender, e) =>
            {
                LogLifecycle("Application.ThreadExit fired.");
            };

            Application.ApplicationExit += (sender, e) =>
            {
                LogLifecycle($"ApplicationExit entered. Cef.IsInitialized={Cef.IsInitialized}");
                try
                {
                    if (Cef.IsInitialized ?? false)
                    {
                        LogLifecycle("Cef.Shutdown begin.");
                        Cef.Shutdown();
                        LogLifecycle("Cef.Shutdown completed.");
                    }
                    else
                    {
                        LogLifecycle("Skip Cef.Shutdown because Cef is not initialized.");
                    }
                }
                catch (Exception ex)
                {
                    LogLifecycle($"Cef.Shutdown failed: {ex}");
                }
            };
 

            // 带管道参数：由主进程调度
            if (!string.IsNullOrWhiteSpace(pipeName))
            {
                var pipeHost = new PipeHostService(pipeName, mainForm);
                mainForm.LogAsync = (message, token) => pipeHost.SendLogAsync($"MainForm: {message}", token);
                var appContext = new CefClientAppContext(mainForm, pipeHost);
                appContext.Start();
                LogLifecycle($"Run with pipe mode. pipe={pipeName}");
                Application.Run(appContext);
                LogLifecycle("Application.Run(appContext) returned.");
                return 0;
            }

            // 不带管道参数：本地直接调试运行
            LogLifecycle("Run in local debug mode (without pipe).");
            Application.Run(mainForm);
            LogLifecycle("Application.Run(mainForm) returned.");
            return 0;
        }
    }
}