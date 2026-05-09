using MainClient.Common;
using MainClient.Infrastructure;
using MainClient.Ipc;
using MainClient.Logging;
using MainClient.LogViewer;
using MainClient.Models;
using MainClient.UiTask;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog.Events;
using System.Collections.Concurrent;
using System.Management;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading.Channels;
using System.Windows.Forms;

namespace MainClient
{
    public partial class MainForm : Form
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger _logger;
        private readonly AppSettings _appSettings;
        private readonly AdeHelper _adeHelper;
        private readonly IpHelper _ipHelper;
        private readonly ProxyTester _ipTester;



        #region 任务调度
        private PipelineRunner<JToken>? _pipeline;
        private UiTaskRunner? _uiRunner;
        private AppAutoRestart? _appAutoRestart;
        private readonly TaskStatsAggregator _aggregator;

        #endregion

        #region LogWrite

        private readonly ConcurrentQueue<UiLogItem> _uiLogBuffer = new();
        private readonly System.Windows.Forms.Timer _uiTimer = new();
        private CancellationTokenSource _uiLogCts = new();
        private int _flushing = 0;
        private const int MaxFlushCount = 500;
        // 新控件
        private LogViewerUltra logViewer;
        private void StartLogConsumer()
        {
            // 初始化新控件
            logViewer = new LogViewerUltra()
            {
                Dock = DockStyle.Fill
            };
            groupBox3.Controls.Add(logViewer);

            // 后台读取日志
            Task.Run(async () =>
            {
                var reader = UiLogChannel.Channel.Reader;

                try
                {
                    await foreach (var item in reader.ReadAllAsync(_uiLogCts.Token))
                    {
                        if (_uiLogCts.IsCancellationRequested)
                            break;

                        _uiLogBuffer.Enqueue(item);
                    }
                }
                catch (OperationCanceledException) { }

            }, _uiLogCts.Token);

            // UI Timer
            _uiTimer.Interval = 200;
            _uiTimer.Tick += (_, __) =>
            {
                if (Interlocked.Exchange(ref _flushing, 1) == 1)
                    return;

                try
                {
                    FlushLogsToUi();
                }
                finally
                {
                    Interlocked.Exchange(ref _flushing, 0);
                }
            };
            _uiTimer.Start();

            this.FormClosing += (s, e) =>
            {
                try
                {
                    _uiTimer.Stop();
                    _uiLogCts.Cancel();
                    UiLogChannel.Channel.Writer.TryComplete();
                }
                catch { }
            };
        }
        private void FlushLogsToUi()
        {
            if (IsDisposed || Disposing)
                return;

            if (!IsHandleCreated || logViewer.IsDisposed)
                return;

            if (_uiLogBuffer.IsEmpty)
                return;

            int count = 0;

            while (_uiLogBuffer.TryDequeue(out var item))
            {
                logViewer.WriteLog(item.Message, ConvertLevel(item.Level));

                if (++count >= MaxFlushCount)
                    break;
            }
        }
        // 日志级别映射
        private LogLevel ConvertLevel(LogEventLevel level) => level switch
        {
            LogEventLevel.Verbose => LogLevel.Trace,
            LogEventLevel.Debug => LogLevel.Debug,
            LogEventLevel.Information => LogLevel.Information,
            LogEventLevel.Warning => LogLevel.Warning,
            LogEventLevel.Error => LogLevel.Error,
            _ => LogLevel.Information
        };

        public void LogWriteLine(string message)
        {
            _logger.LogInformation(message);
        }

        #endregion


        #region 应用设置
        private void LoadAppSetting()
        {

            textBox_ProxyIpUrl.Text = _appSettings.ProxyIpUrl;
            textBox_TaskApiUrl.Text = _appSettings.TaskApiUrl;
            textBox_DevApiUrl.Text = _appSettings.DevApiUrl;
            numericUpDown_FetchTaskInterval.Value = _appSettings.FetchTaskInterval;
            numericUpDown_MaximumConcurrency.Value = _appSettings.MaximumConcurrency;
            textBox_TaskName.Text = _appSettings.TaskName;
            numericUpDown_Multiple.Value = _appSettings.Multiple;
            numericUpDown_MainResetTimeout.Value = _appSettings.MainResetTimeout;
            checkBox_IsHiddenMode.Checked = _appSettings.IsHiddenMode;
            checkBox_IsOsrMode.Checked = _appSettings.IsOsrMode;
            checkBox_IsProxyMode.Checked = _appSettings.IsProxyMode;

            numericUpDown_IpTtl.Value = _appSettings.IpTtl;
            numericUpDown_UVInterval.Value = Math.Max(numericUpDown_UVInterval.Minimum, Math.Min(numericUpDown_UVInterval.Maximum, _appSettings.UVInterval <= 0 ? 1000 : _appSettings.UVInterval));

        }
        private static object lock_config = new object();
        private void UpdateAppSetting()
        {
            lock (lock_config)
            {

                _appSettings.ProxyIpUrl = textBox_ProxyIpUrl.Text;
                _appSettings.TaskApiUrl = textBox_TaskApiUrl.Text;
                _appSettings.DevApiUrl = textBox_DevApiUrl.Text;
                _appSettings.FetchTaskInterval = (int)numericUpDown_FetchTaskInterval.Value;
                _appSettings.MaximumConcurrency = (int)numericUpDown_MaximumConcurrency.Value;
                _appSettings.TaskName = textBox_TaskName.Text;
                _appSettings.Multiple = (int)numericUpDown_Multiple.Value;
                _appSettings.MainResetTimeout = (int)numericUpDown_MainResetTimeout.Value;
                _appSettings.IsHiddenMode = checkBox_IsHiddenMode.Checked;
                _appSettings.IsOsrMode = checkBox_IsOsrMode.Checked;
                _appSettings.IsProxyMode = checkBox_IsProxyMode.Checked;
                _appSettings.IpTtl = (int)numericUpDown_IpTtl.Value;
                _appSettings.UVInterval = (int)numericUpDown_UVInterval.Value;


                UserConfigService.Save("AppSettings", _appSettings);
            }

        }
        #endregion



