using Core.Data;
using Nyar.Types.Targets;
using Std.Config;

namespace Nyar.Language.Valkyrie.Config;

 [Data]
public sealed class VoaBuildConfig
{
    public string? app_name { get; set; }
    public string output { get; set; } = "dist";
    public string? entry { get; set; }
    public string? html_template { get; set; }
    public string runtime { get; set; } = "voa-runtime.js";
    public bool minify { get; set; } = true;
    public bool sourcemap { get; set; } = true;
    public bool generate_wat { get; set; } = false;

    /// <summary>
    ///     编译目标模式，默认从 TargetProfile 继承。
    ///     <c>"dev"</c> = 最大粒度 per-file 输出 + HMR 支持；
    ///     <c>"prod"</c> = 合并优化输出。
    /// </summary>
    [Key("mode")]
    public TargetMode? target_mode { get; set; }

    public VoaChunkConfig chunk { get; set; } = new();
}
