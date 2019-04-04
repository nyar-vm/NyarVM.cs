using Nyar.Language.Css;
using Std.Data.Text.Scss;

namespace Nyar.Language.WebStyle;

/// <summary>
///     Web Style 统一管线：CSS / SCSS / Tailwind 的统一编译、合并与输出门面。
///     下游只需提供样式来源，调用编译接口即可获得最终 CSS，无需自行实现拼接逻辑。
/// </summary>
public sealed class WebStylePipeline
{
    /// <summary>
    ///     Bundle header 文本，自动生成标识
    /// </summary>
    public string Header { get; init; } = "/* 自动生成 — 请勿手动编辑 */";

    /// <summary>
    ///     是否在每个样式来源前输出分隔注释
    /// </summary>
    public bool IncludeSourceComments { get; init; } = true;

    /// <summary>
    ///     是否包含 Tailwind Preflight 基础重置样式
    /// </summary>
    public bool IncludePreflight { get; init; }

    /// <summary>
    ///     编译 SCSS 源代码为 CSS 文本
    /// </summary>
    /// <param name="scssSource">SCSS 源代码</param>
    /// <returns>CSS 文本，解析失败或为空时返回空字符串</returns>
    public static string compile_scss(string scssSource)
    {
        if (string.IsNullOrWhiteSpace(scssSource))
        {
            return string.Empty;
        }

        try
        {
            var sheet = StyleSheet.parse(scssSource);
            return serialize_stylesheet(sheet);
        }
        catch
        {
            return string.Empty;
        }
    }

