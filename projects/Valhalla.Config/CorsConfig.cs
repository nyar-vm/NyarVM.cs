using Core.Data;

namespace Valhalla.Config;

/// <summary>
///     CORS 配置
/// </summary>
[Data]
public class CorsConfig
{
    /// <summary>允许的来源列表</summary>
    public List<string> origins { get; set; } = [];
}
