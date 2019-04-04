using Oak.Markdown.Syntax;

namespace Oak.Markdown.Tests;

public class MarkdownMathTests : MarkdownTestBase
{
    #region 解析测试

    [Fact]
    public void MathInline_SingleDollar()
    {
        var doc = ParseWithTimeout("行内公式 $E = mc^2$ 测试");
        var para = Assert.IsType<MarkdownParagraph>(Assert.Single(doc.Children));
        var math = Assert.Single(para.Children.OfType<MarkdownMathInline>());
        Assert.Equal("E = mc^2", math.Content);
    }

    [Fact]
    public void MathInline_Multiple()
    {
        var doc = ParseWithTimeout("公式 $a+b$ 和 $c+d$ 并列");
        var para = Assert.IsType<MarkdownParagraph>(Assert.Single(doc.Children));
        var maths = para.Children.OfType<MarkdownMathInline>().ToList();
        Assert.Equal(2, maths.Count);
        Assert.Equal("a+b", maths[0].Content);
        Assert.Equal("c+d", maths[1].Content);
    }

    [Fact]
    public void MathBlock_Multiline()
    {
        var source = """
            $$
            \int_{-\infty}^{\infty} e^{-x^2} dx = \sqrt{\pi}
            $$
            """;
        var doc = ParseWithTimeout(source);
        var math = Assert.IsType<MarkdownMathBlock>(Assert.Single(doc.Children));
        Assert.Contains(@"\int", math.Content);
        Assert.Contains(@"\sqrt{\pi}", math.Content);
    }

    [Fact]
    public void MathBlock_SingleLine()
    {
        var source = "$$E = mc^2$$";
        var doc = ParseWithTimeout(source);
        var child = Assert.Single(doc.Children);
        var math = Assert.IsType<MarkdownMathBlock>(child);
        Assert.Equal("E = mc^2", math.Content);
    }

    [Fact]
    public void MathBlock_NotWrappedInParagraph()
    {
        var source = "前文\n\n$$\nx^2 + y^2 = r^2\n$$\n\n后文";
        var doc = ParseWithTimeout(source);
        var types = doc.Children.Select(c => c.NodeType).ToList();
        Assert.Equal(
            [MarkdownNodeType.Paragraph, MarkdownNodeType.MathBlock, MarkdownNodeType.Paragraph],
            types);
    }

    [Fact]
    public void MathBlock_ComplexFormula()
    {
        var source = """
            $$
            \nabla \times \mathbf{E} = -\frac{\partial \mathbf{B}}{\partial t}
            $$
            """;
        var doc = ParseWithTimeout(source);
        var math = Assert.IsType<MarkdownMathBlock>(Assert.Single(doc.Children));
        Assert.Contains(@"\nabla", math.Content);
        Assert.Contains(@"\frac", math.Content);
    }

    #endregion

    #region 渲染测试

    [Fact]
    public void Render_MathInline_KaTeX_ProducesKatexHtml()
    {
        var renderer = new MarkdownHtmlRenderer(new MarkdownHtmlOptions
        {
            MathMode = MathRenderMode.KaTeX
        });
        var result = renderer.Render("公式 $E = mc^2$ 测试");
        Assert.Contains("katex", result.Html);
        Assert.Contains("katex-mathml", result.Html);
        Assert.Contains("katex-html", result.Html);
        Assert.DoesNotContain("$E = mc^2$", result.Html);
        Assert.True(result.HasKaTeXMath);
    }

    [Fact]
    public void Render_MathBlock_KaTeX_ProducesKatexHtml()
    {
        var renderer = new MarkdownHtmlRenderer(new MarkdownHtmlOptions
        {
            MathMode = MathRenderMode.KaTeX
        });
        var result = renderer.Render("$$\nE = mc^2\n$$");
        Assert.Contains("katex", result.Html);
        Assert.Contains("katex-mathml", result.Html);
        Assert.DoesNotContain("$$E = mc^2$$", result.Html);
        Assert.True(result.HasKaTeXMath);
    }

    [Fact]
    public void Render_MathInline_Raw_ProducesDollarDelimiters()
    {
        var renderer = new MarkdownHtmlRenderer(new MarkdownHtmlOptions
        {
            MathMode = MathRenderMode.Raw
        });
        var result = renderer.Render("公式 $E = mc^2$ 测试");
        Assert.Contains("$E = mc^2$", result.Html);
        Assert.False(result.HasKaTeXMath);
    }

    [Fact]
    public void Render_MathBlock_Raw_ProducesDoubleDollarDelimiters()
    {
        var renderer = new MarkdownHtmlRenderer(new MarkdownHtmlOptions
        {
            MathMode = MathRenderMode.Raw
        });
        var result = renderer.Render("$$\nE = mc^2\n$$");
        Assert.Contains("$$", result.Html);
        Assert.False(result.HasKaTeXMath);
    }

    [Fact]
    public void Render_MathInline_MathJax_ProducesParenDelimiters()
    {
        var renderer = new MarkdownHtmlRenderer(new MarkdownHtmlOptions
        {
            MathMode = MathRenderMode.MathJax
        });
        var result = renderer.Render("公式 $E = mc^2$ 测试");
        Assert.Contains(@"\(E = mc^2\)", result.Html);
        Assert.False(result.HasKaTeXMath);
    }

    [Fact]
    public void Render_NoMath_HasKaTeXMathIsFalse()
    {
        var renderer = new MarkdownHtmlRenderer(new MarkdownHtmlOptions
        {
            MathMode = MathRenderMode.KaTeX
        });
        var result = renderer.Render("普通文本没有数学公式");
        Assert.False(result.HasKaTeXMath);
    }

    [Fact]
    public void Render_KaTeX_EEqualsMcSquared_ContainsSuperscript()
    {
        var renderer = new MarkdownHtmlRenderer(new MarkdownHtmlOptions
        {
            MathMode = MathRenderMode.KaTeX
        });
        var result = renderer.Render("$E = mc^2$");
        Assert.Contains("katex", result.Html);
        Assert.Contains("msup", result.Html);
    }

    [Fact]
    public void Render_KaTeX_Integral_ProducesCorrectHtml()
    {
        var renderer = new MarkdownHtmlRenderer(new MarkdownHtmlOptions
        {
            MathMode = MathRenderMode.KaTeX
        });
        var result = renderer.Render("$$\n\\int_0^1 x dx\n$$");
        Assert.Contains("katex", result.Html);
        Assert.Contains("∫", result.Html);
    }

    #endregion

    #region KaTeXService 测试

    [Fact]
    public void KaTeXService_RenderInline_ReturnsKatexHtml()
    {
        var service = new KaTeXService();
        var html = service.RenderToString("E = mc^2", displayMode: false);
        Assert.Contains("katex", html);
        Assert.Contains("katex-mathml", html);
    }

    [Fact]
    public void KaTeXService_RenderBlock_ReturnsKatexHtml()
    {
        var service = new KaTeXService();
        var html = service.RenderToString("E = mc^2", displayMode: true);
        Assert.Contains("katex", html);
        Assert.Contains("katex-mathml", html);
    }

    [Fact]
    public void KaTeXService_GetCssContent_ReturnsNonEmpty()
    {
        var service = new KaTeXService();
        var css = service.GetCssContent();
        Assert.NotEmpty(css);
        Assert.Contains("katex", css);
    }

    #endregion
}
