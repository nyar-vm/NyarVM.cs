using Oak.Markdown.Syntax;

namespace Oak.Markdown.Tests;

public class GfmCapabilityTests : MarkdownTestBase
{
    #region 表格

    [Fact]
    public void Table_Basic()
    {
        var source = "| Name | Age |\n| --- | --- |\n| Alice | 30 |\n| Bob | 25 |";
        var doc = ParseWithTimeout(source);
        var table = Assert.IsType<MarkdownTable>(Assert.Single(doc.Children));
        Assert.Equal(2, table.Header.Cells.Count);
        Assert.Equal(2, table.Rows.Count);
    }

    [Fact]
    public void Table_RenderHtml()
    {
        var source = "| A | B |\n| --- | --- |\n| 1 | 2 |";
        var renderer = new MarkdownHtmlRenderer();
        var result = renderer.Render(source);
        Assert.Contains("<table>", result.Html);
        Assert.Contains("<thead>", result.Html);
        Assert.Contains("<th>A</th>", result.Html);
        Assert.Contains("<td>1</td>", result.Html);
    }

    [Fact]
    public void Table_SingleColumn()
    {
        var source = "| Header |\n| --- |\n| Cell |";
        var doc = ParseWithTimeout(source);
        var table = Assert.IsType<MarkdownTable>(Assert.Single(doc.Children));
        Assert.Single(table.Header.Cells);
        Assert.Single(table.Rows);
    }

    #endregion

    #region 任务列表

    [Fact]
    public void TaskList_Checked()
    {
        var source = "- [x] Done";
        var doc = ParseWithTimeout(source);
        var list = Assert.IsType<MarkdownList>(Assert.Single(doc.Children));
        var task = Assert.IsType<MarkdownTaskListItem>(Assert.Single(list.Items));
        Assert.True(task.IsChecked);
    }

    [Fact]
    public void TaskList_Unchecked()
    {
        var source = "- [ ] Todo";
        var doc = ParseWithTimeout(source);
        var list = Assert.IsType<MarkdownList>(Assert.Single(doc.Children));
        var task = Assert.IsType<MarkdownTaskListItem>(Assert.Single(list.Items));
        Assert.False(task.IsChecked);
    }

    [Fact]
    public void TaskList_Mixed()
    {
        var source = "- [x] Done\n- [ ] Todo\n- [x] Also done";
        var doc = ParseWithTimeout(source);
        var list = Assert.IsType<MarkdownList>(Assert.Single(doc.Children));
        Assert.Equal(3, list.Items.Count);
        Assert.True(Assert.IsType<MarkdownTaskListItem>(list.Items[0]).IsChecked);
        Assert.False(Assert.IsType<MarkdownTaskListItem>(list.Items[1]).IsChecked);
        Assert.True(Assert.IsType<MarkdownTaskListItem>(list.Items[2]).IsChecked);
    }

    [Fact]
    public void TaskList_RenderHtml()
    {
        var source = "- [x] Done\n- [ ] Todo";
        var renderer = new MarkdownHtmlRenderer();
        var result = renderer.Render(source);
        Assert.Contains("type=\"checkbox\" checked disabled", result.Html);
        Assert.Contains("type=\"checkbox\" disabled", result.Html);
    }

    #endregion

    #region 删除线

    [Fact]
    public void Strikethrough_Basic()
    {
        var doc = ParseWithTimeout("This is ~~deleted~~ text");
        var para = Assert.IsType<MarkdownParagraph>(Assert.Single(doc.Children));
        Assert.Contains(para.Children, c => c.NodeType == MarkdownNodeType.Strikethrough);
    }

    [Fact]
    public void Strikethrough_Content()
    {
        var doc = ParseWithTimeout("~~deleted content~~");
        var para = Assert.IsType<MarkdownParagraph>(Assert.Single(doc.Children));
        var strike = Assert.IsType<MarkdownStrikethrough>(Assert.Single(para.Children));
        var text = Assert.IsType<MarkdownText>(Assert.Single(strike.Children));
        Assert.Equal("deleted content", text.Content);
    }

    [Fact]
    public void Strikethrough_RenderHtml()
    {
        var renderer = new MarkdownHtmlRenderer();
        var result = renderer.Render("~~deleted~~");
        Assert.Contains("<del>deleted</del>", result.Html);
    }

