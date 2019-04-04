namespace Std.Data.Text.Regex;


/// <summary>

///     正则表达式 AST 基类


/// </summary>
public abstract record RegexAstNode;


/// <summary>

///     字面量字符


/// </summary>

/// <param name="Char">字面量字符值。</param>
public sealed record RegexAstLiteral(char Char) : RegexAstNode;


/// <summary>

///     连接序列


/// </summary>

/// <param name="Children">子节点列表。</param>
public sealed record RegexAstConcat(IReadOnlyList<RegexAstNode> Children) : RegexAstNode;


/// <summary>

///     选择（Alternation）


/// </summary>

/// <param name="Left">左侧子表达式。</param>

/// <param name="Right">右侧子表达式。</param>
public sealed record RegexAstAlt(RegexAstNode Left, RegexAstNode Right) : RegexAstNode;


/// <summary>

///     Kleene 星号


/// </summary>

/// <param name="Child">子表达式。</param>
public sealed record RegexAstStar(RegexAstNode Child) : RegexAstNode;


/// <summary>

///     Kleene 加号


/// </summary>

/// <param name="Child">子表达式。</param>
public sealed record RegexAstPlus(RegexAstNode Child) : RegexAstNode;


/// <summary>

///     可选（?）


/// </summary>

/// <param name="Child">子表达式。</param>
public sealed record RegexAstQuestion(RegexAstNode Child) : RegexAstNode;


/// <summary>

///     字符类


/// </summary>

/// <param name="Chars">字符类中的字符集合。</param>
public sealed record RegexAstCharClass(string Chars) : RegexAstNode;


/// <summary>

///     空串（ε）


/// </summary>
public sealed record RegexAstEmpty : RegexAstNode;
