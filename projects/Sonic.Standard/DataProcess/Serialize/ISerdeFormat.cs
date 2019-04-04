namespace Std.DataProcess.Serialize;

/// <summary>
///     Serde 格式序列化/反序列化器接口
/// </summary>
public interface ISerdeFormat : ISerdeSerializer, ISerdeDeserializer
{
    /// <summary>
    ///     格式名称
    /// </summary>
    string format_name { get; }
}