    /// <summary>
    ///     生成 Tailwind CSS（从类名列表或源码文本扫描）
    /// </summary>
    /// <param name="source">Tailwind 源码或类名文本</param>
    /// <returns>生成的 Tailwind CSS 文本</returns>
    /// <remarks>当前为占位实现，后续与 Nyar.Language.Tailwind 整合</remarks>
    public static string generate_tailwind(string source)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            return string.Empty;
        }

        // Tailwind 生成引擎尚未实现，暂时返回空
        return string.Empty;
    }

    /// <summary>
    ///     编译单个资产为 CSS 文本
    /// </summary>
    /// <param name="asset">样式资产</param>
    /// <returns>编译后的 CSS 文本</returns>
    public static string compile_asset(WebStyleAsset asset)
    {
        if (asset.IsEmpty)
        {
            return string.Empty;
        }

        return asset.Kind switch
        {
            WebStyleAssetKind.Css => asset.Content,
            WebStyleAssetKind.Scss => compile_scss(asset.Content),
            WebStyleAssetKind.Tailwind => generate_tailwind(asset.Content),
            _ => string.Empty
        };
    }

    /// <summary>
    ///     合并多个 WebStyleAsset 为一个统一的 CSS 字符串
    /// </summary>
    /// <param name="assets">样式资产列表</param>
    /// <param name="bundleName">Bundle 名称（用于生成注释头）</param>
    /// <returns>合并后的 CSS 文本</returns>
    public static string merge(IReadOnlyList<WebStyleAsset> assets, string bundleName = "")
    {
        var compiled = new List<(string Name, string Css)>();
        foreach (var asset in assets)
        {
            if (asset.IsEmpty)
            {
                continue;
            }

            var css = compile_asset(asset);
            if (!string.IsNullOrWhiteSpace(css))
            {
                compiled.Add((asset.Name, css));
            }
        }

        var sb = new StringBuilder();
        sb.AppendLine($"/* Legion 文档样式 — {bundleName} */");
        foreach (var (name, css) in compiled)
        {
            sb.AppendLine();
            sb.AppendLine($"/* {name} */");
            sb.Append(css);
            if (!css.EndsWith('\n'))
            {
                sb.AppendLine();
            }
        }

        return sb.ToString();
    }

    /// <summary>
    ///     编译并合并所有资产为统一 CSS bundle
    /// </summary>
    /// <param name="assets">样式资产列表</param>
    /// <param name="bundleName">Bundle 名称</param>
    /// <returns>合并后的 WebStyleBundle</returns>
    public static WebStyleBundle compile_all(IEnumerable<WebStyleAsset> assets, string bundleName = "")
    {
        var compiledEntries = new List<(string Name, string Css)>();

        foreach (var asset in assets)
        {
            if (asset.IsEmpty)
            {
                continue;
            }

            var css = compile_asset(asset);
            if (!string.IsNullOrWhiteSpace(css))
            {
                compiledEntries.Add((asset.Name, css));
            }
        }

        if (compiledEntries.Count == 0)
        {
            return new WebStyleBundle
            {
                Name = bundleName,
                Css = string.Empty,
                Sources = []
            };
        }

        var mergedCss = merge_sources(compiledEntries);
        return new WebStyleBundle
        {
            Name = bundleName,
            Css = mergedCss,
            Sources = compiledEntries.Select(e => e.Name).ToList()
        };
    }

    /// <summary>
    ///     合并多个 CSS 来源文本为单一 CSS 字符串
    /// </summary>
    /// <param name="sources">名称与 CSS 文本配对列表</param>
    /// <returns>合并后的 CSS 字符串</returns>
    private static string merge_sources(IReadOnlyList<(string Name, string Css)> sources)
    {
        var sb = new StringBuilder();
        sb.AppendLine("/* 自动生成 — 请勿手动编辑 */");

        foreach (var (name, css) in sources)
        {
            if (string.IsNullOrWhiteSpace(css))
            {
                continue;
            }

            sb.AppendLine();
            sb.AppendLine($"/* {name} */");
            sb.Append(css);

            if (!css.EndsWith('\n'))
            {
                sb.AppendLine();
            }
        }

        return sb.ToString();
    }

    #region SCSS 序列化

    /// <summary>
    ///     将 Std.Data.Text.Scss.StyleSheet 序列化为 CSS 文本
    /// </summary>
    private static string serialize_stylesheet(StyleSheet sheet)
    {
        if (sheet.rules.Count == 0)
        {
            return string.Empty;
        }

        var sb = new StringBuilder();
        foreach (var rule in sheet.rules)
        {
            serialize_rule(sb, rule);
        }

        return sb.ToString();
    }

    /// <summary>
    ///     序列化单个 StyleRule 为 CSS 文本
    /// </summary>
    private static void serialize_rule(StringBuilder sb, StyleRule rule)
    {
        var selector = serialize_selectors(rule.selectors);
        if (string.IsNullOrEmpty(selector))
        {
            return;
        }

        sb.Append(selector);
        sb.AppendLine(" {");

        foreach (var decl in rule.declarations)
        {
            serialize_declaration(sb, decl);
        }

        sb.AppendLine("}");
    }

    /// <summary>
    ///     将选择器列表序列化为 CSS 选择器字符串
    /// </summary>
    private static string serialize_selectors(IReadOnlyList<StyleSelector> selectors)
    {
        var sb = new StringBuilder();
        for (var i = 0; i < selectors.Count; i++)
        {
            if (i > 0)
            {
                sb.Append(' ');
            }

            var sel = selectors[i];
            sb.Append(sel.type switch
            {
                StyleSelectorType.id => $"#{sel.value}",
                StyleSelectorType.@class => $".{sel.value}",
                StyleSelectorType.type => sel.value,
                StyleSelectorType.pseudo_class => $":{sel.value}",
                StyleSelectorType.parent_ref => "&",
                _ => sel.value
            });
        }

        return sb.ToString();
    }

    /// <summary>
    ///     序列化单个 StyleDeclaration 为 CSS 声明
    /// </summary>
    private static void serialize_declaration(StringBuilder sb, StyleDeclaration decl)
    {
        var important = decl.important is not null ? $" {decl.important}" : string.Empty;
        sb.AppendLine($"  {decl.property}: {decl.value}{important};");
    }

    #endregion
}