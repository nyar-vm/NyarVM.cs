namespace Std.Data.Binary.Pe.Data;

/// <summary>
///     PE 数据目录条目的
/// </summary>
public sealed class PeDirectoryDataEntry
{
    /// <summary>
    ///     数据的相对虚拟地址（RVA）的
    /// </summary>
    public uint rva { get; init; }

    /// <summary>
    ///     数据大小（字节）的
    /// </summary>
    public uint size { get; init; }

    /// <summary>
    ///     数据目录是否为空（RVA 的Size 均为 0）的
    /// </summary>
    public bool is_empty => rva == 0 && size == 0;
}