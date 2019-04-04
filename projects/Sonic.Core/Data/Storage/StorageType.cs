namespace Core.Data.Storage;

/// <summary>
///     存储字段类型枚举，定义支持的存储数据类型。
/// </summary>
public enum StorageType
{
    /// <summary>
    ///     32 位有符号整数。
    /// </summary>
    int32,

    /// <summary>
    ///     64 位有符号整数。
    /// </summary>
    int64,

    /// <summary>
    ///     字符串。
    /// </summary>
    @string,

    /// <summary>
    ///     64 位双精度浮点数。
    /// </summary>
    float64,

    /// <summary>
    ///     布尔值。
    /// </summary>
    boolean,

    /// <summary>
    ///     二进制大对象。
    /// </summary>
    blob,

    /// <summary>
    ///     日期时间。
    /// </summary>
    date_time,

    /// <summary>
    ///     全局唯一标识符。
    /// </summary>
    guid
}