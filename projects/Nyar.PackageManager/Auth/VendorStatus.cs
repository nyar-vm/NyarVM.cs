namespace Nyar.PackageManager.Auth;

/// <summary>
///     Vendor 状态信息
/// </summary>
public class VendorStatus
{
    /// <summary>
    ///     Vendor 名称
    /// </summary>
    public string name { get; set; } = string.Empty;

    /// <summary>
    ///     注册表端点地址
    /// </summary>
    public string endpoint { get; set; } = string.Empty;

    /// <summary>
    ///     是否已登录
    /// </summary>
    public bool is_logged_in { get; set; }

    /// <summary>
    ///     当前登录用户名
    /// </summary>
    public string? current_user { get; set; }

    /// <summary>
    ///     登录时间
    /// </summary>
    public DateTime? logged_in_at { get; set; }

    /// <summary>
    ///     令牌过期时间
    /// </summary>
    public DateTime? expires_at { get; set; }

    /// <summary>
    ///     是否有官方工具凭据可用
    /// </summary>
    public bool has_official_credentials { get; set; }

    /// <summary>
    ///     可用的官方工具凭据来源列表
    /// </summary>
    public List<string> available_credential_sources { get; set; } = [];
}