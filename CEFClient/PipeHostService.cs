using System.IO.Pipes;
using System.Text;
using System.Text.Json;

namespace CefClient;

public sealed class PipeHostService : IAsyncDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly string _pipeName;
    private readonly MainForm _mainForm;
    private readonly CancellationTokenSource _cts = new();

    private NamedPipeClientStream? _client;
    private StreamReader? _reader;
    private StreamWriter? _writer;
    private readonly SemaphoreSlim _writeLock = new(1, 1);

    private string? _taskId;
    private System.Text.Json.Nodes.JsonNode? _taskPayload;

    public PipeHostService(string pipeName, MainForm mainForm)
    {
        _pipeName = pipeName;
        _mainForm = mainForm;
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _client = new NamedPipeClientStream(".", _pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
        await _client.ConnectAsync(5000, cancellationToken);

        _reader = new StreamReader(_client, new UTF8Encoding(false), false, 4096, leaveOpen: true);
        _writer = new StreamWriter(_client, new UTF8Encoding(false), 4096, leaveOpen: true)
        {
            AutoFlush = true
        };

        await SendAsync(new PipeEnvelope
        {
            Type = "ready"
        }, cancellationToken);
    }

    public async Task RunLoopAsync(CancellationToken cancellationToken = default)
    {
        if (_reader == null)
            throw new InvalidOperationException("未启动");

        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, cancellationToken);
        var token = linkedCts.Token;

        while (!token.IsCancellationRequested)
        {
            var line = await _reader.ReadLineAsync();
            if (line == null)
                break;

            PipeEnvelope? req;
            try
            {
                req = JsonSerializer.Deserialize<PipeEnvelope>(line, JsonOptions);
            }
            catch
            {
                continue;
            }

            if (req == null)
                continue;

            switch (req.Type)
            {
                case "start":
                    _taskId = req.TaskId;
                    _taskPayload = req.Payload;
                    await SendAsync(new PipeEnvelope
                    {
                        Type = "started",
                        TaskId = _taskId,
                        Success = true
                    }, token);
                    break;

                case "createBrowser":
                    await HandleCreateBrowserAsync(req, token);
                    break;

                case "runBrowser":
                    await HandleRunBrowserAsync(req, token);
                    break;

                case "removeBrowser":
                    await HandleRemoveBrowserAsync(req, token);
                    break;

                case "exit":
                    await _mainForm.RemoveAllBrowsersAsync();
                    return;
            }
        }
    }

    private async Task HandleCreateBrowserAsync(PipeEnvelope req, CancellationToken token)
    {
        var ok = await _mainForm.CreateBrowserAsync(req.BrowserId!, token);

        await SendAsync(new PipeEnvelope
        {
            Type = "browserCreated",
            TaskId = _taskId,
            BrowserId = req.BrowserId,
            Success = ok,
            Message = ok ? "created" : "create failed"
        }, token);
    }

    private async Task HandleRunBrowserAsync(PipeEnvelope req, CancellationToken token)
    {
        var payload = req.Payload ?? _taskPayload;
        var result = await _mainForm.RunBrowserAsync(req.BrowserId!, payload, token);

        await SendAsync(new PipeEnvelope
        {
            Type = "browserResult",
            TaskId = _taskId,
            BrowserId = req.BrowserId,
            Success = result.Success,
            Message = result.Message,
            Data = result.Data
        }, token);
    }

    private async Task HandleRemoveBrowserAsync(PipeEnvelope req, CancellationToken token)
    {
        try
        {
            await _mainForm.RemoveBrowserFastAsync(req.BrowserId!);

            await SendAsync(new PipeEnvelope
            {
                Type = "browserRemoved",
                TaskId = _taskId,
                BrowserId = req.BrowserId,
                Success = true,
                Message = "removed"
            }, token);
        }
        catch (Exception ex)
        {
            await SendAsync(new PipeEnvelope
            {
                Type = "browserRemoved",
                TaskId = _taskId,
                BrowserId = req.BrowserId,
                Success = false,
                Message = ex.Message
            }, token);
        }
    }

    private async Task SendAsync(PipeEnvelope envelope, CancellationToken cancellationToken = default)
    {
        if (_writer == null)
            throw new InvalidOperationException("未启动");

        var json = JsonSerializer.Serialize(envelope, JsonOptions);

        await _writeLock.WaitAsync(cancellationToken);
        try
        {
            await _writer.WriteLineAsync(json);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        try { _cts.Cancel(); } catch { }

        _reader?.Dispose();
        _writer?.Dispose();
        _client?.Dispose();
        _writeLock.Dispose();
        _cts.Dispose();

        await Task.CompletedTask;
    }
}