namespace Nyar.PackageRegistry;

/// <summary>
///     令牌验证结果
/// </summary>
public class TokenVerifyResult
{
    /// <summary>
    ///     是否有效
    /// </summary>
    public bool valid { get; set; }

    /// <summary>
    ///     用户名
    /// </summary>
    public string? username { get; set; }

    /// <summary>
    ///     令牌过期时间
    /// </summary>
    public DateTime? expires_at { get; set; }

    /// <summary>
    ///     错误消息
    /// </summary>
    public string? error_message { get; set; }

    /// <summary>
    ///     创建成功的验证结果
    /// </summary>
    public static TokenVerifyResult success(string username, DateTime? expiresAt = null)
    {
        return new TokenVerifyResult
        {
            valid = true,
            username = username,
            expires_at = expiresAt
        };
    }

    /// <summary>
    ///     创建失败的验证结果
    /// </summary>
    public static TokenVerifyResult failure(string errorMessage)
    {
        return new TokenVerifyResult
        {
            valid = false,
            error_message = errorMessage
        };
    }
}