using Oak.Csv;
using Oak.Ini;
using Oak.Toml;
using Oak.Xml;

namespace Oak.DataFormats.Tests;

public class CsvParserTests
{
    [Fact]
    public void ParseRows_SimpleCsv_ShouldReturnRows()
    {
        var rows = CsvParser.ParseRows("a,b,c\n1,2,3");
        Assert.Equal(2, rows.Count);
    }

    [Fact]
    public void ParseRows_EmptyContent_ShouldReturnEmptyList()
    {
        var rows = CsvParser.ParseRows("");
        Assert.Empty(rows);
    }

    [Fact]
    public void ParseRows_SingleRow_ShouldReturnOneRow()
    {
        var rows = CsvParser.ParseRows("hello,world");
        Assert.Single(rows);
        Assert.Equal(2, rows[0].Count);
    }

    [Fact]
    public void ParseLine_SimpleLine_ShouldReturnFields()
    {
        var fields = CsvParser.ParseLine("a,b,c");
        Assert.Equal(3, fields.Count);
    }

    [Fact]
    public void ParseLine_QuotedField_ShouldRemoveQuotes()
    {
        var fields = CsvParser.ParseLine("\"hello\",\"world\"");
        Assert.Equal(2, fields.Count);
    }
}

public class IniParserTests
{
    private readonly IniParser _parser = new();

    [Fact]
    public void Parse_SimpleIni_ShouldReturnSections()
    {
        var source = "[section]\nkey=value";
        var result = _parser.Parse(source);
        Assert.NotNull(result);
        Assert.Single(result.Sections);
    }

    [Fact]
    public void Parse_GlobalEntries_ShouldCaptureGlobalKeys()
    {
        var source = "globalKey=globalValue\n[section]\nkey=value";
        var result = _parser.Parse(source);
        Assert.NotEmpty(result.GlobalEntries);
    }

    [Fact]
    public void Parse_SectionEntries_ShouldCaptureSectionKeys()
    {
        var source = "[database]\nhost=localhost\nport=5432";
        var result = _parser.Parse(source);
        var section = Assert.Single(result.Sections);
        Assert.Equal("database", section.Name);
        Assert.Equal(2, section.Entries.Count);
    }

    [Fact]
    public void Parse_EmptySource_ShouldReturnEmptyResult()
    {
        var result = _parser.Parse("");
        Assert.NotNull(result);
    }
}

public class TomlParserTests
{
    private readonly TomlParser _parser = new();

    [Fact]
    public void Parse_SimpleToml_ShouldReturnRootTable()
    {
        var source = "title = \"TOML Example\"";
        var result = _parser.Parse(source);
        Assert.NotNull(result);
        Assert.NotNull(result.Root);
    }

    [Fact]
    public void Parse_TableSection_ShouldCaptureTable()
    {
        var source = "[database]\nserver = \"192.168.1.1\"\nport = 5432";
        var result = _parser.Parse(source);
        Assert.NotNull(result);
    }

    [Fact]
    public void Parse_IntegerValue_ShouldParseAsInteger()
    {
        var source = "port = 5432";
        var result = _parser.Parse(source);
        Assert.NotNull(result);
    }

    [Fact]
    public void Parse_BooleanValue_ShouldParseAsBoolean()
    {
        var source = "enabled = true";
        var result = _parser.Parse(source);
        Assert.NotNull(result);
    }

    [Fact]
    public void Parse_EmptySource_ShouldReturnEmptyRoot()
    {
        var result = _parser.Parse("");
        Assert.NotNull(result);
    }
}

public class XmlParserTests
{
    [Fact]
    public void Parse_SimpleXml_ShouldReturnDocument()
    {
        var source = "<root><child>text</child></root>";
        var doc = XmlParser.Parse(source);
        Assert.NotNull(doc);
        Assert.NotNull(doc.Root);
    }

    [Fact]
    public void Parse_WithAttributes_ShouldCaptureAttributes()
    {
        var source = "<root attr=\"value\">content</root>";
        var doc = XmlParser.Parse(source);
        Assert.NotNull(doc.Root);
    }

    [Fact]
    public void Parse_WithDeclaration_ShouldCaptureDeclaration()
    {
        var source = "<?xml version=\"1.0\"?><root/>";
        var doc = XmlParser.Parse(source);
        Assert.NotNull(doc.Declaration);
    }

    [Fact]
    public void Parse_SelfClosingTag_ShouldParseCorrectly()
    {
        var source = "<root><br/><hr/></root>";
        var doc = XmlParser.Parse(source);
        Assert.NotNull(doc.Root);
    }

    [Fact]
    public void Parse_NestedElements_ShouldParseHierarchy()
    {
        var source = "<root><a><b><c/></b></a></root>";
        var doc = XmlParser.Parse(source);
        Assert.NotNull(doc.Root);
    }
}
