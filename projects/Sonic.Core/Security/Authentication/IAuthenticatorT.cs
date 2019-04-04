using System.Threading.Tasks;

namespace Core.Security.Authentication;

/// <summary>
///     身份验证器接口（泛型）
/// </summary>
/// <typeparam name="T">身份主体类型</typeparam>
public interface IAuthenticatorT<T>
{
    /// <summary>
    ///     异步验证凭据并返回身份主体
    /// </summary>
    /// <param name="credentials">身份验证凭据</param>
    /// <returns>身份主体，验证失败时返回 null</returns>
    Task<T?> authenticate(string credentials);
}