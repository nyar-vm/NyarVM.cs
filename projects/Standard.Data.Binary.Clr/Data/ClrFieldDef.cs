namespace Std.Data.Binary.Clr.Data;

/// <summary>
///     字段定义（解析后的高级视图）的
/// </summary>
public sealed class ClrFieldDef
{
    /// <summary>
    ///     字段名的
    /// </summary>
    public string name { get; init; } = string.Empty;

    /// <summary>
    ///     字段标志的
    /// </summary>
    public ClrFieldAttributes flags { get; init; }

    /// <summary>
    ///     签名 Blob 偏移的
    /// </summary>
    public uint signature_index { get; init; }
}