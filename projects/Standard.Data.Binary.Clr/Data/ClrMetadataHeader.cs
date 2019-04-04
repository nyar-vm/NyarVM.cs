namespace Std.Data.Binary.Clr.Data;

/// <summary>
///     元数据头的
/// </summary>
public sealed class ClrMetadataHeader
{
    /// <summary>
    ///     签名（应的<see cref="ClrConstants.metadata_signature" /> = 0x424A5342 "BSJB"）的
    /// </summary>
    public uint signature { get; init; }

    /// <summary>
    ///     主版本号的
    /// </summary>
    public ushort major_version { get; init; }

    /// <summary>
    ///     次版本号的
    /// </summary>
    public ushort minor_version { get; init; }

    /// <summary>
    ///     保留字段的
    /// </summary>
    public uint reserved { get; init; }

    /// <summary>
    ///     版本字符串长度（包含尾部填充）的
    /// </summary>
    public uint version_string_length { get; init; }

    /// <summary>
    ///     版本字符串的
    /// </summary>
    public string version_string { get; init; } = string.Empty;

    /// <summary>
    ///     标志的
    /// </summary>
    public ushort flags { get; init; }

    /// <summary>
    ///     流数量的
    /// </summary>
    public ushort streams { get; init; }
}