    #endregion

    #region 高亮

    [Fact]
    public void Highlight_Basic()
    {
        var doc = ParseWithTimeout("This is ==highlighted== text");
        var para = Assert.IsType<MarkdownParagraph>(Assert.Single(doc.Children));
        Assert.Contains(para.Children, c => c.NodeType == MarkdownNodeType.Highlight);
    }

    [Fact]
    public void Highlight_RenderHtml()
    {
        var renderer = new MarkdownHtmlRenderer();
        var result = renderer.Render("==highlighted==");
        Assert.Contains("<mark>highlighted</mark>", result.Html);
    }

    #endregion

    #region 自动链接

    [Fact]
    public void AutoLink_Http()
    {
        var doc = ParseWithTimeout("Visit https://example.com for more");
        var para = Assert.IsType<MarkdownParagraph>(Assert.Single(doc.Children));
        Assert.Contains(para.Children, c => c.NodeType == MarkdownNodeType.Link);
    }

    [Fact]
    public void AutoLink_RenderHtml()
    {
        var renderer = new MarkdownHtmlRenderer();
        var result = renderer.Render("Visit https://example.com for more");
        Assert.Contains("href=\"https://example.com\"", result.Html);
    }

    #endregion

    #region 脚注

    [Fact]
    public void Footnote_Reference()
    {
        var doc = ParseWithTimeout("Text with footnote[^1].\n\n[^1]: Footnote content");
        Assert.Equal(2, doc.Children.Count);
    }

    [Fact]
    public void Footnote_RenderHtml()
    {
        var renderer = new MarkdownHtmlRenderer();
        var result = renderer.Render("Text[^1].\n\n[^1]: Content");
        Assert.Contains("footnote-ref", result.Html);
        Assert.Contains("footnotes", result.Html);
    }

    #endregion

    #region 行内 HTML

    [Fact]
    public void HtmlInline_Basic()
    {
        var doc = ParseWithTimeout("Text with <span>inline</span> html");
        var para = Assert.IsType<MarkdownParagraph>(Assert.Single(doc.Children));
        Assert.Contains(para.Children, c => c.NodeType == MarkdownNodeType.HtmlInline);
    }

    [Fact]
    public void HtmlInline_RenderHtml()
    {
        var renderer = new MarkdownHtmlRenderer();
        var result = renderer.Render("Text <br> more");
        Assert.Contains("<br>", result.Html);
    }

    #endregion

    #region 块级 HTML

    [Fact]
    public void HtmlBlock_Div()
    {
        var doc = ParseWithTimeout("<div>\nSome content\n</div>");
        var html = Assert.IsType<MarkdownHtmlBlock>(Assert.Single(doc.Children));
        Assert.Contains("<div>", html.Content);
    }

    [Fact]
    public void HtmlBlock_RenderHtml()
    {
        var renderer = new MarkdownHtmlRenderer();
        var result = renderer.Render("<div>content</div>");
        Assert.Contains("<div>content</div>", result.Html);
    }

    #endregion

    #region 引用式链接

    [Fact]
    public void ReferenceLink_Basic()
    {
        var source = "[Oak][1]\n\n[1]: https://oak.dev";
        var doc = ParseWithTimeout(source);
        var para = Assert.IsType<MarkdownParagraph>(doc.Children[0]);
        Assert.Contains(para.Children, c => c.NodeType == MarkdownNodeType.Link);
    }

    [Fact]
    public void ReferenceLink_RenderHtml()
    {
        var source = "[Oak][1]\n\n[1]: https://oak.dev";
        var renderer = new MarkdownHtmlRenderer();
        var result = renderer.Render(source);
        Assert.Contains("href=\"https://oak.dev\"", result.Html);
    }

    #endregion

    #region Setext 标题

    [Fact]
    public void SetextHeading_H1()
    {
        var source = "Title\n=====";
        var doc = ParseWithTimeout(source);
        var heading = Assert.IsType<MarkdownHeading>(Assert.Single(doc.Children));
        Assert.Equal(1, heading.Level);
    }

    [Fact]
    public void SetextHeading_H2()
    {
        var source = "Subtitle\n-----";
        var doc = ParseWithTimeout(source);
        var heading = Assert.IsType<MarkdownHeading>(Assert.Single(doc.Children));
        Assert.Equal(2, heading.Level);
    }

