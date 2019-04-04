namespace Core.Security.Sandbox;

/// <summary>
///     ResourceKind 枚举
/// </summary>
public enum ResourceKind
{
    /// <summary>
    ///     无
    /// </summary>
    none,

    /// <summary>
    ///     文件系统
    /// </summary>
    file_system,

    /// <summary>
    ///     网络
    /// </summary>
    network,

    /// <summary>
    ///     内存
    /// </summary>
    memory,

    /// <summary>
    ///     环境变量
    /// </summary>
    environment,

    /// <summary>
    ///     注册表
    /// </summary>
    registry
}