        public MainForm(
            AdeHelper adeHelper,
            IpHelper ipHelper,
            ProxyTester ipTester,
            TaskStatsAggregator aggregator,
            AppSettings appSettings,
            IHttpClientFactory httpClientFactory,
            ILogger<MainForm> logger)
        {
            InitializeComponent();
            this._adeHelper = adeHelper;
            this._ipHelper = ipHelper;
            this._ipTester = ipTester;
            this._aggregator = aggregator;
            this._appSettings = appSettings;
            this._logger = logger;
            this._httpClientFactory = httpClientFactory;

            LoadAppSetting();
            #region 数据初始化
            foreach (var item in new ManagementObjectSearcher("Select * from Win32_ComputerSystem").Get())
            {
                toolStripStatusLabel1.Text = $"CPU:{item["NumberOfLogicalProcessors"]}";
            }
            #endregion
        }


        private void MainForm_Load(object sender, EventArgs e)
        {
            StartLogConsumer();
            _logger.LogInformation("应用已启动");
            Task.Run(() =>
            {
                this.InvokeOnUiThreadIfRequired(() =>
                {

                    #region 控件初始化
                    var controls = new List<Control>() { groupBox2 };
                    foreach (var control in controls)
                    {
                        foreach (var c in control.Controls)
                        {
                            if (c is NumericUpDown)
                            {
                                (c as NumericUpDown).ValueChanged += (s, e) =>
                                {
                                    UpdateAppSetting();
                                };
                            }
                            else if (c is TextBox)
                            {
                                (c as TextBox).TextChanged += (s, e) =>
                                {
                                    UpdateAppSetting();
                                };
                            }
                            else if (c is CheckBox)
                            {
                                (c as CheckBox).Click += (s, e) =>
                                {
                                    UpdateAppSetting();
                                };
                            }
                            else if (c is RadioButton)
                            {
                                (c as RadioButton).Click += (s, e) =>
                                {
                                    UpdateAppSetting();
                                };
                            }
                            else if (c is ComboBox)
                            {
                                (c as ComboBox).SelectedIndexChanged += (s, e) =>
                                {
                                    UpdateAppSetting();
                                };
                            }
                        }
                    }
                    #endregion

                });
            });
        }



