namespace Nyar.Dialect.Core;

/// <summary>
///     内存序枚举
/// </summary>
public enum MemoryOrder
{
    /// <summary>
    ///     无特定内存序要求
    /// </summary>
    none = 0,

    /// <summary>
    ///     宽松序
    /// </summary>
    relaxed = 1,

    /// <summary>
    ///     获取序
    /// </summary>
    acquire = 2,

    /// <summary>
    ///     释放序
    /// </summary>
    release = 3,

    /// <summary>
    ///     获取-释放序
    /// </summary>
    acq_rel = 4,

    /// <summary>
    ///     顺序一致序
    /// </summary>
    seq_cst = 5
}