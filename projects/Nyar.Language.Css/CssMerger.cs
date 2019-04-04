namespace Nyar.Language.Css;

/// <summary>
///     CSS 合并工具：将多个样式表合并为单一 CSS 字符串
/// </summary>
public static class CssMerger
{
    /// <summary>
    ///     合并多个 CSS 样式表为单一字符串，每个样式表以注释分隔
    /// </summary>
    /// <param name="stylesheets">样式表集合</param>
    /// <returns>合并后的 CSS 字符串</returns>
    public static string merge(IEnumerable<CssStylesheet> stylesheets)
    {
        var sb = new StringBuilder();
        sb.AppendLine("/* VOA 组件样式 — 自动生成 */");

        foreach (var stylesheet in stylesheets)
        {
            var cssText = stylesheet.ToStringCss();
            if (!string.IsNullOrWhiteSpace(cssText))
            {
                sb.AppendLine();
                sb.Append(cssText);
                if (!cssText.EndsWith('\n'))
                {
                    sb.AppendLine();
                }
            }
        }

        return sb.ToString();
    }

    /// <summary>
    ///     从编译结果列表合并 CSS（兼容 AwslCompileResult 的旧接口）
    /// </summary>
    /// <param name="cssBodies">CSS 字符串与组件名配对</param>
    /// <returns>合并后的 CSS 字符串</returns>
    public static string merge_component_css(
        IEnumerable<(string ComponentName, string CssBody)> cssBodies)
    {
        var sb = new StringBuilder();
        sb.AppendLine("/* VOA 组件样式 — 自动生成 */");

        foreach (var (name, css) in cssBodies)
        {
            if (!string.IsNullOrWhiteSpace(css))
            {
                sb.AppendLine();
                sb.AppendLine($"/* {name} */");
                sb.Append(css);
                if (!css.EndsWith('\n'))
                {
                    sb.AppendLine();
                }
            }
        }

        return sb.ToString();
    }
}