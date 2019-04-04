namespace Nyar.Types;

/// <summary>
///     导入类型
/// </summary>
public enum ImportKind
{
    /// <summary>
    ///     函数导入
    /// </summary>
    function,

    /// <summary>
    ///     全局变量导入
    /// </summary>
    global,

    /// <summary>
    ///     模块导入
    /// </summary>
    module
}