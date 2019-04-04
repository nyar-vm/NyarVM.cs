namespace Valhalla.Client;

/// <summary>
///     下载结果
/// </summary>
public class ValhallaDownloadResult
{
    /// <summary>包名</summary>
    public string package_name { get; set; } = string.Empty;

    /// <summary>版本号</summary>
    public string version { get; set; } = string.Empty;

    /// <summary>.nyar 字节码数据</summary>
    public byte[] package_data { get; set; } = [];

    /// <summary>源码包数据</summary>
    public byte[]? source_data { get; set; }

    /// <summary>服务端声明的 .nyar SHA-256</summary>
    public string package_sha256 { get; set; } = string.Empty;

    /// <summary>服务端声明的源码 SHA-256</summary>
    public string? source_sha256 { get; set; }

    /// <summary>错误信息（下载失败时设置，如 404）</summary>
    public string? error { get; set; }
}