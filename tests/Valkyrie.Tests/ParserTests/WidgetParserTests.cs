namespace Valkyrie.Tests.ParserTests;

public class WidgetParserTests : ValkyrieParserTestBase
{
    [Fact]
    public void Parse_SimpleWidget_ShouldSucceed()
    {
        var source = """
                     widget Button {
                         text: string;
                         onClick: event<void>;
                         
                         render(frame: Frame) {
                             // render logic
                         }
                     }
                     """;
        var unit = parse_with_timeout(source);
        var widget = Assert.IsType<DeclareWidget>(unit.Declarations[0]);

        Assert.Equal("Button", widget.Name?.Name);
        Assert.Equal(2, widget.Properties.Count);
        Assert.Equal("text", widget.Properties[0].Name);
        Assert.Equal("onClick", widget.Properties[1].Name);
        Assert.NotNull(widget.RenderMethod);
        Assert.Equal("render", widget.RenderMethod!.Name?.Name);
    }

    [Fact]
    public void Parse_WidgetWithAnnotations_ShouldSucceed()
    {
        var source = """
                     [EditorVisible]
                     public widget CustomPanel {
                         title: string;
                     }
                     """;
        var unit = parse_with_timeout(source);
        var widget = Assert.IsType<DeclareWidget>(unit.Declarations[0]);

        var attrs = widget.Annotations.Attributes();
        Assert.Single(attrs);
        Assert.Equal("EditorVisible", attrs[0].Name);

        Assert.Single(widget.Annotations.Modifiers);
        Assert.Equal("public", widget.Annotations.Modifiers[0].Name);
    }

    [Fact]
    public void Parse_WidgetWithoutRender_ShouldSucceed()
    {
        var source = """
                     widget Container {
                         children: list<Widget>;
                     }
                     """;
        var unit = parse_with_timeout(source);
        var widget = Assert.IsType<DeclareWidget>(unit.Declarations[0]);

        Assert.Single(widget.Properties);
        Assert.Null(widget.RenderMethod);
    }
}
