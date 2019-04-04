namespace Core.Security.Authentication;

/// <summary>
///     身份验证方案
/// </summary>
public enum AuthScheme
{
    /// <summary>
    ///     Bearer Token 方案
    /// </summary>
    bearer,

    /// <summary>
    ///     Cookie 方案
    /// </summary>
    cookie,

    /// <summary>
    ///     API 密钥方案
    /// </summary>
    api_key,

    /// <summary>
    ///     基本认证方案
    /// </summary>
    basic
}