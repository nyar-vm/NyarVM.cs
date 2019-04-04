using Xunit;

namespace VOA.ToolChain.Tests;

public class AwslParserLegacyTests
{
    private readonly AwslParser _parser = new();

    [Fact]
    public void Parse_BasicTemplate()
    {
        const string source = @"
<template>
    <VStack>
        <Text>Hello World</Text>
        <Button>Click Me</Button>
    </VStack>
</template>
";

        var result = _parser.parse(source);

        Assert.NotNull(result);
        Assert.Equal("AnonymousWidget", result.name);
        Assert.NotNull(result.template_nodes);
        Assert.NotEmpty(result.template_nodes);
    }

    [Fact]
    public void Parse_TextInterpolation()
    {
        const string source = @"
<template>
    <VStack>
        <Text>Hello {name}!</Text>
        <Text>Age: {age}</Text>
    </VStack>
</template>
";

        var result = _parser.parse(source);

        Assert.NotNull(result);
        var vstack = result.template_nodes.First() as AwslElementNode;
        Assert.NotNull(vstack);

        var text1 = vstack.children[0] as AwslElementNode;
        Assert.NotNull(text1);
        var text1Children = text1.children;
        Assert.Equal(3, text1Children.Count);

        var textNode1 = text1Children[0] as AwslTextNode;
        var interpNode = text1Children[1] as AwslInterpolationNode;
        var textNode2 = text1Children[2] as AwslTextNode;

        Assert.NotNull(textNode1);
        Assert.NotNull(interpNode);
        Assert.NotNull(textNode2);
        Assert.Equal("Hello", textNode1.text);
        Assert.Equal("name", interpNode.expression);
        Assert.Equal("!", textNode2.text);
    }

    [Fact]
    public void Parse_ElementInterpolation()
    {
        const string source = @"
<template>
    <VStack>
        {items}
        <Text>Footer</Text>
    </VStack>
</template>
";

        var result = _parser.parse(source);

        Assert.NotNull(result);
        var vstack = result.template_nodes.First() as AwslElementNode;
        Assert.NotNull(vstack);

        var interpNode = vstack.children[0] as AwslInterpolationNode;
        var footerNode = vstack.children[1] as AwslElementNode;

        Assert.NotNull(interpNode);
        Assert.NotNull(footerNode);
        Assert.Equal("items", interpNode.expression);
        Assert.Equal("Text", footerNode.tag_name);
    }

    [Fact]
    public void Parse_JsxAttributeExpression()
    {
        const string source = @"
<widget>
    <Button style={btn_style} disabled={is_disabled}>
        {buttonText}
    </Button>
    <Button style=btn_style disabled=is_disabled>
        {buttonText}
    </Button>
</widget>
";

        var result = _parser.parse(source);

        Assert.NotNull(result);
        var button = result.template_nodes.First() as AwslElementNode;
        Assert.NotNull(button);

        Assert.Contains("style", button.attributes);
        Assert.Contains("disabled", button.attributes);
        Assert.Equal("btn_style", button.attributes["style"]);
        Assert.Equal("is_disabled", button.attributes["disabled"]);

        var interpNode = button.children[0] as AwslInterpolationNode;
        Assert.NotNull(interpNode);
        Assert.Equal("buttonText", interpNode.expression);
    }

    [Fact]
    public void Parse_ScriptProperties()
    {
        const string source =
            "<script>\n    let name: string = \"Alice\"\n    let age: int = 30\n    let active: bool = true\n</script>";

        var result = _parser.parse(source);

        Assert.NotNull(result);
        var properties = result.properties;
        Assert.Equal(3, properties.Count);

        var nameProp = properties[0];
        Assert.Equal("name", nameProp.name);
        Assert.Equal("string", nameProp.type_name);
        Assert.False(nameProp.is_readonly);
        Assert.Equal("Alice", nameProp.default_value);

        var ageProp = properties[1];
        Assert.Equal("age", ageProp.name);
        Assert.Equal("int", ageProp.type_name);
        Assert.False(ageProp.is_readonly);
        Assert.Equal("30", ageProp.default_value);

        var activeProp = properties[2];
        Assert.Equal("active", activeProp.name);
        Assert.Equal("bool", activeProp.type_name);
        Assert.False(activeProp.is_readonly);
        Assert.Equal("true", activeProp.default_value);
    }

