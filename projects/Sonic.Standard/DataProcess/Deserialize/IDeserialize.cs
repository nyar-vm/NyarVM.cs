namespace Std.DataProcess.Deserialize;

/// <summary>
///     类型反序列化器，将具体类�?<typeparamref name="T" /> 与反序列化器连接�?/// 在反序列化层操作，名称中使用 <c>Deserializer</c> 词根，不会与 <c>IRestorer</c>
///     混淆�?///
/// </summary>
/// <typeparam name="T">要反序列化的类型�?/typeparam>
public interface IDeserialize<out T>
{
    /// <summary>
    ///     从反序列化器中读取指定类型的值�?    ///
    /// </summary>
    /// <param name="deserializer">
    ///     源反序列化器�?/param>
    ///     <returns>反序列化得到的值�?/returns>
    T deserialize(IDeserializer deserializer);
}