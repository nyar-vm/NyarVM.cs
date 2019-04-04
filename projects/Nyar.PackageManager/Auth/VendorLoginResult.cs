namespace Nyar.PackageManager.Auth;

/// <summary>
///     Vendor 登录结果
/// </summary>
public class VendorLoginResult
{
    /// <summary>
    ///     是否成功
    /// </summary>
    public bool success { get; set; }

    /// <summary>
    ///     Vendor 名称
    /// </summary>
    public string vendor_name { get; set; } = string.Empty;

    /// <summary>
    ///     用户名
    /// </summary>
    public string? username { get; set; }

    /// <summary>
    ///     令牌过期时间
    /// </summary>
    public DateTime? expires_at { get; set; }

    /// <summary>
    ///     凭据来源
    /// </summary>
    public string? credential_source { get; set; }

    /// <summary>
    ///     错误消息
    /// </summary>
    public string? error_message { get; set; }

    /// <summary>
    ///     创建成功结果
    /// </summary>
    public static VendorLoginResult ok(string vendorName, string username, DateTime? expiresAt)
    {
        return new VendorLoginResult
        {
            success = true,
            vendor_name = vendorName,
            username = username,
            expires_at = expiresAt
        };
    }

    /// <summary>
    ///     创建失败结果
    /// </summary>
    public static VendorLoginResult fail(string errorMessage)
    {
        return new VendorLoginResult
        {
            success = false,
            error_message = errorMessage
        };
    }
}