namespace Std.Data.Text.Scss;

/// <summary>
///     样式规则
/// </summary>
public sealed class StyleRule
{
    public StyleRule(IReadOnlyList<StyleSelector> selectors, IReadOnlyList<StyleDeclaration> declarations)
    {
        this.selectors = selectors;
        this.declarations = declarations;
        specificity = compute_specificity(selectors);
    }


    /// <summary>
    ///     选择器列表
    /// </summary>
    public IReadOnlyList<StyleSelector> selectors { get; }


    /// <summary>
    ///     声明列表
    /// </summary>
    public IReadOnlyList<StyleDeclaration> declarations { get; }


    /// <summary>
    ///     特异性权重
    /// </summary>
    public int specificity { get; }

    private static int compute_specificity(IReadOnlyList<StyleSelector> selectors)
    {
        var ids = 0;
        var classes = 0;
        var types = 0;

        foreach (var selector in selectors)
            switch (selector.type)
            {
                case StyleSelectorType.id:
                    ids++;
                    break;
                case StyleSelectorType.@class:
                case StyleSelectorType.pseudo_class:
                    classes++;
                    break;
                case StyleSelectorType.type:
                    types++;
                    break;
                case StyleSelectorType.parent_ref:
                    break;
            }

        return (ids << 16) | (classes << 8) | types;
    }
}