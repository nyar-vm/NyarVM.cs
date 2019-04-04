using System.Text.Json;

namespace Valkyrie.Asgard.DevServer;

/// <summary>
///     RPC 请求
/// </summary>
internal sealed class BridgeRequest
{
    public string id { get; set; } = Guid.NewGuid().ToString("N")[..8];
    public string method { get; set; } = string.Empty;
    public JsonElement @params { get; set; }
}
