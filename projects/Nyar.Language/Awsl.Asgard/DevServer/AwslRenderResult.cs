namespace Valkyrie.Asgard.DevServer;

/// <summary>
///     AWSL 渲染结果
/// </summary>
public sealed class AwslRenderResult
{
    public string html { get; init; } = string.Empty;
    public string css { get; init; } = string.Empty;
    public string java_script { get; init; } = string.Empty;
    public string component_name { get; init; } = string.Empty;
}
