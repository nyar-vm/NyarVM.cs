namespace Std.Database.Core;

/// <summary>
///     LightDB 键值对条目
/// </summary>
internal readonly record struct DatabaseEntry(DatabaseKey key, DatabaseValue value)
{
    /// <summary>
    ///     空条目
    /// </summary>
    public static DatabaseEntry empty => new(DatabaseKey.empty, DatabaseValue.empty);

    /// <summary>
    ///     是否为空
    /// </summary>
    public bool is_empty => key.is_empty && value.is_empty;

    /// <summary>
    ///     反序列化值为指定类型
    /// </summary>
    /// <typeparam name="TValue">目标类型</typeparam>
    /// <returns>反序列化后的值</returns>
    public TValue? to_value<TValue>()
    {
        return value.to_object<TValue>();
    }
}