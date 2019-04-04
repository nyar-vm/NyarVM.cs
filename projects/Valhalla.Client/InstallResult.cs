namespace Valhalla.Client;

/// <summary>
///     安装结果
/// </summary>
public class InstallResult
{
    /// <summary>是否安装成功</summary>
    public bool success { get; set; }

    /// <summary>安装的包名</summary>
    public string package_name { get; set; } = string.Empty;

    /// <summary>安装的版本号</summary>
    public string version { get; set; } = string.Empty;

    /// <summary>.nyar 的 SHA-256</summary>
    public string sha256 { get; set; } = string.Empty;

    /// <summary>源码的 SHA-256</summary>
    public string? source_sha256 { get; set; }

    /// <summary>错误消息</summary>
    public string? error { get; set; }

    /// <summary>完整性校验是否通过</summary>
    public bool integrity_verified { get; set; }

    /// <summary>发布者身份是否已验证</summary>
    public bool publisher_verified { get; set; }

    /// <summary>化身编号是否匹配</summary>
    public bool incarnation_matched { get; set; }

    /// <summary>创建成功的安装结果</summary>
    public static InstallResult succeed(string packageName, string version, string sha256,
        string? sourceSha256 = null)
    {
        return new InstallResult
        {
            success = true,
            package_name = packageName,
            version = version,
            sha256 = sha256,
            source_sha256 = sourceSha256
        };
    }

    /// <summary>创建失败的安装结果</summary>
    public static InstallResult fail(string packageName, string version, string error)
    {
        return new InstallResult
        {
            success = false,
            package_name = packageName,
            version = version,
            error = error
        };
    }
}