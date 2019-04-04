namespace Nyar.Language.Css;

/// <summary>
///     CSS 作用域工具：为选择器添加组件作用域前缀
/// </summary>
public static class CssScoper
{
    /// <summary>
    ///     为 CssStylesheet 添加作用域前缀，生成 Scoped CSS 字符串
    /// </summary>
    /// <param name="stylesheet">样式表</param>
    /// <param name="componentName">组件名称</param>
    /// <param name="scopePrefix">作用域前缀（如 "voa-" 或 "voa-ssr-"）</param>
    /// <returns>Scoped CSS 字符串，无样式时返回空字符串</returns>
    public static string scope_css_stylesheet(CssStylesheet stylesheet, string componentName, string scopePrefix)
    {
        if (stylesheet.IsEmpty)
        {
            return string.Empty;
        }

        var sb = new StringBuilder();
        var scope = $"{scopePrefix}{CssStringUtils.to_kebab_case(componentName)}";

        foreach (var rule in stylesheet.Rules)
        {
            sb.Append($".{scope} {rule.Selector} {{ ");
            foreach (var decl in rule.Declarations)
            {
                sb.Append($"{decl.Property}: {decl.Value}; ");
            }
            sb.AppendLine("}");
        }

        return sb.ToString();
    }

    /// <summary>
    ///     为原始样式字典生成 Scoped CSS（兼容旧接口）
    /// </summary>
    /// <param name="styles">类名到 CSS 属性字符串的映射</param>
    /// <param name="componentName">组件名称</param>
    /// <param name="scopePrefix">作用域前缀</param>
    /// <returns>Scoped CSS 字符串</returns>
    public static string render_scoped_styles(
        IReadOnlyDictionary<string, string> styles,
        string componentName,
        string scopePrefix)
    {
        if (styles.Count == 0)
        {
            return string.Empty;
        }

        var sb = new StringBuilder();
        var scope = $"{scopePrefix}{CssStringUtils.to_kebab_case(componentName)}";

        foreach (var kvp in styles)
        {
            var className = kvp.Key;
            var properties = kvp.Value;
            sb.AppendLine($".{scope} .{className} {{ {properties} }}");
        }

        return sb.ToString();
    }
}