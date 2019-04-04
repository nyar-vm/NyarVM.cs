namespace Std.DataProcess.Serialize;

/// <summary>
///     提供 to_map 和 from_map 等数据序列化和反序列化入口函数
/// </summary>
public static class SerdeExtensions
{
    /// <summary>
    ///     将对象转换为 SerdeValue
    /// </summary>
    public static SerdeValue to_map<T>(this T value) where T : IDataSerialize<T>
    {
        return value.to_serde();
    }

    /// <summary>
    ///     将 SerdeValue 转换为对象
    /// </summary>
    public static T from_map<T>(this SerdeValue value) where T : IDataDeserialize<T>
    {
        return T.from_serde(value);
    }
}

/// <summary>
///     数据序列化接口，由 [data] 类型自动实现
/// </summary>
public interface IDataSerialize<out T>
{
    /// <summary>
    ///     将当前对象序列化为 SerdeValue
    /// </summary>
    SerdeValue to_serde();
}

/// <summary>
///     数据反序列化接口，由 [data] 类型自动实现
/// </summary>
public interface IDataDeserialize<T>
{
    /// <summary>
    ///     从 SerdeValue 反序列化为对象
    /// </summary>
    abstract static T from_serde(SerdeValue value);
}
