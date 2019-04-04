namespace Std.Data.Binary.Pe.Data;

/// <summary>
///     PE 重定位条目的
/// </summary>
public sealed class PeBaseRelocationEntry
{
    /// <summary>
    ///     重定位类型（的HIGHLOW=3, DIR64=10）的
    /// </summary>
    public byte type { get; init; }

    /// <summary>
    ///     页内偏移（低 12 位）的
    /// </summary>
    public ushort offset { get; init; }
}