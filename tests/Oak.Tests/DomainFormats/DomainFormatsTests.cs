using Oak.Vue;
using Oak.Obj;
using Oak.SpineAtlas;
using Std.Data.Text.Svg;

namespace Oak.DomainFormats.Tests;

public class SvgParserTests
{
    private readonly SvgParser _parser = new();

    [Fact]
    public void Parse_SimpleSvg_ShouldReturnDocument()
    {
        var source = "<svg xmlns=\"http://www.w3.org/2000/svg\"><rect width=\"100\" height=\"100\"/></svg>";
        var doc = _parser.Parse(source);
        Assert.NotNull(doc);
        Assert.NotNull(doc.Root);
    }

    [Fact]
    public void Parse_SvgWithViewBox_ShouldCaptureViewBox()
    {
        var source = "<svg viewBox=\"0 0 200 200\"><circle cx=\"50\" cy=\"50\" r=\"40\"/></svg>";
        var doc = _parser.Parse(source);
        Assert.NotNull(doc);
    }

    [Fact]
    public void Parse_SvgWithWidthHeight_ShouldCaptureDimensions()
    {
        var source = "<svg width=\"100\" height=\"100\"><rect/></svg>";
        var doc = _parser.Parse(source);
        Assert.NotNull(doc);
    }
}

public class VueSfcParserTests
{
    private readonly VueSfcParser _parser = new();

    [Fact]
    public void Parse_SimpleSfc_ShouldReturnResult()
    {
        var source = "<template><div>Hello</div></template><script>export default {}</script>";
        var result = _parser.Parse(source);
        Assert.NotNull(result);
    }

    [Fact]
    public void Parse_SfcWithStyle_ShouldCaptureStyle()
    {
        var source = "<template><div/></template><style scoped>.red { color: red; }</style>";
        var result = _parser.Parse(source);
        Assert.NotNull(result);
        Assert.NotEmpty(result.Styles);
    }

    [Fact]
    public void Parse_SfcWithScript_ShouldCaptureScript()
    {
        var source = "<script setup>const msg = 'hello'</script><template><div/></template>";
        var result = _parser.Parse(source);
        Assert.NotNull(result);
        Assert.NotNull(result.Script);
    }

    [Fact]
    public void Parse_EmptyTemplate_ShouldNotThrow()
    {
        var source = "<template></template>";
        var result = _parser.Parse(source);
        Assert.NotNull(result);
    }
}

public class ObjParserTests
{
    private readonly ObjParser _parser = new();

    [Fact]
    public void Parse_SimpleObj_ShouldReturnResult()
    {
        var source = "v 0.0 0.0 0.0\nv 1.0 0.0 0.0\nv 0.0 1.0 0.0\nf 1 2 3";
        var result = _parser.Parse(source.AsSpan());
        Assert.NotNull(result);
        Assert.Equal(3, result.Vertices.Count);
    }

    [Fact]
    public void Parse_ObjWithNormals_ShouldParseCorrectly()
    {
        var source = "v 0.0 0.0 0.0\nvn 0.0 1.0 0.0";
        var result = _parser.Parse(source.AsSpan());
        Assert.NotNull(result);
    }

    [Fact]
    public void Parse_EmptyObj_ShouldReturnEmptyResult()
    {
        var result = _parser.Parse("".AsSpan());
        Assert.NotNull(result);
        Assert.Empty(result.Vertices);
    }
}

public class SpineAtlasParserTests
{
    [Fact]
    public void Parse_SimpleAtlas_ShouldReturnData()
    {
        var source = "page.png\nsize: 512,512\nformat: RGBA8888\nregion\n  bounds: 0,0,64,64";
        var data = SpineAtlasParser.Parse(source);
        Assert.NotNull(data);
    }

    [Fact]
    public void Parse_EmptyAtlas_ShouldReturnEmptyData()
    {
        var data = SpineAtlasParser.Parse("");
        Assert.NotNull(data);
    }

    [Fact]
    public void Parse_AtlasWithMultiplePages_ShouldParseAll()
    {
        var source = "page1.png\nsize: 256,256\nregion1\n  bounds: 0,0,32,32\npage2.png\nsize: 512,512\nregion2\n  bounds: 0,0,64,64";
        var data = SpineAtlasParser.Parse(source);
        Assert.NotNull(data);
    }
}
