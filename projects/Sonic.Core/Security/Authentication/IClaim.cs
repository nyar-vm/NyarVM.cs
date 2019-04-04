namespace Core.Security.Authentication;

/// <summary>
///     声明接口
/// </summary>
public interface IClaim
{
    /// <summary>
    ///     声明类型
    /// </summary>
    string type { get; }

    /// <summary>
    ///     声明值
    /// </summary>
    string value { get; }
}