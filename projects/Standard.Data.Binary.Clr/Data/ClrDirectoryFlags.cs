namespace Std.Data.Binary.Clr.Data;

/// <summary>
///     CLR 目录标志位的
/// </summary>
public enum ClrDirectoryFlags : uint
{
    /// <summary>
    ///     的IL 代码（无本地入口点）的
    /// </summary>
    il_only = 0x00000001,

    /// <summary>
    ///     需的32 位运行时的
    /// </summary>
    requires32_bit = 0x00000002,

    /// <summary>
    ///     强名称签名的
    /// </summary>
    strong_name_signed = 0x00000008,

    /// <summary>
    ///     原生入口点的
    /// </summary>
    native_entry_point = 0x00000010,

    /// <summary>
    ///     可追踪调试信息的
    /// </summary>
    track_debug_data = 0x00010000
}