namespace Std.Database.Core;

/// <summary>
///     存储引擎类型
/// </summary>
public enum StorageEngineType
{
    /// <summary>
    ///     B+ 树页面存储引擎（默认）
    /// </summary>
    b_tree,

    /// <summary>
    ///     LSM-Tree 存储引擎（写优化）
    /// </summary>
    lsm
}