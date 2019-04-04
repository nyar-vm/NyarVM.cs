using Core.Data;

namespace Nyar.PackageManager.Config;

/// <summary>
///     编译选项配置
/// </summary>
[Data]
public class BuildConfig
{
    /// <summary>
    ///     优化等级：debug / release
    /// </summary>
    public string optimize { get; set; } = "debug";

    /// <summary>
    ///     是否生成调试符号
    /// </summary>
    public bool debug_symbols { get; set; } = true;

    /// <summary>
    ///     输出目录
    /// </summary>
    public string output_dir { get; set; } = "dist";

    /// <summary>
    ///     是否生成 Source Map
    /// </summary>
    public bool sourcemap { get; set; } = true;
}
