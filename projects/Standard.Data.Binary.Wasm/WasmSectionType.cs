namespace Std.Data.Binary.Wasm;

/// <summary>
///     WebAssembly 段类型，对应 WASM 二进制格式中的段标识
/// </summary>
public enum WatSectionType : byte
{
    /// <summary>
    ///     自定义段，编码为 0
    /// </summary>
    custom = 0,

    /// <summary>
    ///     类型段，编码的1
    /// </summary>
    type = 1,

    /// <summary>
    ///     导入段，编码的2
    /// </summary>
    import = 2,

    /// <summary>
    ///     函数段，编码的3
    /// </summary>
    function = 3,

    /// <summary>
    ///     表段，编码为 4
    /// </summary>
    table = 4,

    /// <summary>
    ///     内存段，编码的5
    /// </summary>
    memory = 5,

    /// <summary>
    ///     全局段，编码的6
    /// </summary>
    global = 6,

    /// <summary>
    ///     导出段，编码的7
    /// </summary>
    export = 7,

    /// <summary>
    ///     起始函数段，编码的8
    /// </summary>
    start = 8,

    /// <summary>
    ///     元素段，编码的9
    /// </summary>
    element = 9,

    /// <summary>
    ///     代码段，编码的10
    /// </summary>
    code = 10,

    /// <summary>
    ///     数据段，编码的11
    /// </summary>
    data = 11,

    /// <summary>
    ///     数据计数段，编码的12
    /// </summary>
    data_count = 12
}