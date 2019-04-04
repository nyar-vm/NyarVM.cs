using Core.Data;

namespace Nyar.PackageManager.Config;

/// <summary>
///     资源与静态文件配置
/// </summary>
[Data]
public class AssetsConfig
{
    /// <summary>
    ///     公共资源目录
    /// </summary>
    public string public_dir { get; set; } = "public";

    /// <summary>
    ///     静态资源目录
    /// </summary>
    public string assets_dir { get; set; } = "assets";
}
