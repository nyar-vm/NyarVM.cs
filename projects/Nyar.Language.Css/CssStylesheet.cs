namespace Nyar.Language.Css;

/// <summary>
///     CSS 样式表：聚合一组 CSS 规则
/// </summary>
public sealed class CssStylesheet
{
    /// <summary>
    ///     规则列表
    /// </summary>
    public List<CssRule> Rules { get; init; } = [];

    /// <summary>
    ///     样式表是否为空
    /// </summary>
    public bool IsEmpty => Rules.Count == 0;

    /// <summary>
    ///     将样式表序列化为 CSS 文本
    /// </summary>
    public string ToStringCss()
    {
        if (Rules.Count == 0)
        {
            return string.Empty;
        }

        var sb = new StringBuilder();
        foreach (var rule in Rules)
        {
            sb.Append(rule.Selector);
            sb.Append(" { ");
            foreach (var decl in rule.Declarations)
            {
                sb.Append(decl.Property);
                sb.Append(": ");
                sb.Append(decl.Value);
                sb.Append("; ");
            }
            sb.AppendLine("}");
        }

        return sb.ToString();
    }
}