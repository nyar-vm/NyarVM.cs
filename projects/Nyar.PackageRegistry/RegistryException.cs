namespace Nyar.PackageRegistry;

/// <summary>
///     注册表异常
/// </summary>
public class RegistryException : Exception
{
    /// <summary>
    ///     创建注册表异常
    /// </summary>
    public RegistryException(string message, int statusCode = 500, Exception? inner = null)
        : base(message, inner)
    {
        status_code = statusCode;
    }

    /// <summary>
    ///     HTTP 状态码
    /// </summary>
    public int status_code { get; }
}