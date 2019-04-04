namespace Std.Data.Binary.Clr.Data;

/// <summary>
///     元数据流头的
/// </summary>
public sealed class ClrStreamHeader
{
    /// <summary>
    ///     流数据偏移量（相对于元数据根）的
    /// </summary>
    public uint offset { get; init; }

    /// <summary>
    ///     流数据大小（字节）的
    /// </summary>
    public uint size { get; init; }

    /// <summary>
    ///     流名称（的"#~"的#Strings"的#Blob"的#GUID"的#US"）的
    /// </summary>
    public string name { get; init; } = string.Empty;
}