using Core.Data;

namespace Nyar.Language.Valkyrie.Config;

/// <summary>
///     路由渲染模式配置
/// </summary>
[Data]
public sealed class VoaRouteModeConfig
{
    public string path { get; set; } = "/";
    public string? mode { get; set; }
    public int? revalidate { get; set; }
}
