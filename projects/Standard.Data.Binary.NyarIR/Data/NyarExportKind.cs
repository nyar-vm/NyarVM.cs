namespace Std.Data.Binary.NyarIR.Data;

/// <summary>
///     导出类型
/// </summary>
public enum NyarExportKind : byte
{
    /// <summary>
    ///     函数导出
    /// </summary>
    function = 0,

    /// <summary>
    ///     全局变量导出
    /// </summary>
    global = 1
}