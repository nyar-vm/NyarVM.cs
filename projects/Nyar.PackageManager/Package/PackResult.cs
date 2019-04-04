namespace Nyar.PackageManager.Package;

/// <summary>
///     打包结果
/// </summary>
public class PackResult
{
    /// <summary>
    ///     tarball 字节数据
    /// </summary>
    public byte[] tarball_data { get; set; } = [];

    /// <summary>
    ///     SHA-256 完整性哈希
    /// </summary>
    public string sha256 { get; set; } = string.Empty;

    /// <summary>
    ///     tarball 大小（字节）
    /// </summary>
    public long size { get; set; }

    /// <summary>
    ///     包含的文件数量
    /// </summary>
    public int file_count { get; set; }
}