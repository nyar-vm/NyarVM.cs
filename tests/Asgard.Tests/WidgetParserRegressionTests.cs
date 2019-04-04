using Xunit;

namespace VOA.ToolChain.Tests;

public class AwslParserRegressionTests
{
    private readonly AwslParser _parser = new();

    #region const 声明测试

    [Fact]
    public void Parse_ConstProperty_ShouldBeReadonly()
    {
        const string source = @"
<script>
    const title: string = ""Hello""
</script>
";

        var result = _parser.parse(source);

        Assert.NotNull(result);
        Assert.Single(result.properties);
        Assert.True(result.properties[0].is_readonly);
        Assert.Equal("title", result.properties[0].name);
    }

    #endregion

    #region 控制流嵌套测试

    [Fact]
    public void Parse_NestedIfInFor_ShouldParseCorrectly()
    {
        const string source = @"
<template>
    <for each={item} in={items}>
        <if condition={item.active}>
            <Text>{item.name}</Text>
        </if>
    </for>
</template>
";

        var result = _parser.parse(source);

        Assert.NotNull(result);
        var forNode = result.template_nodes[0] as AwslForNode;
        Assert.NotNull(forNode);
        Assert.Equal("item", forNode.iterator);
        Assert.Equal("items", forNode.iterable);
        Assert.Single(forNode.children);
        var ifNode = forNode.children[0] as AwslIfNode;
        Assert.NotNull(ifNode);
        Assert.Equal("item.active", ifNode.condition);
    }

    #endregion

    #region style 块回归测试

    [Fact]
    public void Parse_StyleWithHyphenatedClass_ShouldParseCorrectly()
    {
        const string source = @"
<style>
    .my-class {
        font-size: 14px;
    }
</style>
";

        var result = _parser.parse(source);

        Assert.NotNull(result);
        Assert.Single(result.styles);
        Assert.Contains("my-class", result.styles);
    }

    #endregion

    #region 完整 Widget 组件回归测试

    [Fact]
    public void Parse_FullWidgetComponent_ShouldParseAllSections()
    {
        const string source = @"
<script>
    let count: int = 0
    let label: string = ""Click me""
    const max: int = 100
</script>

<widget>
    <VStack>
        <Text>{label}: {count}</Text>
        <Button onClick={increment} disabled={count >= max}>{label}</Button>
    </VStack>
</widget>

<style>
    .container {
        padding: 10px;
    }
</style>
";

        var result = _parser.parse(source, "Counter.awsl");

        Assert.NotNull(result);
        Assert.Equal("Counter", result.name);
        Assert.Equal(3, result.properties.Count);
        Assert.NotEmpty(result.template_nodes);
        Assert.NotEmpty(result.styles);
    }

    #endregion

    #region DiagnosticSink 注入测试

    [Fact]
    public void Parse_WithDiagnosticSink_ShouldNotThrow()
    {
        var diagnostics = new DiagnosticSink();
        var parser = new AwslParser(diagnostics);

        const string source = "<template><Text>Hello</Text></template>";

        var result = parser.parse(source);

        Assert.NotNull(result);
    }

    #endregion

    #region filePath 参数测试

    [Fact]
    public void Parse_WithFilePath_ShouldExtractComponentName()
    {
        const string source = "<template><Text>Hello</Text></template>";

        var result = _parser.parse(source, "MyButton.awsl");

        Assert.NotNull(result);
        Assert.Equal("MyButton", result.name);
    }

    [Fact]
    public void Parse_WithEmptyFilePath_ShouldUseDefaultName()
    {
        const string source = "<template><Text>Hello</Text></template>";

        var result = _parser.parse(source);

        Assert.NotNull(result);
        Assert.Equal("AnonymousWidget", result.name);
    }

    #endregion

    #region 类型映射测试

    [Fact]
    public void Parse_NumberType_ShouldMapToF64()
    {
        const string source = @"
<script>
    let score: number = 100
</script>
";

        var result = _parser.parse(source);

        Assert.NotNull(result);
        Assert.Single(result.properties);
        Assert.Equal("f64", result.properties[0].type_name);
    }

    [Fact]
    public void Parse_BooleanType_ShouldMapToBool()
    {
        const string source = @"
<script>
    let active: boolean = true
</script>
";

        var result = _parser.parse(source);

        Assert.NotNull(result);
        Assert.Single(result.properties);
        Assert.Equal("bool", result.properties[0].type_name);
    }

    [Fact]
    public void Parse_ObjectType_ShouldMapToMap()
    {
        const string source = @"
<script>
    let data: object = {}
</script>
";

        var result = _parser.parse(source);

        Assert.NotNull(result);
        Assert.Single(result.properties);
        Assert.Equal("map", result.properties[0].type_name);
    }

    [Fact]
    public void Parse_ArrayType_ShouldMapToArray()
    {
        const string source = @"
<script>
    let items: array = []
</script>
";

        var result = _parser.parse(source);

        Assert.NotNull(result);
        Assert.Single(result.properties);
        Assert.Equal("list", result.properties[0].type_name);
    }

    #endregion

    #region AwslValueKind 覆盖测试

    [Fact]
    public void Parse_PropertyWithArrayDefault_ShouldReturnArrayKind()
    {
        const string source = @"
<script>
    let items: Array = []
</script>
";

        var result = _parser.parse(source);

        Assert.NotNull(result);
        Assert.Single(result.properties);
        Assert.Equal(AwslValueKind.array, result.properties[0].default_value_kind);
    }

    [Fact]
    public void Parse_PropertyWithObjectDefault_ShouldParseSuccessfully()
    {
        const string source = @"
<script>
    let config: Map = {}
</script>
";

        var result = _parser.parse(source);

        Assert.NotNull(result);
        Assert.Single(result.properties);
    }

    [Fact]
    public void Parse_PropertyWithIdentifierDefault_ShouldReturnIdentifierKind()
    {
        const string source = @"
<script>
    let color = red
</script>
";

        var result = _parser.parse(source);

        Assert.NotNull(result);
        Assert.Single(result.properties);
        Assert.Equal(AwslValueKind.identifier, result.properties[0].default_value_kind);
    }

    [Fact]
    public void Parse_PropertyWithNoDefault_ShouldParseSuccessfully()
    {
        const string source = @"
<script>
    let name: string = """"
</script>
";

        var result = _parser.parse(source);

        Assert.NotNull(result);
        Assert.Single(result.properties);
    }

    #endregion

    #region 属性解析回归测试

    [Fact]
    public void Parse_QuotedStringAttribute_ShouldExtractValue()
    {
        const string source = @"
<template>
    <Text class=""header"">Hello</Text>
</template>
";

        var result = _parser.parse(source);

        Assert.NotNull(result);
        var text = result.template_nodes.First() as AwslElementNode;
        Assert.NotNull(text);
        Assert.Contains("class", text.attributes);
        Assert.Equal("header", text.attributes["class"]);
    }

    [Fact]
    public void Parse_BooleanAttribute_ShouldDefaultToTrue()
    {
        const string source = @"
<template>
    <Input disabled />
</template>
";

        var result = _parser.parse(source);

        Assert.NotNull(result);
        var input = result.template_nodes.First() as AwslElementNode;
        Assert.NotNull(input);
        Assert.Contains("disabled", input.attributes);
        Assert.Equal("true", input.attributes["disabled"]);
    }

    #endregion
}