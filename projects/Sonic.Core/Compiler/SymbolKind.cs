namespace Core.Compiler;

/// <summary>
///     符号类别，表示符号在程序中的角色
/// </summary>
public enum SymbolKind
{
    /// <summary>
    ///     类型符号
    /// </summary>
    type,

    /// <summary>
    ///     方法符号
    /// </summary>
    method,

    /// <summary>
    ///     字段符号
    /// </summary>
    field,

    /// <summary>
    ///     变量符号
    /// </summary>
    variable,

    /// <summary>
    ///     命名空间符号
    /// </summary>
    @namespace
}