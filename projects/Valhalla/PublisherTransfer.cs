namespace Valhalla;

/// <summary>
///     发布者转移事件
/// </summary>
public class PublisherTransfer
{
    /// <summary>包名</summary>
    public string package { get; set; } = string.Empty;

    /// <summary>新的发布者公钥指纹</summary>
    public string new_publisher { get; set; } = string.Empty;

    /// <summary>当前的 incarnation</summary>
    public int incarnation { get; set; } = 1;

    /// <summary>签发时间</summary>
    public DateTime issued_at { get; set; } = DateTime.UtcNow;

    /// <summary>原发布者公钥指纹</summary>
    public string old_publisher { get; set; } = string.Empty;

    /// <summary>原发布者 Ed25519 签名（Base64）</summary>
    public string signature { get; set; } = string.Empty;
}