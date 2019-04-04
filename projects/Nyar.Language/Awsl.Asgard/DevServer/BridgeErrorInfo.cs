namespace Valkyrie.Asgard.DevServer;

/// <summary>
///     RPC 错误
/// </summary>
public sealed class BridgeErrorInfo
{
    public int code { get; init; }
    public string message { get; init; } = string.Empty;
}
