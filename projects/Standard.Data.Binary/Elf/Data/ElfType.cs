namespace Std.Data.Binary.Elf.Data;

/// <summary>
///     ELF 文件类型的
/// </summary>
public enum ElfType : ushort
{
    /// <summary>
    ///     未指定文件类型的
    /// </summary>
    none = 0,

    /// <summary>
    ///     可重定位文件的
    /// </summary>
    relocatable = 1,

    /// <summary>
    ///     可执行文件的
    /// </summary>
    executable = 2,

    /// <summary>
    ///     共享目标文件的
    /// </summary>
    shared_object = 3,

    /// <summary>
    ///     核心转储文件的
    /// </summary>
    core = 4
}