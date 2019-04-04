namespace Core.Security.Sandbox;

/// <summary>
///     AccessPermission 枚举
/// </summary>
public enum AccessPermission
{
    /// <summary>
    ///     无权限
    /// </summary>
    none,

    /// <summary>
    ///     读取
    /// </summary>
    read,

    /// <summary>
    ///     写入
    /// </summary>
    write,

    /// <summary>
    ///     执行
    /// </summary>
    execute,

    /// <summary>
    ///     完全控制
    /// </summary>
    full
}