    #endregion

    #region 缩进代码块

    [Fact]
    public void IndentedCodeBlock_Basic()
    {
        var source = "    code line 1\n    code line 2";
        var doc = ParseWithTimeout(source);
        var code = Assert.IsType<MarkdownIndentedCodeBlock>(Assert.Single(doc.Children));
        Assert.Contains("code line 1", code.Content);
    }

    [Fact]
    public void IndentedCodeBlock_RenderHtml()
    {
        var source = "    var x = 1;";
        var renderer = new MarkdownHtmlRenderer();
        var result = renderer.Render(source);
        Assert.Contains("<pre><code>", result.Html);
        Assert.Contains("var x = 1;", result.Html);
    }

    #endregion

    #region 引用块

    [Fact]
    public void Blockquote_Basic()
    {
        var source = "> This is a quote";
        var doc = ParseWithTimeout(source);
        var blockquote = Assert.IsType<MarkdownBlockquote>(Assert.Single(doc.Children));
        Assert.Single(blockquote.Children);
    }

    [Fact]
    public void Blockquote_RenderHtml()
    {
        var source = "> Quote text";
        var renderer = new MarkdownHtmlRenderer();
        var result = renderer.Render(source);
        Assert.Contains("<blockquote>", result.Html);
        Assert.Contains("Quote text", result.Html);
    }

    [Fact]
    public void Blockquote_Multiline()
    {
        var source = "> Line 1\n> Line 2";
        var doc = ParseWithTimeout(source);
        var blockquote = Assert.IsType<MarkdownBlockquote>(Assert.Single(doc.Children));
        Assert.NotNull(blockquote);
    }

    #endregion

    #region 图片

    [Fact]
    public void Image_Basic()
    {
        var doc = ParseWithTimeout("![Alt text](https://example.com/image.png)");
        var para = Assert.IsType<MarkdownParagraph>(Assert.Single(doc.Children));
        Assert.Contains(para.Children, c => c.NodeType == MarkdownNodeType.Image);
    }

    [Fact]
    public void Image_RenderHtml()
    {
        var renderer = new MarkdownHtmlRenderer();
        var result = renderer.Render("![Alt](https://example.com/img.png)");
        Assert.Contains("<img src=\"https://example.com/img.png\" alt=\"Alt\"", result.Html);
    }

    [Fact]
    public void Image_WithTitle()
    {
        var renderer = new MarkdownHtmlRenderer();
        var result = renderer.Render("![Alt](https://example.com/img.png \"Title\")");
        Assert.Contains("title=\"Title\"", result.Html);
    }

    #endregion

    #region 链接

    [Fact]
    public void Link_WithTitle()
    {
        var renderer = new MarkdownHtmlRenderer();
        var result = renderer.Render("[Oak](https://oak.dev \"Official\")");
        Assert.Contains("href=\"https://oak.dev\"", result.Html);
        Assert.Contains("title=\"Official\"", result.Html);
    }

    [Fact]
    public void Link_ExternalNewTab()
    {
        var renderer = new MarkdownHtmlRenderer();
        var result = renderer.Render("[Link](https://example.com)");
        Assert.Contains("target=\"_blank\"", result.Html);
        Assert.Contains("rel=\"noopener noreferrer\"", result.Html);
    }

    [Fact]
    public void Link_InternalNoNewTab()
    {
        var renderer = new MarkdownHtmlRenderer();
        var result = renderer.Render("[Link](/about)");
        Assert.DoesNotContain("target=\"_blank\"", result.Html);
    }

    #endregion

    #region 代码块

    [Fact]
    public void CodeBlock_WithLanguage()
    {
        var renderer = new MarkdownHtmlRenderer();
        var result = renderer.Render("```csharp\nConsole.WriteLine();\n```");
        Assert.Contains("class=\"language-csharp\"", result.Html);
    }

    [Fact]
    public void CodeBlock_NoLanguage()
    {
        var renderer = new MarkdownHtmlRenderer();
        var result = renderer.Render("```\nplain code\n```");
        Assert.Contains("<pre><code>", result.Html);
        Assert.DoesNotContain("class=\"language-", result.Html);
    }

    [Fact]
    public void CodeBlock_EscapesHtml()
    {
        var renderer = new MarkdownHtmlRenderer();
        var result = renderer.Render("```html\n<div>\n```");
        Assert.Contains("&lt;div&gt;", result.Html);
    }

