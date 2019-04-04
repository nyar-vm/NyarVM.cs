namespace Std.Data.Text.Syntax;

/// <summary>
///     语法树工厂，从源文本创建语法树
/// </summary>
public sealed class SyntaxTreeFactory
{
    private readonly Language _language;
    private readonly Func<ISource, Language, GreenNode> _parse_green;

    /// <summary>
    ///     初始化语法树工厂
    /// </summary>
    /// <param name="language">语言配置。</param>
    /// <param name="parseGreen">解析器委托，从源文本产出绿树根节点。</param>
    public SyntaxTreeFactory(Language language, Func<ISource, Language, GreenNode> parseGreen)
    {
        _language = language;
        _parse_green = parseGreen;
    }

    /// <summary>
    ///     从源文本创建语法树
    /// </summary>
    /// <param name="source">源文本。</param>
    /// <returns>语法树。</returns>
    public SyntaxTree create_tree(ISource source)
    {
        var green = _parse_green(source, _language);
        return new SyntaxTree(source, green);
    }

    /// <summary>
    ///     从字符串创建语法树
    /// </summary>
    /// <param name="text">源代码文本。</param>
    /// <returns>语法树。</returns>
    public SyntaxTree create_tree(string text)
    {
        return create_tree(new StringSource(text));
    }
}