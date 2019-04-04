namespace Valkyrie.Asgard.DevServer;

/// <summary>
///     AWSL SSR 渲染结果
/// </summary>
public sealed class AwslSsrRenderResult
{
    public string html { get; init; } = string.Empty;
    public string css { get; init; } = string.Empty;
    public string component_name { get; init; } = string.Empty;
    public string initial_state_json { get; init; } = "{}";
    public string scope { get; init; } = string.Empty;
    public List<string> head_tags { get; init; } = [];
    public List<string> script_tags { get; init; } = [];
    public bool is_suspense { get; init; }
    public string? fallback_html { get; init; }
}
