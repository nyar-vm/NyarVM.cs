using Core.Data;
using Std.Config;

namespace Nyar.Language.Valkyrie.Config;

/// <summary>
///     VOA 项目配置模型，对应 voa.config.v 中的 VON 配置对象
/// </summary>
[Data]
public sealed class VoaProjectConfig
{
    public string? name { get; set; }
    public string? version { get; set; }
    public string project_type { get; set; } = "frontend";
    public string target { get; set; } = "wasm";
    public string? entry { get; set; }
    public string? html_template { get; set; }
    public string? runtime { get; set; }
    public VoaServerConfig? server { get; set; }
    [Flatten]
    public VoaBuildConfig build { get; set; } = new();
    public VoaHotReloadConfig hot_reload { get; set; } = new();
    [Merge(MergeMode.replace)]
    public List<VoaRouteModeConfig>? routes { get; set; }
    public string? default_render_mode { get; set; }

    public bool is_frontend => project_type is "frontend" or "application";
    public bool is_backend => project_type == "backend";
    public bool is_library => project_type == "library";

    /// <summary>
    ///     获取指定路径的渲染模式，若无显式配置则返回默认值
    /// </summary>
    public string get_render_mode(string path)
    {
        if (routes == null || routes.Count == 0) return default_render_mode ?? "csr";

        foreach (var route in routes)
            if (match_route_path(route.path, path))
                return route.mode ?? default_render_mode ?? "csr";

        return default_render_mode ?? "csr";
    }

    private static bool match_route_path(string pattern, string actual)
    {
        if (pattern == actual) return true;

        if (pattern.EndsWith("/*"))
        {
            var prefix = pattern[..^2];
            return actual.StartsWith(prefix);
        }

        return false;
    }
}
