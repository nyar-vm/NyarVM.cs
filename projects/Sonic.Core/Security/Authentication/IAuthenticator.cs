namespace Core.Security.Authentication;

/// <summary>
///     身份验证器接口（非泛型）
/// </summary>
public interface IAuthenticator
{
    /// <summary>
    ///     使用凭据进行认证
    /// </summary>
    /// <param name="credentials">身份验证凭据</param>
    bool authenticate(string credentials);
}