namespace Valkyrie.Tests.ParserTests;

public class ComponentParserTests : ValkyrieParserTestBase
{
    [Fact]
    public void Parse_SimpleComponent_ShouldSucceed()
    {
        var source = """
                     component Position {
                         x: f32;
                         y: f32;
                     }
                     """;
        var unit = parse_with_timeout(source);
        var component = Assert.IsType<ComponentDeclaration>(unit.Declarations[0]);

        Assert.Equal("Position", component.Name?.Name);
        Assert.Equal(2, component.Body!.Fields.Count);
        Assert.Equal("x", component.Body.Fields[0].Name);
        Assert.Equal("y", component.Body.Fields[1].Name);
    }

    [Fact]
    public void Parse_ComponentWithAnnotations_ShouldSucceed()
    {
        var source = """
                     # This is a position component
                     [Serializable]
                     public component Position {
                         x: f32;
                     }
                     """;
        var unit = parse_with_timeout(source);
        var component = Assert.IsType<DeclareComponent>(unit.Declarations[0]);

        var docs = component.Annotations.Documents;
        Assert.Single(docs);
        Assert.Contains("This is a position component", docs[0].Content);

        var attrs = component.Annotations.Attributes();
        Assert.Single(attrs);
        Assert.Equal("Serializable", attrs[0].Name);

        Assert.Single(component.Annotations.Modifiers);
        Assert.Equal("public", component.Annotations.Modifiers[0].Name);
    }

    [Fact]
    public void Parse_ComponentWithComplexFields_ShouldSucceed()
    {
        var source = """
                     component Data {
                         [Networked]
                         public velocity: Vector3;
                         
                         private secret: string;
                     }
                     """;
        var unit = parse_with_timeout(source);
        var component = Assert.IsType<DeclareComponent>(unit.Declarations[0]);

        Assert.Equal(2, component.Body!.Fields.Count);

        var field1 = component.Body.Fields[0];
        Assert.Single(field1.Annotations.Attributes());
        Assert.Equal("Networked", field1.Annotations.Attributes()[0].Name);

        var field2 = component.Body.Fields[1];
        Assert.Equal("secret", field2.Name);
    }
}