    [Fact]
    public void Parse_ScriptProperties_Updated()
    {
        const string source = @"
<script>
    let name: string = ""Alice""
    let age: number = 30
</script>
";

        var result = _parser.parse(source);

        Assert.NotNull(result);
        Assert.Equal(2, result.properties.Count);
        Assert.Equal("name", result.properties[0].name);
        Assert.Equal("string", result.properties[0].type_name);
        Assert.Equal("Alice", result.properties[0].default_value);
        Assert.False(result.properties[0].is_readonly);
    }

    [Fact]
    public void Parse_StyleBlock()
    {
        const string source = @"
<style>
    .container {
        padding: 10px;
        margin: 5px;
    }
    .button {
        color: blue;
        background: white;
    }
</style>
";

        var result = _parser.parse(source);

        Assert.NotNull(result);
        var styles = result.styles;
        Assert.Equal(2, styles.Count);

        Assert.Contains("container", styles);
        Assert.Contains("button", styles);
        Assert.Contains("padding:10px", styles["container"]);
        Assert.Contains("color:blue", styles["button"]);
    }

    [Fact]
    public void Parse_NestedInterpolation()
    {
        const string source = @"
<template>
    <VStack>
        <Text>Items: {items.map(item => item.name).join(', ')}</Text>
        <Text>Total: {cart.reduce((sum, item) => sum + item.price, 0)}</Text>
    </VStack>
</template>
";

        var result = _parser.parse(source);

        Assert.NotNull(result);
        var vstack = result.template_nodes.First() as AwslElementNode;
        Assert.NotNull(vstack);

        var text1 = vstack.children[0] as AwslElementNode;
        var text2 = vstack.children[1] as AwslElementNode;

        Assert.NotNull(text1);
        Assert.NotNull(text2);

        var interp1 = text1.children[1] as AwslInterpolationNode;
        var interp2 = text2.children[1] as AwslInterpolationNode;

        Assert.NotNull(interp1);
        Assert.NotNull(interp2);
        Assert.Equal("items.map(item=>item.name).join(\", \")", interp1.expression);
        Assert.Equal("cart.reduce((sum,item)=>sum+item.price,0)", interp2.expression);
    }

    [Fact]
    public void Parse_MultipleJsxAttributes()
    {
        const string source = @"
<template>
    <Button class={btnClass} onClick={handleClick} disabled={isDisabled}>Click</Button>
</template>
";

        var result = _parser.parse(source);

        Assert.NotNull(result);
        var button = result.template_nodes.First() as AwslElementNode;
        Assert.NotNull(button);
        Assert.Equal("Button", button.tag_name);
        Assert.Equal("btnClass", button.attributes["class"]);
        Assert.Equal("handleClick", button.attributes["onClick"]);
        Assert.Equal("isDisabled", button.attributes["disabled"]);
    }

    [Fact]
    public void Parse_SelfClosingElementWithAttributes()
    {
        const string source = @"
<template>
    <Input type=""text"" value={inputValue} />
</template>
";

        var result = _parser.parse(source);

        Assert.NotNull(result);
        var input = result.template_nodes.First() as AwslElementNode;
        Assert.NotNull(input);
        Assert.Equal("Input", input.tag_name);
        Assert.True(input.is_self_closing);
        Assert.Equal("text", input.attributes["type"]);
        Assert.Equal("inputValue", input.attributes["value"]);
    }

    [Fact]
    public void Parse_NestedTemplateInterpolation()
    {
        const string source = @"
<template>
    <Container>
        <Header title={getTitle()} />
        <Body content={formatContent(data)} />
    </Container>
</template>
";

        var result = _parser.parse(source);

        Assert.NotNull(result);
        var container = result.template_nodes.First() as AwslElementNode;
        Assert.NotNull(container);
        Assert.Equal("Container", container.tag_name);

        var header = container.children[0] as AwslElementNode;
        Assert.NotNull(header);
        Assert.Equal("Header", header.tag_name);
        Assert.Equal("getTitle()", header.attributes["title"]);

        var body = container.children[1] as AwslElementNode;
        Assert.NotNull(body);
        Assert.Equal("Body", body.tag_name);
        Assert.Equal("formatContent(data)", body.attributes["content"]);
    }

