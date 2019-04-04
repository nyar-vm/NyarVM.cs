using Xunit;

namespace Oak.Csv.Tests;

public class CsvParserTests
{
    #region 边界测试

    [Fact]
    public void ParseLine_EmptyString_ShouldReturnSingleEmptyField()
    {
        var fields = CsvParser.ParseLine("");

        Assert.Single(fields);
        Assert.Equal("", fields[0]);
    }

    [Fact]
    public void ParseRows_EmptyContent_ShouldReturnEmptyList()
    {
        var rows = CsvParser.ParseRows("");

        Assert.Empty(rows);
    }

    [Fact]
    public void ParseRows_WhitespaceOnly_ShouldReturnEmptyList()
    {
        var rows = CsvParser.ParseRows("   ");

        Assert.Empty(rows);
    }

    [Fact]
    public void ParseLine_OnlyCommas_ShouldReturnEmptyFields()
    {
        var fields = CsvParser.ParseLine(",,");

        Assert.Equal(3, fields.Count);
        Assert.All(fields, f => Assert.Equal("", f));
    }

    #endregion

    #region 基本解析测试

    [Fact]
    public void ParseLine_SingleField_ShouldReturnOneField()
    {
        var fields = CsvParser.ParseLine("hello");

        Assert.Single(fields);
        Assert.Equal("hello", fields[0]);
    }

    [Fact]
    public void ParseLine_TwoFields_ShouldReturnTwoFields()
    {
        var fields = CsvParser.ParseLine("a,b");

        Assert.Equal(2, fields.Count);
        Assert.Equal("a", fields[0]);
        Assert.Equal("b", fields[1]);
    }

    [Fact]
    public void ParseLine_NumericFields_ShouldBeString()
    {
        var fields = CsvParser.ParseLine("1,2,3");

        Assert.Equal(3, fields.Count);
        Assert.Equal("1", fields[0]);
        Assert.Equal("2", fields[1]);
        Assert.Equal("3", fields[2]);
    }

    [Fact]
    public void ParseLine_ChineseCharFields_ShouldReturnCorrectString()
    {
        var fields = CsvParser.ParseLine("你好,世界");

        Assert.Equal(2, fields.Count);
        Assert.Equal("你好", fields[0]);
        Assert.Equal("世界", fields[1]);
    }

    [Fact]
    public void ParseLine_SpecialChars_ShouldPreserveChar()
    {
        var fields = CsvParser.ParseLine("a@b,c#d");

        Assert.Equal(2, fields.Count);
        Assert.Equal("a@b", fields[0]);
        Assert.Equal("c#d", fields[1]);
    }

    #endregion

    #region 引号测试

    [Fact]
    public void ParseLine_QuotedField_ShouldStripQuotes()
    {
        var fields = CsvParser.ParseLine("\"hello\"");

        Assert.Equal("hello", fields[0]);
    }

    [Fact]
    public void ParseLine_QuotedFieldWithComma_ShouldNotSplit()
    {
        var fields = CsvParser.ParseLine("\"a,b\"");

        Assert.Single(fields);
        Assert.Equal("a,b", fields[0]);
    }

    [Fact]
    public void ParseLine_MixedQuotedUnquoted_ShouldHandleCorrectly()
    {
        var fields = CsvParser.ParseLine("\"hello\",world");

        Assert.Equal(2, fields.Count);
        Assert.Equal("hello", fields[0]);
        Assert.Equal("world", fields[1]);
    }

    [Fact]
    public void ParseLine_QuotedFieldWithNewline_ShouldPreserveNewline()
    {
        var fields = CsvParser.ParseLine("\"a\nb\"");

        Assert.Equal("a\nb", fields[0]);
    }

    [Fact]
    public void ParseLine_EscapedQuote_ShouldReturnSingleQuote()
    {
        var fields = CsvParser.ParseLine("\"a\"\"b\"");

        Assert.Equal("a\"b", fields[0]);
    }

    [Fact]
    public void ParseLine_EmptyQuotedField_ShouldReturnEmpty()
    {
        var fields = CsvParser.ParseLine("\"\"");

        Assert.Equal("", fields[0]);
    }

    #endregion

    #region 多行解析测试

    [Fact]
    public void ParseRows_SingleRow_ShouldReturnOneRow()
    {
        var rows = CsvParser.ParseRows("a,b,c");

        Assert.Single(rows);
        Assert.Equal(3, rows[0].Count);
    }

    [Fact]
    public void ParseRows_MultipleRows_ShouldReturnAllRows()
    {
        var rows = CsvParser.ParseRows("a,b\nc,d\ne,f");

        Assert.Equal(3, rows.Count);
    }

    [Fact]
    public void ParseRows_BlankLines_ShouldBeSkipped()
    {
        var rows = CsvParser.ParseRows("a,b\n\nc,d");

        Assert.Equal(2, rows.Count);
    }

    [Fact]
    public void ParseRows_MixedQuotedAndUnquotedRows_ShouldAllBeParsed()
    {
        var rows = CsvParser.ParseRows("\"hello\",world\nfoo,\"bar\"");

        Assert.Equal(2, rows.Count);
        Assert.Equal("hello", rows[0][0]);
        Assert.Equal("world", rows[0][1]);
        Assert.Equal("foo", rows[1][0]);
        Assert.Equal("bar", rows[1][1]);
    }

    [Fact]
    public void ParseRows_CarriageReturns_ShouldBeTrimmed()
    {
        var rows = CsvParser.ParseRows("a,b\r\nc,d\r\n");

        Assert.Equal(2, rows.Count);
        Assert.Equal("a", rows[0][0]);
        Assert.Equal("b", rows[0][1]);
    }

    #endregion

    #region 综合测试

    [Fact]
    public void ParseLine_ComplexRow_ShouldParseCorrectly()
    {
        var fields = CsvParser.ParseLine("1,\"hello, world\",3.14,true");

        Assert.Equal(4, fields.Count);
        Assert.Equal("1", fields[0]);
        Assert.Equal("hello, world", fields[1]);
        Assert.Equal("3.14", fields[2]);
        Assert.Equal("true", fields[3]);
    }

    [Fact]
    public void ParseLine_ManyQuotedFields_ShouldAllBeUnquoted()
    {
        var fields = CsvParser.ParseLine("\"a\",\"b\",\"c\",\"d\",\"e\"");

        Assert.Equal(5, fields.Count);
        Assert.All(fields, f => Assert.DoesNotContain("\"", f));
    }

    [Fact]
    public void ParseLine_TrailingEmptyField_ShouldBePreserved()
    {
        var fields = CsvParser.ParseLine("a,");

        Assert.Equal(2, fields.Count);
        Assert.Equal("a", fields[0]);
        Assert.Equal("", fields[1]);
    }

    [Fact]
    public void ParseLine_LeadingEmptyField_ShouldBePreserved()
    {
        var fields = CsvParser.ParseLine(",a");

        Assert.Equal(2, fields.Count);
        Assert.Equal("", fields[0]);
        Assert.Equal("a", fields[1]);
    }

    [Fact]
    public void ParseLine_SpacesInUnquoted_ShouldBePreserved()
    {
        var fields = CsvParser.ParseLine("hello world,foo bar");

        Assert.Equal("hello world", fields[0]);
        Assert.Equal("foo bar", fields[1]);
    }

    #endregion
}
