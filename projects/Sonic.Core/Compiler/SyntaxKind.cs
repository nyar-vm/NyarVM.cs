namespace Core.Compiler;

/// <summary>
///     语法类别，标识语法树节点的类型
/// </summary>
public enum SyntaxKind
{
    /// <summary>
    ///     模块
    /// </summary>
    module,

    /// <summary>
    ///     函数
    /// </summary>
    function,

    /// <summary>
    ///     变量
    /// </summary>
    variable,

    /// <summary>
    ///     表达式
    /// </summary>
    expression,

    /// <summary>
    ///     语句
    /// </summary>
    statement,

    /// <summary>
    ///     类型
    /// </summary>
    type,

    /// <summary>
    ///     未知类型
    /// </summary>
    unknown
}