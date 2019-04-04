namespace Core.Security.Cryptography;

/// <summary>
///     密钥派生接口
/// </summary>
public interface IKeyDerivation
{
    /// <summary>
    ///     从密码和盐值派生密钥
    /// </summary>
    /// <param name="password">用户密码</param>
    /// <param name="salt">盐值</param>
    /// <returns>派生的密钥</returns>
    byte[] derive_key(string password, byte[] salt);
}