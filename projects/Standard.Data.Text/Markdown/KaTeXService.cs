using System.Reflection;

namespace Std.Data.Text.Markdown;

/// <summary>
///     KaTeX 服务端渲染服务（纯回退模式，不依赖 Jint）
///     当 Jint 不可用时，LaTeX 公式将原样输出
/// </summary>
public sealed class KaTeXService
{
    private readonly object _lock = new();
    private string? _css_content;
    private bool _initialized;


    /// <summary>
    ///     将 LaTeX 公式渲染为 HTML 字符串（回退模式：原样输出 LaTeX 公式）
    /// </summary>
    /// <param name="latex">LaTeX 公式内容。</param>
    /// <param name="displayMode">是否为块级显示模式。</param>
    /// <returns>渲染后的 HTML 字符串。</returns>
    public string render_to_string(string latex, bool displayMode)
    {
        return displayMode ? $"$${escape_html(latex)}$$" : $"${escape_html(latex)}$";
    }


    /// <summary>
    ///     获取 KaTeX CSS 内容，用于在页面中内联或外链引用
    /// </summary>
    public string get_css_content()
    {
        if (_css_content != null) return _css_content;

        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = "Oak.Markdown.Resources.katex.min.css";

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream != null)
        {
            using var reader = new StreamReader(stream);
            _css_content = reader.ReadToEnd();
        }

        return _css_content ?? "";
    }


    /// <summary>
    ///     获取 KaTeX CSS 的 CDN 链接
    /// </summary>
    public static string get_css_cdn_url()
    {
        return "https://cdn.jsdelivr.net/npm/katex@0.16.11/dist/katex.min.css";
    }


    /// <summary>
    ///     获取 KaTeX CSS 的 link 标签
    /// </summary>
    public static string get_css_link_tag()
    {
        return $"<link rel=\"stylesheet\" href=\"{get_css_cdn_url()}\">";
    }

    private static string escape_html(string text)
    {
        return text
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;");
    }
}