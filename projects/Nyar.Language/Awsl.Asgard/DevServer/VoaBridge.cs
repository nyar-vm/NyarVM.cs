using System.Collections.Concurrent;
using System.Text.Json;

namespace Valkyrie.Asgard.DevServer;

/// <summary>
///     VOA 前后端通信桥，支持 WebSocket RPC 调用
/// </summary>
public sealed class VoaBridge
{
    private readonly ConcurrentDictionary<string, Func<JsonElement, Task<object?>>> _handlers = new();

    public VoaBridge()
    {
        register_default_handlers();
    }

    /// <summary>
    ///     注册 RPC 方法处理器
    /// </summary>
    public void register_handler(string method, Func<JsonElement, Task<object?>> handler)
    {
        _handlers[method] = handler;
    }

    /// <summary>
    ///     处理 RPC 调用
    /// </summary>
    public async Task<BridgeResult> handle_call(string requestBody)
    {
        BridgeRequest? request;

        try
        {
            request = JsonSerializer.Deserialize<BridgeRequest>(requestBody);
        }
        catch (JsonException ex)
        {
            return BridgeResult.fail(-32700, $"解析错误：{ex.Message}");
        }

        if (request is null || string.IsNullOrEmpty(request.method))
        {
            return BridgeResult.fail(-32600, "无效请求");
        }

        if (!_handlers.TryGetValue(request.method, out var handler))
        {
            return BridgeResult.fail(-32601, $"方法未找到：{request.method}");
        }

        try
        {
            var result = await handler(request.@params);
            return BridgeResult.success(request.id, result);
        }
        catch (Exception ex)
        {
            return BridgeResult.fail(-32603, $"内部错误：{ex.Message}", request.id);
        }
    }

    private void register_default_handlers()
    {
        register_handler("voa.ping", _ => Task.FromResult<object?>("pong"));
        register_handler("voa.getState", _ => Task.FromResult<object?>(new { status = "running" }));
        register_handler("voa.getConfig", _ => Task.FromResult<object?>(new { version = "0.0.1" }));

        register_handler("voa.navigate", async param =>
        {
            var path = param.TryGetProperty("path", out var p) ? p.GetString() ?? "/" : "/";
            return new { path, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() };
        });

        register_handler("voa.invoke", async param =>
        {
            var service = param.TryGetProperty("service", out var s) ? s.GetString() ?? "" : "";
            var method = param.TryGetProperty("method", out var m) ? m.GetString() ?? "" : "";
            var args = param.TryGetProperty("args", out var a) ? a : default;
            return new { service, method, result = "ok" };
        });
    }
}
