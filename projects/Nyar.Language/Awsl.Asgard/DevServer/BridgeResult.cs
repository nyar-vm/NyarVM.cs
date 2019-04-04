namespace Valkyrie.Asgard.DevServer;

/// <summary>
///     RPC 响应
/// </summary>
public sealed class BridgeResult
{
    public string id { get; init; } = string.Empty;
    public object? result { get; init; }
    public BridgeErrorInfo? error_info { get; init; }

    public bool is_success => error_info is null;

    public static BridgeResult success(string id, object? result)
    {
        return new BridgeResult { id = id, result = result };
    }

    public static BridgeResult fail(int code, string message, string? id = null)
    {
        return new BridgeResult
        {
            id = id ?? string.Empty,
            error_info = new BridgeErrorInfo { code = code, message = message }
        };
    }
}
