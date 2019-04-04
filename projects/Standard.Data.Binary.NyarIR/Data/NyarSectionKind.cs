namespace Std.Data.Binary.NyarIR.Data;

/// <summary>
///     Nyar 段类型
/// </summary>
public enum NyarSectionKind : byte
{
    /// <summary>
    ///     常量池段
    /// </summary>
    constants = 0x01,

    /// <summary>
    ///     函数表段
    /// </summary>
    functions = 0x02,

    /// <summary>
    ///     代码段
    /// </summary>
    code = 0x03,

    /// <summary>
    ///     导入表段
    /// </summary>
    imports = 0x04,

    /// <summary>
    ///     导出表段
    /// </summary>
    exports = 0x05,

    /// <summary>
    ///     Witness 分派绑定段
    /// </summary>
    witness_entries = 0x06,

    /// <summary>
    ///     调试信息段
    /// </summary>
    debug_info = 0x10,

    /// <summary>
    ///     源码映射段
    /// </summary>
    source_map = 0x11
}