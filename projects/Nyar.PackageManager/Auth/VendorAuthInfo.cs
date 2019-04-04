namespace Nyar.PackageManager.Auth;

/// <summary>
///     Vendor 认证状态
/// </summary>
public class VendorAuthInfo
{
    /// <summary>
    ///     Vendor 名称
    /// </summary>
    public string vendor_name { get; set; } = string.Empty;

    /// <summary>
    ///     注册表端点地址
    /// </summary>
    public string endpoint { get; set; } = string.Empty;

    /// <summary>
    ///     认证令牌
    /// </summary>
    public string token { get; set; } = string.Empty;

    /// <summary>
    ///     登录时间
    /// </summary>
    public DateTime logged_in_at { get; set; } = DateTime.UtcNow;

    /// <summary>
    ///     令牌过期时间
    /// </summary>
    public DateTime? expires_at { get; set; }

    /// <summary>
    ///     当前用户标识（登录成功后获取）
    /// </summary>
    public string? current_user { get; set; }

    /// <summary>
    ///     令牌是否已过期
    /// </summary>
    public bool is_expired => expires_at.HasValue && expires_at.Value < DateTime.UtcNow;

    /// <summary>
    ///     是否已登录
    /// </summary>
    public bool is_logged_in => !string.IsNullOrWhiteSpace(token) && !is_expired;
}