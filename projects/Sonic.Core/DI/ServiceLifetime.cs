namespace Core.DI;

/// <summary>
///     服务生命周期类型
/// </summary>
public enum ServiceLifetime
{
    /// <summary>
    ///     单例生命周期，整个应用共享一个实例
    /// </summary>
    singleton,

    /// <summary>
    ///     作用域生命周期，每个作用域内共享一个实例
    /// </summary>
    scoped,

    /// <summary>
    ///     瞬态生命周期，每次请求创建一个新实例
    /// </summary>
    transient
}