namespace Core.Security.Cryptography;

/// <summary>
///     对称加密模式
/// </summary>
public enum CipherMode
{
    /// <summary>
    ///     密码分组链接模式
    /// </summary>
    cbc,

    /// <summary>
    ///     伽罗瓦/计数器模式
    /// </summary>
    gcm,

    /// <summary>
    ///     计数器模式
    /// </summary>
    ctr
}