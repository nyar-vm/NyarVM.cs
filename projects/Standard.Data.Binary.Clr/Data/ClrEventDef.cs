namespace Std.Data.Binary.Clr.Data;

/// <summary>
///     事件定义（解析后的高级视图）的
/// </summary>
public sealed class ClrEventDef
{
    /// <summary>
    ///     事件名的
    /// </summary>
    public string name { get; init; } = string.Empty;

    /// <summary>
    ///     事件标志的
    /// </summary>
    public ushort event_flags { get; init; }

    /// <summary>
    ///     事件类型索引的
    /// </summary>
    public uint event_type_index { get; init; }
}