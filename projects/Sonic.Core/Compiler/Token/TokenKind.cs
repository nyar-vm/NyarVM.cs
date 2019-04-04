namespace Core.Compiler.Token;

/// <summary>
///     词法单元类别，标识词法分析产生的词法单元类型
/// </summary>
public enum TokenKind
{
    /// <summary>
    ///     标识符
    /// </summary>
    identifier,

    /// <summary>
    ///     字面量
    /// </summary>
    literal,

    /// <summary>
    ///     关键字
    /// </summary>
    keyword,

    /// <summary>
    ///     运算符
    /// </summary>
    @operator,

    /// <summary>
    ///     标点符号
    /// </summary>
    punctuation,

    /// <summary>
    ///     空白字符
    /// </summary>
    whitespace,

    /// <summary>
    ///     注释
    /// </summary>
    comment,

    /// <summary>
    ///     未知类型
    /// </summary>
    unknown
}