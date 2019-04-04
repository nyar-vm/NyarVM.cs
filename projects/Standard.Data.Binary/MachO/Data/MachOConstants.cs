namespace Std.Data.Binary.MachO.Data;

/// <summary>
///     Mach-O 格式常量的
/// </summary>
public static class MachOConstants
{
    /// <summary>
    ///     LC_SEGMENT 的32 位段加载命令的
    /// </summary>
    public const uint lc_segment = 0x01;

    /// <summary>
    ///     LC_SEGMENT_64 的64 位段加载命令的
    /// </summary>
    public const uint lc_segment64 = 0x19;
}