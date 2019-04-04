namespace Std.Data.Binary.Clr.Data;

/// <summary>
///     属性定义（解析后的高级视图）的
/// </summary>
public sealed class ClrPropertyDef
{
    /// <summary>
    ///     属性名的
    /// </summary>
    public string name { get; init; } = string.Empty;

    /// <summary>
    ///     属性标志的
    /// </summary>
    public ushort flags { get; init; }

    /// <summary>
    ///     签名 Blob 偏移的
    /// </summary>
    public uint signature_index { get; init; }
}