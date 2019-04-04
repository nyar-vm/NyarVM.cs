using Oak.Markdown.Syntax;

namespace Oak.Markdown.Tests;

public class MarkdownParserTests : MarkdownTestBase
{
    [Fact]
    public void EmptyInput_ProducesEmptyDocument()
    {
        var doc = ParseWithTimeout("");
        Assert.NotNull(doc);
        Assert.Empty(doc.Children);
    }

    [Fact]
    public void Heading_H1()
    {
        var doc = ParseWithTimeout("# Title");
        var heading = Assert.Single(doc.Children);
        var h = Assert.IsType<MarkdownHeading>(heading);
        Assert.Equal(1, h.Level);
    }

    [Fact]
    public void Heading_H3()
    {
        var doc = ParseWithTimeout("### Section");
        var heading = Assert.Single(doc.Children);
        var h = Assert.IsType<MarkdownHeading>(heading);
        Assert.Equal(3, h.Level);
    }

    [Fact]
    public void Paragraph()
    {
        var doc = ParseWithTimeout("Hello world");
        var para = Assert.Single(doc.Children);
        Assert.IsType<MarkdownParagraph>(para);
    }

    [Fact]
    public void CodeBlock()
    {
        var source = """
            ```csharp
            Console.WriteLine("Hello");
            ```
            """;
        var doc = ParseWithTimeout(source);
        var block = Assert.Single(doc.Children);
        var code = Assert.IsType<MarkdownCodeBlock>(block);
        Assert.Equal("csharp", code.Language);
    }

    [Fact]
    public void UnorderedList()
    {
        var source = """
            - apple
            - banana
            - cherry
            """;
        var doc = ParseWithTimeout(source);
        var block = Assert.Single(doc.Children);
        var list = Assert.IsType<MarkdownList>(block);
        Assert.False(list.IsOrdered);
        Assert.Equal(3, list.Items.Count);
    }

    [Fact]
    public void OrderedList()
    {
        var source = """
            1. first
            2. second
            3. third
            """;
        var doc = ParseWithTimeout(source);
        var block = Assert.Single(doc.Children);
        var list = Assert.IsType<MarkdownList>(block);
        Assert.True(list.IsOrdered);
        Assert.Equal(3, list.Items.Count);
    }

    [Fact]
    public void HorizontalRule()
    {
        var doc = ParseWithTimeout("---");
        var block = Assert.Single(doc.Children);
        Assert.IsType<MarkdownHorizontalRule>(block);
    }

    [Fact]
    public void InlineStrong()
    {
        var doc = ParseWithTimeout("This is **bold** text");
        var para = Assert.IsType<MarkdownParagraph>(Assert.Single(doc.Children));
        Assert.Contains(para.Children, c => c.NodeType == MarkdownNodeType.Strong);
    }

    [Fact]
    public void InlineEmphasis()
    {
        var doc = ParseWithTimeout("This is *italic* text");
        var para = Assert.IsType<MarkdownParagraph>(Assert.Single(doc.Children));
        Assert.Contains(para.Children, c => c.NodeType == MarkdownNodeType.Emphasis);
    }

    [Fact]
    public void Link()
    {
        var doc = ParseWithTimeout("[Oak](https://oak.dev)");
        var para = Assert.IsType<MarkdownParagraph>(Assert.Single(doc.Children));
        Assert.Contains(para.Children, c => c.NodeType == MarkdownNodeType.Link);
    }

    [Fact]
    public void MarkdownLanguageConfig_Gfm()
    {
        var config = MarkdownLanguageConfig.Gfm;
        Assert.True(config.EnableTables);
        Assert.True(config.EnableTaskLists);
        Assert.True(config.EnableStrikethrough);
    }
}
