namespace Std.Data.Text.Syntax;

/// <summary>
///     语法高亮片段类型
/// </summary>
public enum HighlightKind
{
    /// <summary>
    ///     其他
    /// </summary>
    other,

    /// <summary>
    ///     关键字
    /// </summary>
    keyword,

    /// <summary>
    ///     字符串
    /// </summary>
    @string,

    /// <summary>
    ///     数字
    /// </summary>
    number,

    /// <summary>
    ///     类型名
    /// </summary>
    type_name,

    /// <summary>
    ///     标识符
    /// </summary>
    identifier,

    /// <summary>
    ///     运算符
    /// </summary>
    @operator,

    /// <summary>
    ///     分隔符
    /// </summary>
    delimiter,

    /// <summary>
    ///     注释
    /// </summary>
    comment
}