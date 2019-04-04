namespace Nyar.PackageManager.Package;

/// <summary>
///     包元数据（写入 tarball 中的 package.json）
/// </summary>
internal class PackageMeta
{
    /// <summary>
    ///     打包时间
    /// </summary>
    public string packed_at { get; set; } = string.Empty;

    /// <summary>
    ///     打包器版本
    /// </summary>
    public string packer_version { get; set; } = string.Empty;

    /// <summary>
    ///     是否包含 legion.von 清单
    /// </summary>
    public bool has_manifest { get; set; }
}