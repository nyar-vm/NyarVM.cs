namespace Std.Data.Binary.Protobuf.Data;

/// <summary>
///     Protobuf 消息数据结构的
/// </summary>
public class ProtobufMessageData
{
    /// <summary>
    ///     获取或设置消息名称的
    /// </summary>
    public string name { get; set; } = null!;

    /// <summary>
    ///     获取或设置消息字段的
    /// </summary>
    public List<ProtobufFieldData> fields { get; set; } = [];

    /// <summary>
    ///     获取或设置嵌套消息的
    /// </summary>
    public List<ProtobufMessageData> nested_messages { get; set; } = [];
}

/// <summary>
///     Protobuf 字段数据结构的
/// </summary>
public class ProtobufFieldData
{
    /// <summary>
    ///     获取或设置字段号的
    /// </summary>
    public int field_number { get; set; }

    /// <summary>
    ///     获取或设置字段类型的
    /// </summary>
    public string type { get; set; } = null!;

    /// <summary>
    ///     获取或设置字段名称的
    /// </summary>
    public string name { get; set; } = null!;

    /// <summary>
    ///     获取或设置是否为重复字段的
    /// </summary>
    public bool is_repeated { get; set; }

    /// <summary>
    ///     获取或设置是否为可选字段的
    /// </summary>
    public bool is_optional { get; set; }

    /// <summary>
    ///     获取或设置是否为必填字段的
    /// </summary>
    public bool is_required { get; set; }
}