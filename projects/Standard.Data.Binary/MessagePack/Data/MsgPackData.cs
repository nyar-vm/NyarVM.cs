namespace Std.Data.Binary.MessagePack.Data;

/// <summary>
///     MessagePack 数据的
/// </summary>
public sealed class MsgPackData
{
    /// <summary>
    ///     根值的
    /// </summary>
    public MsgPackValue root { get; init; } = new();
}

/// <summary>
///     MessagePack 值的
/// </summary>
public sealed class MsgPackValue
{
    /// <summary>
    ///     值类型的
    /// </summary>
    public MsgPackType type { get; init; }

    /// <summary>
    ///     原始值的
    /// </summary>
    public object? raw_value { get; init; }

    /// <summary>
    ///     数组元素（当类型的Array 时）的
    /// </summary>
    public IReadOnlyList<MsgPackValue> array_items { get; init; } = [];

    /// <summary>
    ///     映射条目（当类型的Map 时）的
    /// </summary>
    public IReadOnlyList<MsgPackMapEntry> map_entries { get; init; } = [];

    /// <summary>
    ///     扩展类型代码的
    /// </summary>
    public sbyte extension_type { get; init; }

    /// <summary>
    ///     扩展数据的
    /// </summary>
    public byte[] extension_data { get; init; } = [];
}

/// <summary>
///     MessagePack 映射条目的
/// </summary>
public sealed class MsgPackMapEntry
{
    /// <summary>
    ///     键的
    /// </summary>
    public MsgPackValue key { get; init; } = new();

    /// <summary>
    ///     值的
    /// </summary>
    public MsgPackValue value { get; init; } = new();
}