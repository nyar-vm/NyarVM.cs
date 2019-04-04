namespace Std.Data.Text.Syntax;

/// <summary>
///     语言顶层结构的基类///
/// </summary>
public abstract class SyntaxRoot : SyntaxNode
{
    /// <summary>
    ///     初始化语法根节点
    /// </summary>
    protected SyntaxRoot(GreenNode green, SyntaxTree tree, int offset, string languageId)
        : base(green, tree, offset)
    {
        language_id = languageId;
    }

    /// <summary>
    ///     语言标识符
    public string language_id { get; }

    /// <summary>
    ///     与该语法根绑定的语言对象，供分析器直接面向节点语言分派。
    public Language? language => LanguageRegistry.get_language(language_id);
}