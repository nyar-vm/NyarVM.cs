namespace Std.Data.Text.Awsl.Lexer;

/// <summary>
///     AWSL 词法节点类型定义
/// </summary>
public static class AwslNodeKind
{
    public static readonly NodeKind unknown = 0;

    /// <summary>
    ///     关键字（let, const, micro, if, else, for 等）
    /// </summary>
    public static readonly NodeKind keyword = 1;

    /// <summary>
    ///     标识符（变量名、标签名、函数名等）
    /// </summary>
    public static readonly NodeKind identifier = 2;

    /// <summary>
    ///     数字字面量
    /// </summary>
    public static readonly NodeKind number = 3;

    /// <summary>
    ///     字符串字面量
    /// </summary>
    public static readonly NodeKind @string = 4;

    /// <summary>
    ///     布尔/null 字面量（true, false, null）
    /// </summary>
    public static readonly NodeKind literal = 5;

    /// <summary>
    ///     运算符（+, -, *, /, ==, !=, =>, -> 等）
    /// </summary>
    public static readonly NodeKind @operator = 6;

    /// <summary>
    ///     标点符号（ :, ::, ;, ., , 等）
    /// </summary>
    public static readonly NodeKind punctuation = 7;

    /// <summary>
    ///     分隔符（(, ), [, ], {, } 等）
    /// </summary>
    public static readonly NodeKind delimiter = 8;

    /// <summary>
    ///     注释
    /// </summary>
    public static readonly NodeKind comment = 9;

    /// <summary>
    ///     事件/绑定前缀（@click, @bind 等）
    /// </summary>
    public static readonly NodeKind at_prefix = 10;

    /// <summary>
    ///     文件结束
    /// </summary>
    public static readonly NodeKind eof = 11;
}