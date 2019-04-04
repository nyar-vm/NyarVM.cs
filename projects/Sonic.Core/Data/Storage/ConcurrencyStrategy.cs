namespace Core.Data.Storage;

/// <summary>
///     并发控制策略枚举，定义数据访问时的并发处理方式。
/// </summary>
public enum ConcurrencyStrategy
{
    /// <summary>
    ///     无并发控制。
    /// </summary>
    none,

    /// <summary>
    ///     乐观并发控制，通过版本号检测冲突。
    /// </summary>
    optimistic,

    /// <summary>
    ///     悲观并发控制，通过锁机制阻止冲突。
    /// </summary>
    pessimistic
}