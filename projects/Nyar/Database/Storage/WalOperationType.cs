namespace Nyar.Database.Storage;

/// <summary>
///     WAL 操作类型
/// </summary>
public enum WalOperationType : byte
{
    /// <summary>
    ///     插入或更新符号
    /// </summary>
    upsert_symbol = 1,

    /// <summary>
    ///     删除符号
    /// </summary>
    remove_symbol = 2,

    /// <summary>
    ///     插入或更新引用
    /// </summary>
    upsert_reference = 3,

    /// <summary>
    ///     删除引用
    /// </summary>
    remove_reference = 4,

    /// <summary>
    ///     插入或更新文件记录
    /// </summary>
    upsert_file = 5,

    /// <summary>
    ///     删除文件记录
    /// </summary>
    remove_file = 6,

    /// <summary>
    ///     检查点标记
    /// </summary>
    checkpoint = 7
}