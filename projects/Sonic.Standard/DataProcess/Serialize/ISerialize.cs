namespace Std.DataProcess.Serialize;

/// <summary>
///     类型序列化器，将具体类型 <typeparamref name="T" /> 与序列化器连接�?/// 在序列化层操作，名称中使�?<c>Serializer</c> 词根，不会与 <c>IProjector</c>
///     混淆�?///
/// </summary>
/// <typeparam name="T">要序列化的类型�?/typeparam>
public interface ISerialize<in T>
{
    /// <summary>
    ///     将指定类型的值序列化到序列化器�?    ///
    /// </summary>
    /// <param name="value">
    ///     要序列化的值�?/param>
    ///     <param name="serializer">目标序列化器�?/param>
    void serialize(T value, ISerializer serializer);
}