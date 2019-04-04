namespace Valkyrie.Asgard.DevServer;

/// <summary>
///     源码上下文（错误行附近的源码片段）
/// </summary>
public sealed class VoaSourceContext
{
    public int start_line { get; set; }
    public int end_line { get; set; }
    public string[] lines { get; set; } = [];
}