    [Fact]
    public void Parse_IfAndForBlocks()
    {
        const string source = @"
<template>
    <if condition={showMessage}>
        <Text>{message}</Text>
    </if>
    <for each={item} in={items}>
        <Text>{item}</Text>
    </for>
</template>
";

        var result = _parser.parse(source);

        Assert.NotNull(result);
        var ifNode = result.template_nodes[0] as AwslIfNode;
        var forNode = result.template_nodes[1] as AwslForNode;

        Assert.NotNull(ifNode);
        Assert.NotNull(forNode);
        Assert.Equal("showMessage", ifNode.condition);
        Assert.Equal("item", forNode.iterator);
        Assert.Equal("items", forNode.iterable);
    }

    [Fact]
    public void Parse_SelfClosingElements()
    {
        const string source = @"
<template>
    <Image src={avatarUrl} />
    <Divider />
</template>
";

        var result = _parser.parse(source);

        Assert.NotNull(result);
        var image = result.template_nodes[0] as AwslElementNode;
        var divider = result.template_nodes[1] as AwslElementNode;

        Assert.NotNull(image);
        Assert.NotNull(divider);
        Assert.True(image.is_self_closing);
        Assert.True(divider.is_self_closing);
        Assert.Contains("src", image.attributes);
        Assert.Equal("avatarUrl", image.attributes["src"]);
    }

    [Fact]
    public void Parse_EmptyTemplate()
    {
        const string source = @"
<template>
</template>
";

        var result = _parser.parse(source);

        Assert.NotNull(result);
        Assert.NotNull(result.template_nodes);
        Assert.Empty(result.template_nodes);
    }

    [Fact]
    public void Parse_EmptyScript()
    {
        const string source = @"
<script>
</script>
";

        var result = _parser.parse(source);

        Assert.NotNull(result);
        Assert.NotNull(result.properties);
        Assert.Empty(result.properties);
    }

    [Fact]
    public void Parse_BareAttributeAndJsxAttribute()
    {
        const string source = @"
<widget>
    <Button style={btn_style} disabled={is_disabled}>
        {buttonText}
    </Button>
    <Button style=btn_style disabled=is_disabled>
        {buttonText}
    </Button>
</widget>
";

        var result = _parser.parse(source);

        Assert.NotNull(result);
        Assert.Equal(2, result.template_nodes.Count);

        var button1 = result.template_nodes[0] as AwslElementNode;
        var button2 = result.template_nodes[1] as AwslElementNode;

        Assert.NotNull(button1);
        Assert.NotNull(button2);
        Assert.Equal("Button", button1.tag_name);
        Assert.Equal("Button", button2.tag_name);

        Assert.Equal("btn_style", button1.attributes["style"]);
        Assert.Equal("is_disabled", button1.attributes["disabled"]);

        Assert.Equal("btn_style", button2.attributes["style"]);
        Assert.Equal("is_disabled", button2.attributes["disabled"]);
    }

    [Fact]
    public void Parse_MultiRootElements()
    {
        const string source = @"
<widget>
    <Header title=""Hello"" />
    <Body content={data} />
    <Footer />
</widget>
";

        var result = _parser.parse(source);

        Assert.NotNull(result);
        Assert.Equal(3, result.template_nodes.Count);

        var header = result.template_nodes[0] as AwslElementNode;
        var body = result.template_nodes[1] as AwslElementNode;
        var footer = result.template_nodes[2] as AwslElementNode;

        Assert.NotNull(header);
        Assert.NotNull(body);
        Assert.NotNull(footer);
        Assert.Equal("Header", header.tag_name);
        Assert.Equal("Body", body.tag_name);
        Assert.Equal("Footer", footer.tag_name);
    }

    [Fact]
    public void Parse_WidgetStandardSyntax()
    {
        const string source = @"
<widget>
    <Button style={btn_style} disabled={is_disabled}>
        {buttonText}
    </Button>
    <Button style=btn_style disabled=is_disabled>
        {buttonText}
    </Button>
</widget>
";

        var result = _parser.parse(source);

        Assert.NotNull(result);
        Assert.Equal(2, result.template_nodes.Count);
    }

    [Fact]
    public void Parse_EmptyStyle()
    {
        const string source = @"
<style>
</style>
";

        var result = _parser.parse(source);

        Assert.NotNull(result);
        Assert.NotNull(result.styles);
        Assert.Empty(result.styles);
    }
}