        /// <summary>
        /// 获取任务
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        private async Task ProducerAsync(ChannelWriter<JToken> writer, CancellationToken token)
        {
            Exception? completionError = null;

            try
            {
                var host = await CommonHelper.GetHostAsync();
                while (!token.IsCancellationRequested)
                {
                    var url = $"{_appSettings.TaskApiUrl}?type=1&action=getTask&name={_appSettings.TaskName}&host={System.Web.HttpUtility.UrlEncode(host)}&ver={AppConsts.AppVersion}&_t={DateTime.Now.Ticks}";
                    var res = await _adeHelper.GetTaskAsync(url, token);
                    if (string.IsNullOrWhiteSpace(res))
                    {
                        LogWriteLine("读取任务异常");
                        await Task.Delay(_appSettings.FetchTaskInterval, token);
                        continue;
                    }

                    JArray? data;
                    try
                    {
                        var json = JObject.Parse(res);
                        data = json["data"] as JArray;
                    }
                    catch (JsonReaderException)
                    {
                        _logger.LogError("ProducerAsync json parse failed: {Response}", res);
                        await Task.Delay(_appSettings.FetchTaskInterval, token);
                        continue;
                    }

                    if (data == null || data.Count == 0)
                    {
                        LogWriteLine("暂无任务");
                        await Task.Delay(_appSettings.FetchTaskInterval, token);
                        continue;
                    }

                    int multiple = Math.Max(1, _appSettings.Multiple);
                    int totalEnqueued = 0;
                    for (int i = 0; i < multiple; i++)
                    {
                        foreach (var item in data)
                        {
                            if (!await writer.WaitToWriteAsync(token))
                                return;

                            await writer.WriteAsync(item, token);
                            totalEnqueued++;
                        }
                    }

                    LogWriteLine($"新增{totalEnqueued}条任务");
                    await Task.Delay(_appSettings.FetchTaskInterval, token);
                }
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {

            }
            catch (Exception ex)
            {
                completionError = ex;
                throw;
            }
            finally
            {
                writer.TryComplete(completionError);
            }
        }

        /// <summary>
        /// 消费任务
        /// </summary>
        /// <param name="consumerId"></param>
        /// <param name="task"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        private async Task ConsumerAsyncv2(int consumerId, JToken task, CancellationToken token)
        {
            try
            {
                token.ThrowIfCancellationRequested();

                var parseResult = ParseTask(task);
                if (!parseResult.Success)
                {
                    _logger.LogWarning("ConsumerAsync skip malformed task: {Task}", task?.ToString(Newtonsoft.Json.Formatting.None));
                    return;
                }

                var ctx = parseResult.Context!;
                ApplyUvPvOverrides(ctx);
                ///检测设备接口是否可用
                var check_dev = await GetDeviceForTaskAsync(ctx.OS, ctx.TaskId, 0, token);
                if (check_dev == null)
                {
                    _logger.LogWarning("ConsumerAsync get device failed after retries. taskId={TaskId}, uv={Uv}", ctx.TaskId, 1);
                    return;
                }

                await PrepareProxyContextAsync(ctx, task, token);

                var ipTtlSeconds = _appSettings.IpTtl;
                if (ipTtlSeconds <= 0)
                {
                    _logger.LogWarning("ConsumerAsync invalid IpTtl={IpTtl}, taskId={TaskId}", ipTtlSeconds, ctx.TaskId);
                    return;
                }

                using var ipTtlCts = new CancellationTokenSource(TimeSpan.FromSeconds(ipTtlSeconds));
                using var consumerLinkedCts = CancellationTokenSource.CreateLinkedTokenSource(token, ipTtlCts.Token);
                var consumerToken = consumerLinkedCts.Token;

                for (int uvIndex = 0; uvIndex < ctx.TotalUV; uvIndex++)
                {
                    if (token.IsCancellationRequested)
                        return;

                    try
                    {
                        _aggregator.Enqueue(new TaskEvent(ctx.TaskId, StateType.Request, 1));

                        var dev = await GetDeviceForTaskAsync(ctx.OS, ctx.TaskId, uvIndex, consumerToken);
                        if (dev == null)
                            continue;

                        NormalizeDevice(dev, ctx.OS);

                        var pluginArgs = BuildPluginArgs(ctx, task, dev, consumerId, uvIndex);

                        bool stopRemainingUv = await ExecutePluginOnceAsync(
                            ctx,
                            pluginArgs,
                            consumerId,
                            uvIndex,
                            consumerToken);

                        if (stopRemainingUv)
                            break;
                    }
                    catch (OperationCanceledException) when (token.IsCancellationRequested)
                    {
                        return;
                    }
                    catch (OperationCanceledException) when (ipTtlCts.IsCancellationRequested)
                    {
                        LogWriteLine($"任务 {ctx.TaskTitle}[{ctx.TaskId}] 的 IP 总有效时长已到，停止后续 UV。");
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex,
                            "ConsumerAsync uv failed. taskId={TaskId}, uv={Uv}, consumer={ConsumerId}",
                            ctx.TaskId, uvIndex + 1, consumerId);
                    }
                }
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"ConsumerAsync failed:{ex.Message}");
            }
        }


        private async Task ConsumerAsync(int consumerId, JToken task, CancellationToken token)
        {
            try
            {
                token.ThrowIfCancellationRequested();

                var parseResult = ParseTask(task);
                if (!parseResult.Success)
                {
                    _logger.LogWarning("ConsumerAsync skip malformed task: {Task}", task?.ToString(Newtonsoft.Json.Formatting.None));
                    return;
                }

                var ctx = parseResult.Context!;
                ApplyUvPvOverrides(ctx);


                var check_dev = await GetDeviceForTaskAsync(ctx.OS, ctx.TaskId, 0, token);
                if (check_dev == null)
                {
                    _logger.LogWarning("ConsumerAsync get device failed after retries. taskId={TaskId}, uv={Uv}", ctx.TaskId, 1);
                    return;
                }

                await PrepareProxyContextAsync(ctx, task, token);

                var ipTtlSeconds = _appSettings.IpTtl;
                if (ipTtlSeconds <= 0)
                {
                    _logger.LogWarning("ConsumerAsync invalid IpTtl={IpTtl}, taskId={TaskId}", ipTtlSeconds, ctx.TaskId);
                    return;
                }

                bool stopRemainingUv = await ExecuteTaskByCefClientAsync(
                    ctx,
                    task,
                    consumerId,
                    token);

                if (stopRemainingUv)
                {
                    _logger.LogInformation("ConsumerAsync stop remaining uv. taskId={TaskId}", ctx.TaskId);
                }
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                return;
            }
            catch (IOException ex) when (ex.Message.Contains("Pipe is broken", StringComparison.OrdinalIgnoreCase) || ex.Message.Contains("pipe has been ended", StringComparison.OrdinalIgnoreCase))
            {
                //_logger.LogDebug(ex, "Pipe closed during shutdown. taskId={TaskId}", ctx.TaskId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ConsumerAsync failed:{Message}", ex.Message);
            }
        }


        private async Task<bool> ExecuteTaskByCefClientAsync(
           ConsumerTaskContext ctx,
           JToken rawTask,
           int consumerId,
           CancellationToken token)
        {
            ctx.UniqueId = Guid.NewGuid().ToString("D");

            var cefProcessDirectory = _appSettings.IsOsrMode ? "CefClient" : "CefClient";
            var cefProcessFileName = _appSettings.IsOsrMode ? "CefClient.OffScreen.exe" : "CefClient.exe";
            var cefExePath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                cefProcessDirectory,
                cefProcessFileName);

            _logger.LogInformation(
                "Use {CefProcessFileName} for taskId={TaskId}, osrMode={IsOsrMode}",
                cefProcessFileName,
                ctx.TaskId,
                _appSettings.IsOsrMode);

            await using var session = new CefClientSession(cefExePath, TimeSpan.FromSeconds(15));

            session.OnLog += message =>
            {
                _logger.LogInformation("CefClient[{TaskId}] {Message}", ctx.TaskId, message);
                return Task.CompletedTask;
            };
            session.OnBrowserStatus += status =>
            {
                var stage = status.Data?["stage"]?.GetValue<string>() ?? "unknown";
                var browserId = status.BrowserId ?? string.Empty;
                _logger.LogInformation(
                    "CefClient browser status. taskId={TaskId}, browserId={BrowserId}, stage={Stage}, success={Success}, msg={Message}",
                    ctx.TaskId,
                    browserId,
                    stage,
                    status.Success,
                    status.Message);
                return Task.CompletedTask;
            };

            var completedUvTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            int dispatchedUvCount = 0;
            int completedUvCount = 0;
            bool stopRemainingUvByResult = false;
            var inFlightBrowsers = new ConcurrentDictionary<string, byte>(StringComparer.OrdinalIgnoreCase);
            var uvRunTimeout = TimeSpan.FromSeconds(90);

            void TryCompleteAll()
            {
                if (Volatile.Read(ref dispatchedUvCount) <= 0)
                    return;

                if (Volatile.Read(ref completedUvCount) >= Volatile.Read(ref dispatchedUvCount))
                    completedUvTcs.TrySetResult(true);
            }

            Task StartUvTimeoutWatchdogAsync(string browserId, CancellationToken watchdogToken)
            {
                return Task.Run(async () =>
                {
                    try
                    {
                        await Task.Delay(uvRunTimeout, watchdogToken);
                    }
                    catch (OperationCanceledException)
                    {
                        return;
                    }

                    if (!inFlightBrowsers.TryRemove(browserId, out _))
                        return;

                    var done = Interlocked.Increment(ref completedUvCount);
                    _logger.LogWarning(
                        "UV run timeout fallback. taskId={TaskId}, browserId={BrowserId}, timeout={TimeoutSeconds}s, completed={Completed}/{Dispatched}",
                        ctx.TaskId,
                        browserId,
                        (int)uvRunTimeout.TotalSeconds,
                        done,
                        Volatile.Read(ref dispatchedUvCount));

                    try
                    {
                        await session.RemoveBrowserAsync(ctx.UniqueId, browserId, CancellationToken.None);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex,
                            "Timeout fallback remove browser failed. taskId={TaskId}, browserId={BrowserId}",
                            ctx.TaskId,
                            browserId);
                    }

                    TryCompleteAll();
                }, CancellationToken.None);
            }

            session.OnBrowserResult += async response =>
            {
                if (_appSettings.IsOsrMode)
                    return;

                if (!string.Equals(response.TaskId, ctx.UniqueId, StringComparison.OrdinalIgnoreCase))
                    return;

                if (string.IsNullOrWhiteSpace(response.BrowserId))
                    return;

                if (!inFlightBrowsers.TryRemove(response.BrowserId, out _))
                {
                    _logger.LogDebug(
                        "Ignore duplicated or late browserResult. taskId={TaskId}, browserId={BrowserId}",
                        ctx.TaskId,
                        response.BrowserId);
                    return;
                }

                var uvNumber = Interlocked.Increment(ref completedUvCount);
                _logger.LogInformation(
                    "RunBrowserAsync done. taskId={TaskId}, uv={Uv}, browserId={BrowserId}, success={Success}, msg={Message}",
                    ctx.TaskId,
                    uvNumber,
                    response.BrowserId,
                    response.Success,
                    response.Message);

                var result = new BrowserRunResponse
                {
                    Success = response.Success ?? false,
                    Message = response.Message ?? string.Empty,
                    Data = response.Data
                };

                if (ShouldStopRemainingUv(ctx, result))
                {
                    stopRemainingUvByResult = true;
                }

                var removedByCefClient = response.Data?["removedByCefClient"]?.GetValue<bool?>() ?? false;
                if (!removedByCefClient)
                {
                    try
                    {
                        await session.RemoveBrowserAsync(ctx.UniqueId, response.BrowserId, CancellationToken.None);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex,
                            "RemoveBrowserAsync failed after browserResult. taskId={TaskId}, browserId={BrowserId}",
                            ctx.TaskId, response.BrowserId);
                    }
                }

                if (Volatile.Read(ref completedUvCount) >= Volatile.Read(ref dispatchedUvCount))
                {
                    completedUvTcs.TrySetResult(true);
                }
            };

            try
            {
                await session.StartAsync(token);

                var startPayload = BuildStartPayload(ctx, rawTask);
                await session.StartTaskAsync(ctx.UniqueId, startPayload, token);

                var ipTtlSeconds = _appSettings.IpTtl;
                using var ipTtlCts = new CancellationTokenSource(TimeSpan.FromSeconds(ipTtlSeconds));
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(token, ipTtlCts.Token);
                var innerToken = linkedCts.Token;
                var uvIntervalMs = Math.Max(1000, _appSettings.UVInterval <= 0 ? 1000 : _appSettings.UVInterval);

                if (_appSettings.IsOsrMode)
                {
                    for (int uvIndex = 0; uvIndex < ctx.TotalUV; uvIndex++)
                    {
                        if (token.IsCancellationRequested)
                            return false;

                        string browserId = $"uv_{uvIndex + 1}";

                        try
                        {
                            var dev = await GetDeviceForTaskAsync(ctx.OS, ctx.TaskId, uvIndex, innerToken);
                            if (dev == null)
                            {
                                _logger.LogWarning("GetDeviceForTaskAsync failed. taskId={TaskId}, uv={Uv}",
                                    ctx.TaskId, uvIndex + 1);
                                continue;
                            }

                            NormalizeDevice(dev, ctx.OS);

                            var uvPayload = BuildRunBrowserPayload(ctx, rawTask, dev, consumerId, uvIndex);
                            var response = await session.RunBrowserAsync(
                                ctx.UniqueId,
                                browserId,
                                uvPayload,
                                innerToken);

                            _logger.LogInformation(
                                "OSR RunBrowserAsync done. taskId={TaskId}, uv={Uv}, browserId={BrowserId}, success={Success}, msg={Message}",
                                ctx.TaskId,
                                uvIndex + 1,
                                browserId,
                                response.Success,
                                response.Message);

                            if (ShouldStopRemainingUv(ctx, response))
                                return true;

                            if (uvIndex < ctx.TotalUV - 1)
                                await Task.Delay(uvIntervalMs, innerToken);
                        }
                        catch (OperationCanceledException) when (token.IsCancellationRequested)
                        {
                            return false;
                        }
                        catch (OperationCanceledException) when (ipTtlCts.IsCancellationRequested)
                        {
                            LogWriteLine($"任务 {ctx.TaskTitle}[{ctx.TaskId}] 的 IP 总有效时长已到，停止后续 UV。");
                            return true;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex,
                                "ExecuteTaskByCefClientAsync OSR uv failed. taskId={TaskId}, uv={Uv}, consumer={ConsumerId}",
                                ctx.TaskId, uvIndex + 1, consumerId);
                        }
                    }

                    return false;
                }

                for (int uvIndex = 0; uvIndex < ctx.TotalUV; uvIndex++)
                {
                    if (token.IsCancellationRequested)
                        return false;

                    string browserId = $"uv_{uvIndex + 1}";

                    try
                    {
                        var dev = await GetDeviceForTaskAsync(ctx.OS, ctx.TaskId, uvIndex, innerToken);
                        if (dev == null)
                        {
                            _logger.LogWarning("GetDeviceForTaskAsync failed. taskId={TaskId}, uv={Uv}",
                                ctx.TaskId, uvIndex + 1);
                            continue;
                        }

                        NormalizeDevice(dev, ctx.OS);

                        if (!inFlightBrowsers.TryAdd(browserId, 0))
                        {
                            _logger.LogWarning(
                                "Duplicated in-flight browserId. taskId={TaskId}, browserId={BrowserId}",
                                ctx.TaskId,
                                browserId);
                            continue;
                        }

                        Interlocked.Increment(ref dispatchedUvCount);
                        _ = StartUvTimeoutWatchdogAsync(browserId, innerToken);

                        try
                        {
                            await session.CreateBrowserAsync(ctx.UniqueId, browserId, innerToken);

                            var uvPayload = BuildRunBrowserPayload(ctx, rawTask, dev, consumerId, uvIndex);
                            await session.RunBrowserNoWaitAsync(
                                ctx.UniqueId,
                                browserId,
                                uvPayload,
                                innerToken);
                        }
                        catch
                        {
                            inFlightBrowsers.TryRemove(browserId, out _);
                            Interlocked.Decrement(ref dispatchedUvCount);
                            try
                            {
                                await session.RemoveBrowserAsync(ctx.UniqueId, browserId, CancellationToken.None);
                            }
                            catch
                            {
                            }
                            throw;
                        }

                        if (uvIndex < ctx.TotalUV - 1)
                            await Task.Delay(uvIntervalMs, innerToken);
                    }
                    catch (OperationCanceledException) when (token.IsCancellationRequested)
                    {
                        return false;
                    }
                    catch (OperationCanceledException) when (ipTtlCts.IsCancellationRequested)
                    {
                        LogWriteLine($"任务 {ctx.TaskTitle}[{ctx.TaskId}] 的 IP 总有效时长已到，停止后续 UV。");
                        return true;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex,
                            "ExecuteTaskByCefClientAsync uv failed. taskId={TaskId}, uv={Uv}, consumer={ConsumerId}",
                            ctx.TaskId, uvIndex + 1, consumerId);
                    }
                }

                if (Volatile.Read(ref dispatchedUvCount) <= 0)
                    return false;

                TryCompleteAll();

                using var completeReg = innerToken.Register(() => completedUvTcs.TrySetCanceled(innerToken));
                await completedUvTcs.Task;

                return stopRemainingUvByResult;
            }
            finally
            {
                try
                {
                    await session.CloseGracefullyAsync(TimeSpan.FromSeconds(2));
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "CloseGracefullyAsync failed. taskId={TaskId}", ctx.TaskId);
                }
            }
        }



        private JsonObject BuildStartPayload(ConsumerTaskContext ctx, JToken rawTask)
        {
            return new JsonObject
            {
                ["taskId"] = ctx.UniqueId,
                ["taskTitle"] = ctx.TaskTitle ?? "",
                ["os"] = (int)(ctx.OS),
                ["totalUv"] = ctx.TotalUV,
                ["rawTask"] = rawTask.ToString(Newtonsoft.Json.Formatting.None)
            };
        }

        private JsonObject BuildRunBrowserPayload(
            ConsumerTaskContext ctx,
            JToken rawTask,
            object dev,
            int consumerId,
            int uvIndex)
        {
            return new JsonObject
            {
                ["taskId"] = ctx.UniqueId,
                ["taskTitle"] = ctx.TaskTitle ?? "",
                ["uvIndex"] = uvIndex,
                ["uv"] = uvIndex + 1,
                ["consumerId"] = consumerId,
                ["os"] = (int)(ctx.OS),
                ["device"] = JsonNode.Parse(System.Text.Json.JsonSerializer.Serialize(dev)),
                ["userAgent"] = dev is JToken devToken ? devToken["ua"]?.Value<string>() ?? string.Empty : string.Empty,
                ["isProxyMode"] = _appSettings.IsProxyMode,
                ["proxy_server"] = ctx.ProxyServer ?? string.Empty,
                ["rawTask"] = rawTask.ToString(Newtonsoft.Json.Formatting.None),
                ["url"] = ctx.Url,
                // OSR 端用这些短超时防止慢页面长期占住本次 UV，影响后续任务调度。
                ["loadTimeoutMs"] = 8000,
                ["firstScreenshotDelayMs"] = 500,
                ["finalScreenshotDelayMs"] = 1500,
                ["screenshotTimeoutMs"] = 1500,
                ["titleTimeoutMs"] = 1000,
            };
        }

        private bool ShouldStopRemainingUv(ConsumerTaskContext ctx, BrowserRunResponse result)
        {
            // 这里你先按你自己的业务判断
            // 例如 result.Data 里回了 stopRemainingUv = true
            var stop = result.Data?["stopRemainingUv"]?.GetValue<bool?>() ?? false;
            return stop;
        }


        /// <summary>
        /// 解析任务
        /// </summary>
        /// <param name="task"></param>
        /// <returns></returns>
        private ParseTaskResult ParseTask(JToken task)
        {
            if (task is not JObject taskObj)
                return new ParseTaskResult { Success = false };

            var taskIdToken = taskObj["id"];
            var url = taskObj["url"]?.Value<string>();
            var totalUvToken = taskObj["uv"];
            var totalPvToken = taskObj["pv"];

            if (taskIdToken == null || totalUvToken == null || totalPvToken == null || string.IsNullOrWhiteSpace(url))
                return new ParseTaskResult { Success = false };

            var devClientId = taskObj["client"]?.Value<string>()?
                .Split(new[] { "|" }, StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault() ?? "0";

            var ctx = new ConsumerTaskContext
            {
                TaskId = taskIdToken.Value<int>(),
                Url = url,
                TotalUV = Math.Max(1, totalUvToken.Value<int>()),
                TotalPV = Math.Max(1, totalPvToken.Value<int>()),
                DevClientId = devClientId,
                OS = _adeHelper.GetOS(devClientId),
                TaskTitle = taskObj["title"]?.Value<string>() ?? string.Empty,
                StartTime = DateTime.Now
            };

            return new ParseTaskResult
            {
                Success = true,
                Context = ctx
            };
        }
        /// <summary>
        /// 应用 UV / PV 覆盖配置
        /// </summary>
        /// <param name="ctx"></param>
        private void ApplyUvPvOverrides(ConsumerTaskContext ctx)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(_appSettings.UVOverride))
                {
                    var uvValues = _appSettings.UVOverride.Split(
                        '-',
                        StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                    if (uvValues.Length > 1 &&
                        int.TryParse(uvValues[0], out var minUv) &&
                        int.TryParse(uvValues[1], out var maxUv) &&
                        maxUv >= minUv)
                    {
                        ctx.TotalUV = CommonHelper.RandomRange(minUv, maxUv + 1);
                    }
                    else if (uvValues.Length == 1 && int.TryParse(uvValues[0], out var uvExact))
                    {
                        ctx.TotalUV = uvExact;
                    }
                }

                if (!string.IsNullOrWhiteSpace(_appSettings.PVOverride))
                {
                    var pvValues = _appSettings.PVOverride.Split(
                        '-',
                        StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                    if (pvValues.Length > 1 &&
                        int.TryParse(pvValues[0], out var minPv) &&
                        int.TryParse(pvValues[1], out var maxPv) &&
                        maxPv >= minPv)
                    {
                        ctx.TotalPV = CommonHelper.RandomRange(minPv, maxPv + 1);
                        if (ctx.TotalUV == 2)
                            ctx.TotalPV = minPv;
                    }
                    else if (pvValues.Length == 1 && int.TryParse(pvValues[0], out var pvExact))
                    {
                        ctx.TotalPV = pvExact;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "ConsumerAsync override parse failed, fallback to task values. taskId={TaskId}",
                    ctx.TaskId);
            }

            ctx.TotalUV = Math.Max(1, ctx.TotalUV);
            ctx.TotalPV = Math.Max(1, ctx.TotalPV);
        }

        #region 代理 / IP 信息
        /// <summary>
        /// 准备代理 / IP 信息
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="task"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        private async Task PrepareProxyContextAsync(ConsumerTaskContext ctx, JToken task, CancellationToken token)
        {
            ctx.ProxyServer = null;
            ctx.RealIp = string.Empty;
            ctx.IpInfo = null;

            if (_appSettings.IsProxyMode)
            {
                if (!string.IsNullOrWhiteSpace(_appSettings.ProxyIpUrl))
                {
                    await PrepareRemoteProxyAsync(ctx, task, token);
                }
                else
                {
                    await PrepareLocalProxyAsync(ctx, token);
                }
            }
            else
            {
                await PrepareDirectNetworkIpInfoAsync(ctx, token);
            }
        }
        /// <summary>
        /// 远程代理模式
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="task"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        private async Task PrepareRemoteProxyAsync(ConsumerTaskContext ctx, JToken task, CancellationToken token)
        {
            const int maxRetry = 10;

            for (int retry = 1; retry <= maxRetry; retry++)
            {
                token.ThrowIfCancellationRequested();

                try
                {
                    _aggregator.EnqueueProxyIpFetched(ctx.TaskId, 1);

                    var ipEntity = await _ipHelper.GetProxyIpAsync(task);
                    if (ipEntity == null)
                    {
                        LogWriteLine("获取IP错误");
                        await Task.Delay(Random.Shared.Next(100, 200), token);
                        continue;
                    }

                    FillProxyServerFromEntity(ctx, ipEntity);

                    if (string.IsNullOrWhiteSpace(ctx.ProxyServer) || !IsValidProxyServer(ctx.ProxyServer))
                    {
                        LogWriteLine($"IP异常,{ctx.ProxyServer}");
                        await Task.Delay(Random.Shared.Next(100, 200), token);
                        continue;
                    }

                    if (_appSettings.GetIpInfo || _appSettings.IsRealIp)
                    {
                        var ok = await TryFillIpInfoAsync(ctx, token);
                        if (!ok)
                        {
                            LogWriteLine($"无法获取IP信息,{ctx.ProxyServer}");
                            await Task.Delay(Random.Shared.Next(100, 200), token);
                            continue;
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(ctx.RealIp))
                    {
                        _aggregator.EnqueueProxyIpConsumed(ctx.TaskId, ctx.RealIp, 1);
                    }

                    return;
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    LogWriteLine($"IP异常,{ex.Message}");

                    if (ex.Message.Contains("没有满足您选择的条件IP"))
                        await Task.Delay(Random.Shared.Next(2000, 3000), token);

                    await Task.Delay(Random.Shared.Next(300, 500), token);
                }
            }

            throw new InvalidOperationException($"获取代理 IP 失败，taskId={ctx.TaskId}");
        }
        /// <summary>
        /// 本地代理模式
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        private async Task PrepareLocalProxyAsync(ConsumerTaskContext ctx, CancellationToken token)
        {
            ctx.ProxyServer = "127.0.0.1:7890";

            var result = await _ipTester.TestAsync(ctx.ProxyServer);
            if (!result.IsValid)
            {
                LogWriteLine($"无法获取IP信息,{ctx.ProxyServer}");
                throw new InvalidOperationException($"无法获取IP信息,{ctx.ProxyServer}");
            }

            ApplyIpTestResult(ctx, result);
        }
        /// <summary>
        /// 非代理模式
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        private async Task PrepareDirectNetworkIpInfoAsync(ConsumerTaskContext ctx, CancellationToken token)
        {
            if (!_appSettings.GetIpInfo && !_appSettings.IsRealIp)
                return;

            var result = await _ipTester.TestAsync(ctx.ProxyServer);
            if (!result.IsValid)
            {
                LogWriteLine($"无法获取IP信息,{ctx.ProxyServer}");
                throw new InvalidOperationException($"无法获取IP信息,{ctx.ProxyServer}");
            }

            ApplyIpTestResult(ctx, result);
        }
        #endregion

        #region 辅助方法：填代理 / 验证代理 / 填 IP 结果
        /// <summary>
        /// 辅助方法：填代理 / 验证代理 / 填 IP 结果
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="ipEntity"></param>
        private void FillProxyServerFromEntity(ConsumerTaskContext ctx, dynamic ipEntity)
        {
            if (ipEntity.format == IPFormat.JSON)
            {
                ctx.ProxyServer = $"{ipEntity.json["ip"]}:{ipEntity.json["port"]}";

                if (_appSettings.IsRealIp)
                {
                    ctx.RealIp =
                        ipEntity.json["rip"]?.Value<string>() ??
                        ipEntity.json["real_ip"]?.Value<string>() ??
                        ipEntity.json["realIp"]?.Value<string>() ??
                        string.Empty;
                }
            }
            else
            {
                ctx.ProxyServer = ipEntity.value;
                if (_appSettings.IsRealIp)
                    ctx.RealIp = ctx.ProxyServer ?? string.Empty;
            }
        }

        /// <summary>
        /// 验证代理1
        /// </summary>
        /// <param name="proxyServer"></param>
        /// <returns></returns>
        private bool IsValidProxyServer(string proxyServer)
        {
            const string pattern = @"(?:(?:[0,1]?\d?\d|2[0-4]\d|25[0-5])\.){3}(?:[0,1]?\d?\d|2[0-4]\d|25[0-5]):\d{1,5}";
            return Regex.IsMatch(proxyServer, pattern);
        }

        /// <summary>
        /// 验证代理2
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        private async Task<bool> TryFillIpInfoAsync(ConsumerTaskContext ctx, CancellationToken token)
        {
            var result = await _ipTester.TestAsync(ctx.ProxyServer);
            if (!result.IsValid)
                return false;

            ApplyIpTestResult(ctx, result);
            return true;
        }
        /// <summary>
        /// 验证代理3
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="result"></param>

        private void ApplyIpTestResult(ConsumerTaskContext ctx, dynamic result)
        {
            if (result.SuccessUrl.Equals("http://ip-api.com/json") ||
                result.SuccessUrl.Equals("http://117.21.200.221/api/dash/ipinfo.php") ||
                result.SuccessUrl.Equals("http://211.154.24.179:9000/api/dash/ipinfo.php"))
            {
                ctx.IpInfo = JObject.Parse(result.Data);
                ctx.RealIp = ctx.IpInfo["query"]?.Value<string>() ?? string.Empty;
            }
            else
            {
                var ipJson = JObject.Parse(result.Data);

                if (ipJson.ContainsKey("query"))
                    ctx.RealIp = ipJson["query"]?.Value<string>() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(ctx.RealIp) && ipJson.ContainsKey("ip"))
                    ctx.RealIp = ipJson["ip"]?.Value<string>() ?? string.Empty;

                ctx.IpInfo = new JObject
                {
                    ["query"] = ctx.RealIp
                };
            }
        }
        #endregion

        /// <summary>
        /// 获取设备
        /// </summary>
        /// <param name="os"></param>
        /// <param name="taskId"></param>
        /// <param name="uvIndex"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        private async Task<JToken?> GetDeviceForTaskAsync(OSType os, int taskId, int uvIndex, CancellationToken token)
        {
            for (int retry = 0; retry < 5; retry++)
            {
                token.ThrowIfCancellationRequested();

                var dev = await _adeHelper.GetDeviceAsync(os, 100);
                if (dev != null)
                    return dev;
            }

            _logger.LogWarning(
                "ConsumerAsync get device failed after retries. taskId={TaskId}, uv={Uv}",
                taskId, uvIndex + 1);

            return null;
        }

        /// <summary>
        /// 标准化设备信息
        /// </summary>
        /// <param name="dev"></param>
        /// <param name="os"></param>
        private void NormalizeDevice(JToken dev, OSType os)
        {
            var ua = dev["ua"]?.Value<string>() ?? string.Empty;

            if (os == OSType.ANDROID)
            {

            }
            else if (os == OSType.IOS)
            {
                dev["full_version"] = dev["osv"];
            }
            else if (os == OSType.PC)
            {
                dev["gpu"] = dev["renderer"];
                dev["vendor"] = dev["vender"];

            }
        }

        /// <summary>
        /// 构造插件参数
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="task"></param>
        /// <param name="dev"></param>
        /// <param name="consumerId"></param>
        /// <param name="uvIndex"></param>
        /// <returns></returns>
        private JObject BuildPluginArgs(ConsumerTaskContext ctx, JToken task, JToken dev, int consumerId, int uvIndex)
        {
            var cacheName = $"s{consumerId}_{uvIndex + 1}";

            var args = new JObject
            {
                ["task"] = task,
                ["dev"] = dev,
                ["ipInfo"] = ctx.IpInfo,
                ["isProxyMode"] = _appSettings.IsProxyMode,
                ["proxy_server"] = ctx.ProxyServer,
                ["realIp"] = ctx.RealIp,
                ["isHiddenMode"] = _appSettings.IsHiddenMode,
                ["cacheName"] = cacheName,
                ["processIndex"] = consumerId,
                ["totalPV"] = ctx.TotalPV,
                ["currentUV"] = uvIndex + 1,
                ["os"] = (int)ctx.OS,
                ["isTest"] = _appSettings.IsTest,
            };

            return args;
        }

        /// <summary>
        /// 执行插件
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="args"></param>
        /// <param name="consumerId"></param>
        /// <param name="uvIndex"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        private async Task<bool> ExecutePluginOnceAsync(
        ConsumerTaskContext ctx,
        JObject args,
        int consumerId,
        int uvIndex,
        CancellationToken token)
        {
            LogWriteLine("提交任务");
            //if (!allPlugins.TryGetValue(_appSettings.QTPName, out var plugin) || plugin.type == null)
            //{
            //    _logger.LogError("ConsumerAsync plugin not found: {PluginName}", _appSettings.QTPName);
            //    return false;
            //}

            //var pluginInstance = Activator.CreateInstance(
            //    plugin.type,
            //    new object[] { _domainService, _playwrightProvider, _aggregator, _processManager, _adeHelper, _nameGenerator, _appSettings });

            //if (pluginInstance is not IQTPService pluginService)
            //{
            //    _logger.LogWarning("ConsumerAsync plugin instance invalid. plugin={PluginName}", _appSettings.QTPName);
            //    return false;
            //}

            //var uniqueId = Guid.NewGuid().ToString("D");

            //EventHandler<PluginLogEventArgs>? logHandler = null;
            //EventHandler<TaskStateChangedEventArgs>? stateChangedHandler = null;
            //EventHandler<TaskAdWordEventArgs>? adWordHandler = null;

            //try
            //{
            //    logHandler = (s, e) => LogWriteLine(e);
            //    stateChangedHandler = (s, e) =>
            //    {
            //        _aggregator.Enqueue(new TaskEvent(e.Id, e.Type, e.Count, e.Data));
            //    };
            //    adWordHandler = (s, e) =>
            //    {
            //        _aggregator.EnqueueAdWord(e.Type, e.Word);
            //    };

            //    pluginService.OnLogEventHandler += logHandler;
            //    pluginService.OnStateChangedEventHandler += stateChangedHandler;
            //    pluginService.OnTaskAdWordEventHandler += adWordHandler;

            //    LogWriteLine(
            //        $"提交任务:{ctx.TaskTitle}[{ctx.TaskId}_{consumerId}_s{consumerId}_{uvIndex + 1}],os={ctx.OS},proxy={ctx.ProxyServer ?? "False"},realIp={ctx.RealIp},uv={ctx.TotalUV}/{uvIndex + 1}");
            //    LogWriteLine($"任务追踪: taskId={ctx.TaskId}, uniqueId={uniqueId}, consumerId={consumerId}, uvIndex={uvIndex + 1}");

            //    try
            //    {
            //        var (_, isPageTriggerClick, _) =
            //            await pluginService.ExecuteWorkerAsync(uniqueId, args, token);

            //        if (ctx.TotalUV > 1 && isPageTriggerClick && _appSettings.UVsTriggerOne)
            //            return true;
            //    }
            //    catch (OperationCanceledException) when (token.IsCancellationRequested)
            //    {
            //        throw;
            //    }
            //    catch (Exception ex)
            //    {
            //        _logger.LogError(ex,
            //            "ConsumerAsync plugin execute failed. taskId={TaskId}, consumer={ConsumerId}",
            //            ctx.TaskId, consumerId);
            //    }

            //    return false;
            //}
            //finally
            //{
            //    if (logHandler != null) pluginService.OnLogEventHandler -= logHandler;
            //    if (stateChangedHandler != null) pluginService.OnStateChangedEventHandler -= stateChangedHandler;
            //    if (adWordHandler != null) pluginService.OnTaskAdWordEventHandler -= adWordHandler;

            //    try
            //    {
            //        await _processManager.CloseAsync(uniqueId);
            //    }
            //    catch (Exception ex)
            //    {
            //        _logger.LogWarning(ex, "Close process failed. uniqueId={UniqueId}", uniqueId);
            //    }

            //    if (pluginService is IAsyncDisposable asyncDisposable)
            //    {
            //        try
            //        {
            //            await asyncDisposable.DisposeAsync();
            //        }
            //        catch (Exception ex)
            //        {
            //            _logger.LogWarning(ex, "Async dispose plugin failed. uniqueId={UniqueId}", uniqueId);
            //        }
            //    }
            //    else if (pluginService is IDisposable disposable)
            //    {
            //        try
            //        {
            //            disposable.Dispose();
            //        }
            //        catch (Exception ex)
            //        {
            //            _logger.LogWarning(ex, "Dispose plugin failed. uniqueId={UniqueId}", uniqueId);
            //        }
            //    }
            //}
            return await Task.FromResult(true);
        }


        private void InitPipelineRunner()
        {
            int capacity = Math.Max(1, _appSettings.Multiple * _appSettings.MaximumConcurrency);
            int consumerCount = Math.Max(1, _appSettings.MaximumConcurrency);
            _pipeline = new PipelineRunner<JToken>(
                capacity,
                consumerCount,
                ProducerAsync,
                ConsumerAsync
            );
            _pipeline.ProgressChanged += _ =>
            {
                if (IsDisposed || Disposing)
                    return;
            };
            _pipeline.Started += () =>
            {
                this.InvokeOnUiThreadIfRequired(() =>
                {
                    lblStatus.Text = "任务状态：Running";
                });
            };
            _pipeline.Completed += () =>
            {
                this.InvokeOnUiThreadIfRequired(() =>
                {
                    lblStatus.Text = "任务状态：Completed";
                });
            };
            _pipeline.Canceled += () =>
            {
                this.InvokeOnUiThreadIfRequired(() =>
                {
                    lblStatus.Text = "任务状态：Canceled";
                });
            };
            _pipeline.Faulted += ex => _logger.LogError(ex, "Pipeline faulted");
        }

        private async Task StartRunnerAsync()
        {
            //string version = comboBox_KernelVersion.Text;
            //var chromeDir = Path.Combine(
            //    AppDomain.CurrentDomain.BaseDirectory,
            //    "File", "chrome-win", version, version);

            //if (!Directory.Exists(chromeDir))
            //{
            //    await DownloadBrowserAsync(version);
            //    if (!Directory.Exists(chromeDir))
            //    {
            //        _logger.LogWarning("Chrome kernel missing after download: {ChromeDir}", chromeDir);
            //        MessageBox.Show("浏览器内核缺失，请检查下载配置后重试。");
            //        return;
            //    }
            //}

            //if (_appSettings.UseLocalWord)
            //{
            //    var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", $"{_appSettings.WordName}.txt");
            //    if (!File.Exists(filePath))
            //    {
            //        await _adeHelper.DownloadWordFileByNameAsync(_appSettings.WordName);
            //    }
            //    if (!File.Exists(filePath))
            //    {
            //        _logger.LogWarning("缺少本地词库: {filePath}", filePath);
            //        return;
            //    }
            //}


            //await _aggregator.StartAsync();


            InitPipelineRunner();

            var runner = new UiTaskRunner(token => _pipeline!.RunAsync(token));

            ConfigureRunner(runner);

            _uiRunner = runner;
            _uiRunner.Start();


            _appAutoRestart?.Dispose();
            _appAutoRestart = null;
            var restartInterval = CommonHelper.GetRandomizedInterval(_appSettings.MainResetTimeout, 180);
            _appAutoRestart = new AppAutoRestart(
                restartInterval,
                () =>
                {
                    return _uiRunner != null && _uiRunner.State == RunnerState.Running;
                });

            _appAutoRestart.Start();
        }
        private async Task StopRunnerAsync()
        {
            try
            {
                _appAutoRestart?.Stop();

                if (_uiRunner != null)
                {
                    await _uiRunner.StopAsync();
                }
                await _aggregator.StopAsync();
            }
            finally
            {
                _appAutoRestart = null;
            }
        }
        private void ConfigureRunner(UiTaskRunner runner)
        {
            int clearTick = 0;

            runner.StateChanged += state =>
            {
                this.InvokeOnUiThreadIfRequired(() =>
                {
                    lblStatus.Text = $"任务状态：{state}";
                    btnStartStop.Text = state == RunnerState.Running ? "停止" : "开始";
                });
            };

            runner.Faulted += ex =>
            {
                _logger.LogError(ex, "UiTaskRunner faulted");
            };

            runner.LogEmitted += log =>
            {
                if (_appSettings.IsDetailLog)
                {
                    if (log.Exception == null)
                        _logger.LogInformation("[{Source}] {Message}", log.Source, log.Message);
                    else
                        _logger.LogWarning(log.Exception, "[{Source}] {Message}", log.Source, log.Message);
                }
            };

            // 1秒一次：UI统计刷新
            runner.SetPeriodicAction(
                interval: TimeSpan.FromSeconds(1),
                onTick: async token =>
                {
                    var elapsed = runner.RunElapsed;
                    //var totalStats = _aggregator.GetTotalStats();

                    this.InvokeOnUiThreadIfRequired(() =>
                    {
                        //label5.Text = $"提交数量:{totalStats.Request}";
                        //label6.Text = $"执行数量:{totalStats.Start}";
                        //label7.Text = $"曝光数量:{totalStats.DSP}";
                        //label8.Text = $"点击数量:{totalStats.Clickthrough}";
                        //label9.Text = $"成功数量:{totalStats.Success}";
                        //toolStripStatusLabel4.Text = $"执行总量：{QTPTotalStartCount + totalStats.Start}";
                        //toolStripStatusLabel5.Text = $"曝光总量：{QTPTotalDspCount + totalStats.DSP}";
                        //toolStripStatusLabel6.Text = $"点击总量：{QTPTotalClickthroughCount + totalStats.Clickthrough}";
                        //label12.Text = $"运行时长:{elapsed:hh\\:mm\\:ss}";
                    });

                    await Task.CompletedTask;
                },
                name: "RefreshStatsUi",
                skipIfRunning: true,
                timeout: TimeSpan.FromSeconds(2),
                circuitBreakThreshold: 10,
                circuitBreakCooldown: TimeSpan.FromSeconds(30)
            );
        }


        private async void btnStartStop_Click(object sender, EventArgs e)
        {
            if (!btnStartStop.Enabled)
                return;

            btnStartStop.Enabled = false;

            try
            {
                if (_uiRunner != null && _uiRunner.State is RunnerState.Running or RunnerState.Stopping)
                {
                    await StopRunnerAsync();
                }
                else
                {
                    await StartRunnerAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "btnStartStop_Click failed");
                MessageBox.Show($"启动/停止任务失败: {ex.Message}");
            }
            finally
            {
                this.InvokeOnUiThreadIfRequired(() =>
                {
                    btnStartStop.Enabled = true;
                });

            }
        }


    }

}
