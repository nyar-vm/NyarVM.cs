namespace Valkyrie.Asgard.DevServer;

/// <summary>
///     HMR 消息格式
/// </summary>
internal sealed class HmrMessage
{
    public string type { get; set; } = string.Empty;
    public object? payload { get; set; }
}
