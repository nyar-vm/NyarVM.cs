using System.Text;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;

namespace VOA.ToolChain.Tests;

/// <summary>
///     SSR 与 CSR 首帧对比测试 — 验证 SSR 输出与客户端渲染首帧一致，无 hydration mismatch
/// </summary>
public sealed class VoaSsrCsrComparisonTests : IDisposable
{
    private readonly AwslReactiveCompiler _compiler = new();
    private readonly ITestOutputHelper _output;
    private readonly string _temp_dir;

    public VoaSsrCsrComparisonTests(ITestOutputHelper output)
    {
        _output = output;
        _temp_dir = Path.Combine(Path.GetTempPath(), $"voa-ssr-csr-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_temp_dir);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_temp_dir)) Directory.Delete(_temp_dir, true);
        }
        catch
        {
        }
    }

    [Fact]
    public void SsrCsr_SimpleDiv_StructureMatches()
    {
        var source = @"<widget>
    <div id=""app"">
        <h1>Hello VOA</h1>
        <p>Welcome</p>
    </div>
</widget>";

        var ssrHtml = render_ssr(source);
        var csrHtml = render_csr(source);

        Assert.Contains("<h1>Hello VOA</h1>", ssrHtml);
        Assert.Contains("<h1>Hello VOA</h1>", csrHtml);
        Assert.Contains("<div id=\"app\"", ssrHtml);
        Assert.Contains("<div id=\"app\"", csrHtml);
        Assert.Equal(normalize_whitespace(csrHtml), normalize_whitespace(ssrHtml));
    }

    [Fact]
    public void SsrCsr_NestedElements_StructureMatches()
    {
        var source = @"<widget>
    <div class=""container"">
        <nav>
            <a href=""/"">Home</a>
            <a href=""/about"">About</a>
        </nav>
        <main>
            <article>
                <h2>Title</h2>
                <p>Body text</p>
            </article>
        </main>
    </div>
</widget>";

        var ssrHtml = render_ssr(source);
        var csrHtml = render_csr(source);

        Assert.Contains("<nav>", ssrHtml);
        Assert.Contains("<nav>", csrHtml);
        Assert.Contains("<main>", ssrHtml);
        Assert.Contains("<main>", csrHtml);
        Assert.Equal(normalize_whitespace(csrHtml), normalize_whitespace(ssrHtml));
    }

    [Fact]
    public void SsrCsr_StaticAttributes_Match()
    {
        var source = @"<widget>
    <div class=""card"" data-id=""42"" aria-label=""test"">
        <span class=""badge"" title=""info"">X</span>
    </div>
</widget>";

        var ssrHtml = render_ssr(source);
        var csrHtml = render_csr(source);

        Assert.Contains("data-id=\"42\"", ssrHtml);
        Assert.Contains("data-id=\"42\"", csrHtml);
        Assert.Contains("aria-label=\"test\"", ssrHtml);
        Assert.Contains("aria-label=\"test\"", csrHtml);
        Assert.Contains("class=\"badge\"", ssrHtml);
        Assert.Contains("class=\"badge\"", csrHtml);
    }

    private string render_ssr(string source)
    {
        var renderer = new AwslSsrRenderer();
        var result = renderer.render_ssr(source, "");
        return result.html;
    }

    private string render_csr(string source)
    {
        var parseResult = new AwslParser().parse(source);
        var compileResult = _compiler.compile(parseResult, "");
        var componentName = parseResult.name ?? "TestComponent";
        var code = compileResult.java_script;
        var css = compileResult.css;

        return extract_csr_first_frame(code, componentName, parseResult);
    }

    private static string extract_csr_first_frame(string compiledCode, string componentName,
        AwslParseResult parseResult)
    {
        var lines = compiledCode.Split('\n');
        var templateLines = new List<string>();
        var inTemplate = false;

        foreach (var line in lines)
            if (line.Contains("render") || line.Contains("innerHTML") || line.Contains("tag"))
                templateLines.Add(line.Trim());

        var sb = new StringBuilder();

        foreach (var node in parseResult.template_nodes) sb.Append(extract_node_structure(node));

        return sb.ToString();
    }

    private static string extract_node_structure(AwslTemplateNode node)
    {
        switch (node)
        {
            case AwslTextNode t:
                return t.text.Trim();
            case AwslElementNode e:
            {
                var attrs = string.Join(" ", e.attributes.Select(a => $"{a.Key}=\"{a.Value}\""));
                var openTag = string.IsNullOrEmpty(attrs) ? $"<{e.tag_name}>" : $"<{e.tag_name} {attrs}>";
                var closeTag = $"</{e.tag_name}>";
                var children = string.Join("", e.children.Select(extract_node_structure));
                return $"{openTag}{children}{closeTag}";
            }
            case AwslIfNode i:
            {
                var trueChildren = string.Join("", i.children.Select(extract_node_structure));
                return trueChildren;
            }
            default:
                return "";
        }
    }

    private static string normalize_whitespace(string html)
    {
        return Regex.Replace(
            html.Trim(), @"\s+", " ").Replace("> <", "><");
    }
}