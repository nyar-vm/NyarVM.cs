namespace Std.Data.Text.Scss;

/// <summary>
///     样式表
/// </summary>
public sealed class StyleSheet
{
    private readonly List<StyleRule> _rules = [];

    public StyleSheet(string? source = null)
    {
        this.source = source;
    }


    /// <summary>
    ///     所有样式规则
    /// </summary>
    public IReadOnlyList<StyleRule> rules => _rules;


    /// <summary>
    ///     原始源代码
    /// </summary>
    public string? source { get; }


    /// <summary>
    ///     添加样式规则
    /// </summary>
    public void add_rule(StyleRule rule)
    {
        _rules.Add(rule);
    }


    /// <summary>
    ///     添加样式规则
    /// </summary>
    public void add_rule(IReadOnlyList<StyleSelector> selectors, IReadOnlyList<StyleDeclaration> declarations)
    {
        _rules.Add(new StyleRule(selectors, declarations));
    }


    /// <summary>
    ///     清空所有规则
    /// </summary>
    public void clear()
    {
        _rules.Clear();
    }


    /// <summary>
    ///     解析 SCSS 源代码
    /// </summary>
    public static StyleSheet parse(string source)
    {
        var sheet = new StyleSheet(source);
        var parser = new ScssParser(source);
        parser.parse(sheet);
        return sheet;
    }


    /// <summary>
    ///     解析 SCSS 源代码（别名）
    /// </summary>
    public static StyleSheet parse_scss(string source)
    {
        return parse(source);
    }
}