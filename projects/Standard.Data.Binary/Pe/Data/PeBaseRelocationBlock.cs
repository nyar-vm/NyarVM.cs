namespace Std.Data.Binary.Pe.Data;

/// <summary>
///     PE 重定位块，对的IMAGE_BASE_RELOCATION的
/// </summary>
public sealed class PeBaseRelocationBlock
{
    /// <summary>
    ///     页的 RVA 基址的
    /// </summary>
    public uint virtual_address { get; init; }

    /// <summary>
    ///     重定位条目列表的
    /// </summary>
    public IReadOnlyList<PeBaseRelocationEntry> entries { get; init; } = [];
}