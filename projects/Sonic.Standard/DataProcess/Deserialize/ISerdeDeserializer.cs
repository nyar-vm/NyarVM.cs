using Std.DataProcess.Serialize;

namespace Std.DataProcess.Deserialize;

/// <summary>
///     Serde 反序列化器接口，所有反序列化格式的统一抽象
/// </summary>
public interface ISerdeDeserializer
{
    /// <summary>
    ///     将文本反序列化为 SerdeValue
    /// </summary>
    SerdeValue deserialize(string text);
}
