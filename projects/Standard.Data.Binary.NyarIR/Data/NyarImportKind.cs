namespace Std.Data.Binary.NyarIR.Data;

/// <summary>
///     导入类型
/// </summary>
public enum NyarImportKind : byte
{
    /// <summary>
    ///     函数导入
    /// </summary>
    function = 0,

    /// <summary>
    ///     全局变量导入
    /// </summary>
    global = 1,

    /// <summary>
    ///     模块导入
    /// </summary>
    module = 2
}