namespace Std.DataProcess.Serialize;

/// <summary>
///     Serde 序列化器接口，所有序列化格式的统一抽象
/// </summary>
public interface ISerdeSerializer
{
    /// <summary>
    ///     将 SerdeValue 序列化为文本
    /// </summary>
    string serialize(SerdeValue value);
}