    #endregion

    #region 行内代码

    [Fact]
    public void InlineCode_Basic()
    {
        var doc = ParseWithTimeout("Use `var x = 1;` here");
        var para = Assert.IsType<MarkdownParagraph>(Assert.Single(doc.Children));
        Assert.Contains(para.Children, c => c.NodeType == MarkdownNodeType.InlineCode);
    }

    [Fact]
    public void InlineCode_EscapesHtml()
    {
        var renderer = new MarkdownHtmlRenderer();
        var result = renderer.Render("`<div>`");
        Assert.Contains("<code>&lt;div&gt;</code>", result.Html);
    }

    #endregion

    #region 换行

    [Fact]
    public void HardBreak_TwoSpaces()
    {
        var source = "Line 1  \nLine 2";
        var doc = ParseWithTimeout(source);
        var para = Assert.IsType<MarkdownParagraph>(Assert.Single(doc.Children));
        Assert.Contains(para.Children, c => c.NodeType == MarkdownNodeType.LineBreak);
    }

    [Fact]
    public void HardBreak_Backslash()
    {
        var source = "Line 1\\\nLine 2";
        var doc = ParseWithTimeout(source);
        var para = Assert.IsType<MarkdownParagraph>(Assert.Single(doc.Children));
        Assert.Contains(para.Children, c => c.NodeType == MarkdownNodeType.LineBreak);
    }

    [Fact]
    public void SoftBreak_SingleNewline()
    {
        var source = "Line 1\nLine 2";
        var doc = ParseWithTimeout(source);
        var para = Assert.IsType<MarkdownParagraph>(Assert.Single(doc.Children));
        Assert.Contains(para.Children, c => c.NodeType == MarkdownNodeType.SoftBreak);
    }

    #endregion

    #region 转义

    [Fact]
    public void Escape_Asterisk()
    {
        var doc = ParseWithTimeout("This is \\*not italic\\* text");
        var para = Assert.IsType<MarkdownParagraph>(Assert.Single(doc.Children));
        var text = para.Children.OfType<MarkdownText>().Select(t => t.Content);
        Assert.Contains("*not italic*", string.Join("", text));
    }

    #endregion

    #region 渲染选项

    [Fact]
    public void RenderOptions_NoHeadingIds()
    {
        var renderer = new MarkdownHtmlRenderer(new MarkdownHtmlOptions { GenerateHeadingIds = false });
        var result = renderer.Render("# Title");
        Assert.DoesNotContain("id=", result.Html);
    }

    [Fact]
    public void RenderOptions_NoCodeHighlight()
    {
        var renderer = new MarkdownHtmlRenderer(new MarkdownHtmlOptions { HighlightCode = false });
        var result = renderer.Render("```js\ncode\n```");
        Assert.DoesNotContain("class=\"language-", result.Html);
    }

    [Fact]
    public void RenderOptions_SoftBreakAsBr()
    {
        var renderer = new MarkdownHtmlRenderer(new MarkdownHtmlOptions { SoftBreakAsLineBreak = true });
        var result = renderer.Render("Line 1\nLine 2");
        Assert.Contains("<br", result.Html);
    }

    #endregion

    #region 混合内容

    [Fact]
    public void Mixed_StrongAndEmphasis()
    {
        var doc = ParseWithTimeout("This is ***bold and italic*** text");
        var para = Assert.IsType<MarkdownParagraph>(Assert.Single(doc.Children));
        Assert.Contains(para.Children, c => c.NodeType == MarkdownNodeType.Strong);
    }

    [Fact]
    public void Mixed_ListWithCode()
    {
        var source = "- item with `code`\n- **bold** item";
        var doc = ParseWithTimeout(source);
        var list = Assert.IsType<MarkdownList>(Assert.Single(doc.Children));
        Assert.Equal(2, list.Items.Count);
    }

    [Fact]
    public void Mixed_HeadingAndParagraph()
    {
        var source = "# Title\n\nParagraph text";
        var doc = ParseWithTimeout(source);
        Assert.Equal(2, doc.Children.Count);
        Assert.IsType<MarkdownHeading>(doc.Children[0]);
        Assert.IsType<MarkdownParagraph>(doc.Children[1]);
    }

    #endregion
}
