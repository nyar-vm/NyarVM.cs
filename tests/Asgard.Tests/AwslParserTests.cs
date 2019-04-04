using Xunit;

namespace VOA.ToolChain.Tests;

/// <summary>
///     AWSL Parser 完整测试套件，覆盖所有语法特性
/// </summary>
public class AwslParserTests
{
    private readonly AwslParser _parser = new();

    #region 事件处理测试

    /// <summary>
    ///     解析事件处理值为表达式
    /// </summary>
    [Fact]
    public void Parse_EventHandlerWithExpression_ShouldExtractExpression()
    {
        const string source = @"
<widget>
    <Button @click=""{() => handleClick(item.id)}"">Click</Button>
</widget>";

        var result = _parser.parse(source);

        var button = result.template_nodes[0] as AwslElementNode;
        Assert.NotNull(button);
        var clickHandler = button.attributes["@click"];
        Assert.Contains("handleClick", clickHandler);
    }

    #endregion

    #region Islands 标签测试

    /// <summary>
    ///     解析 &lt;widget&gt; 块
    /// </summary>
    [Fact]
    public void Parse_WidgetBlock_ShouldExtractTemplateNodes()
    {
        const string source = @"
<widget>
    <VStack>
        <Text>Hello</Text>
    </VStack>
</widget>";

        var result = _parser.parse(source);

        Assert.NotEmpty(result.template_nodes);
        var vstack = result.template_nodes[0] as AwslElementNode;
        Assert.NotNull(vstack);
        Assert.Equal("VStack", vstack.tag_name);
        Assert.Single(vstack.children);
    }

    /// <summary>
    ///     解析 &lt;template&gt; 块
    /// </summary>
    [Fact]
    public void Parse_TemplateBlock_ShouldWorkSameAsWidget()
    {
        const string source = @"
<template>
    <HStack>
        <Button>Click</Button>
    </HStack>
</template>";

        var result = _parser.parse(source);

        Assert.NotEmpty(result.template_nodes);
        var hstack = result.template_nodes[0] as AwslElementNode;
        Assert.NotNull(hstack);
        Assert.Equal("HStack", hstack.tag_name);
    }

    /// <summary>
    ///     解析 &lt;script&gt; 块中的 let 声明
    /// </summary>
    [Fact]
    public void Parse_ScriptBlock_ShouldExtractProperties()
    {
        const string source = @"
<script>
    let title: string = ""AWSL""
    let count: i32 = 42
</script>";

        var result = _parser.parse(source);

        Assert.Equal(2, result.properties.Count);
        Assert.Equal("title", result.properties[0].name);
        Assert.Equal("string", result.properties[0].type_name);
        Assert.Equal("AWSL", result.properties[0].default_value);
        Assert.Equal("count", result.properties[1].name);
        Assert.Equal("i32", result.properties[1].type_name);
        Assert.Equal("42", result.properties[1].default_value);
    }

    /// <summary>
    ///     解析 &lt;style&gt; 块中的 CSS 规则
    /// </summary>
    [Fact]
    public void Parse_StyleBlock_ShouldExtractCssClasses()
    {
        const string source = @"
<style>
    .header {
        font-size: 24px;
        color: red;
    }
    .footer {
        margin-top: 20px;
    }
</style>";

        var result = _parser.parse(source);

        Assert.Equal(2, result.styles.Count);
        Assert.Contains("header", result.styles);
        Assert.Contains("footer", result.styles);
    }

    /// <summary>
    ///     解析包含所有 Islands 的完整组件
    /// </summary>
    [Fact]
    public void Parse_AllIslands_ShouldExtractAllSections()
    {
        const string source = @"
<script>
    let name: string = ""World""
</script>

<widget>
    <Text>Hello {name}</Text>
</widget>

<style>
    .greeting { color: green; }
</style>";

        var result = _parser.parse(source, "Greeting.awsl");

        Assert.Equal("Greeting", result.name);
        Assert.Single(result.properties);
        Assert.NotEmpty(result.template_nodes);
        Assert.Single(result.styles);
    }

    #endregion

    #region 响应式绑定测试

    /// <summary>
    ///     解析 @bind 数据绑定属性
    /// </summary>
    [Fact]
    public void Parse_AtBind_ShouldExtractBindingAttribute()
    {
        const string source = @"
<widget>
    <Input @bind=""inputValue"" />
</widget>";

        var result = _parser.parse(source);

        var input = result.template_nodes[0] as AwslElementNode;
        Assert.NotNull(input);
        Assert.True(input.attributes.ContainsKey("@bind"));
        Assert.Equal("inputValue", input.attributes["@bind"]);
    }

    /// <summary>
    ///     解析 @input 事件绑定属性
    /// </summary>
    [Fact]
    public void Parse_AtInput_ShouldExtractEventAttribute()
    {
        const string source = @"
<widget>
    <TextField @input=""onInputChange"" />
</widget>";

        var result = _parser.parse(source);

        var field = result.template_nodes[0] as AwslElementNode;
        Assert.NotNull(field);
        Assert.True(field.attributes.ContainsKey("@input"));
        Assert.Equal("onInputChange", field.attributes["@input"]);
    }

    /// <summary>
    ///     解析 @click 事件处理器
    /// </summary>
    [Fact]
    public void Parse_AtClick_ShouldExtractClickHandler()
    {
        const string source = @"
<widget>
    <Button @click=""handleClick"">Click Me</Button>
</widget>";

        var result = _parser.parse(source);

        var button = result.template_nodes[0] as AwslElementNode;
        Assert.NotNull(button);
        Assert.True(button.attributes.ContainsKey("@click"));
        Assert.Equal("handleClick", button.attributes["@click"]);
    }

    /// <summary>
    ///     解析多个 @ 绑定在同一元素上
    /// </summary>
    [Fact]
    public void Parse_MultipleAtBindings_ShouldExtractAll()
    {
        const string source = @"
<widget>
    <Input @bind=""value"" @input=""onChange"" @focus=""onFocus"" />
</widget>";

        var result = _parser.parse(source);

        var input = result.template_nodes[0] as AwslElementNode;
        Assert.NotNull(input);
        Assert.Equal(3, input.attributes.Count);
        Assert.Equal("value", input.attributes["@bind"]);
        Assert.Equal("onChange", input.attributes["@input"]);
        Assert.Equal("onFocus", input.attributes["@focus"]);
    }

    #endregion

    #region 组件声明测试

    /// <summary>
    ///     解析 micro 函数声明
    /// </summary>
    [Fact]
    public void Parse_MicroFunction_ShouldExtractMethod()
    {
        const string source = @"
<script>
    micro increment(n: i32): i32 {
        return n + 1
    }
</script>";

        var result = _parser.parse(source);

        Assert.Single(result.methods);
        var method = result.methods[0];
        Assert.Equal("increment", method.name);
        Assert.True(method.is_micro);
        Assert.Contains("(n:i32)", method.parameters);
    }

    /// <summary>
    ///     解析 const 只读声明
    /// </summary>
    [Fact]
    public void Parse_ConstDeclaration_ShouldBeReadonly()
    {
        const string source = @"
<script>
    const PI: f64 = 3.14
    const MAX: i32 = 100
</script>";

        var result = _parser.parse(source);

        Assert.Equal(2, result.properties.Count);
        Assert.True(result.properties[0].is_readonly);
        Assert.Equal("PI", result.properties[0].name);
        Assert.True(result.properties[1].is_readonly);
        Assert.Equal("MAX", result.properties[1].name);
    }

    /// <summary>
    ///     解析 script 中混合 let、const 和 micro
    /// </summary>
    [Fact]
    public void Parse_ScriptWithMixedDeclarations_ShouldExtractAll()
    {
        const string source = @"
<script>
    let name: string = ""Alice""
    const version: i32 = 1
    micro greet(): string {
        return ""Hello ""
    }
</script>";

        var result = _parser.parse(source);

        Assert.Equal(2, result.properties.Count);
        Assert.False(result.properties[0].is_readonly);
        Assert.True(result.properties[1].is_readonly);
        Assert.Single(result.methods);
    }

    #endregion

    #region 模板表达式测试

    /// <summary>
    ///     解析模板中的 {expression} 插值
    /// </summary>
    [Fact]
    public void Parse_TemplateExpression_ShouldCreateInterpolationNode()
    {
        const string source = @"
<widget>
    <Text>Count: {count}</Text>
</widget>";

        var result = _parser.parse(source);

        var text = result.template_nodes[0] as AwslElementNode;
        Assert.NotNull(text);
        Assert.Equal(2, text.children.Count);

        var interp = text.children[1] as AwslInterpolationNode;
        Assert.NotNull(interp);
        Assert.Equal("count", interp.expression);
    }

    /// <summary>
    ///     解析顶层表达式插值（不作为元素子节点）
    /// </summary>
    [Fact]
    public void Parse_TopLevelInterpolation_ShouldWork()
    {
        const string source = @"
<widget>
    {dynamicContent}
    <Text>Static</Text>
</widget>";

        var result = _parser.parse(source);

        Assert.Equal(2, result.template_nodes.Count);
        var interp = result.template_nodes[0] as AwslInterpolationNode;
        Assert.NotNull(interp);
        Assert.Equal("dynamicContent", interp.expression);
    }

    /// <summary>
    ///     解析带函数调用的表达式插值
    /// </summary>
    [Fact]
    public void Parse_ExpressionWithFunctionCall_ShouldPreserveCall()
    {
        const string source = @"
<widget>
    <Text>{formatName(user.firstName,user.lastName)}</Text>
</widget>";

        var result = _parser.parse(source);

        var text = result.template_nodes[0] as AwslElementNode;
        Assert.NotNull(text);
        var interp = text.children[0] as AwslInterpolationNode;
        Assert.NotNull(interp);
        Assert.Contains("formatName", interp.expression);
        Assert.Contains("user.firstName", interp.expression);
        Assert.Contains("user.lastName", interp.expression);
    }

    #endregion

    #region 条件/循环指令测试

    /// <summary>
    ///     解析 if 块（花括号条件语法）
    /// </summary>
    [Fact]
    public void Parse_IfBlockWithBracedCondition_ShouldWork()
    {
        const string source = @"
<widget>
    <if {isVisible}>
        <Text>Visible</Text>
    </if>
</widget>";

        var result = _parser.parse(source);

        var ifNode = result.template_nodes[0] as AwslIfNode;
        Assert.NotNull(ifNode);
        Assert.Equal("isVisible", ifNode.condition);
        Assert.NotEmpty(ifNode.children);
    }

    /// <summary>
    ///     解析 if 块（属性条件语法）
    /// </summary>
    [Fact]
    public void Parse_IfBlockWithAttributeCondition_ShouldWork()
    {
        const string source = @"
<widget>
    <if condition={showMessage}>
        <Text>{message}</Text>
    </if>
</widget>";

        var result = _parser.parse(source);

        var ifNode = result.template_nodes[0] as AwslIfNode;
        Assert.NotNull(ifNode);
        Assert.Equal("showMessage", ifNode.condition);
    }

    /// <summary>
    ///     解析 if-else 块
    /// </summary>
    [Fact]
    public void Parse_IfElseBlock_ShouldExtractBothBranches()
    {
        const string source = @"
<widget>
    <if {isAuthenticated}>
        <Text>Welcome</Text>
        <else/>
        <Text>Please login</Text>
    </if>
</widget>";

        var result = _parser.parse(source);

        var ifNode = result.template_nodes[0] as AwslIfNode;
        Assert.NotNull(ifNode);
        Assert.Equal("isAuthenticated", ifNode.condition);
        Assert.NotEmpty(ifNode.children);
        Assert.NotEmpty(ifNode.else_children);
    }

    /// <summary>
    ///     解析 loop 块
    /// </summary>
    [Fact]
    public void Parse_LoopBlock_ShouldExtractIteratorAndIterable()
    {
        const string source = @"
<widget>
    <loop item in {items}>
        <Text>{item}</Text>
    </loop>
</widget>";

        var result = _parser.parse(source);

        var forNode = result.template_nodes[0] as AwslForNode;
        Assert.NotNull(forNode);
        Assert.Equal("item", forNode.iterator);
        Assert.Equal("items", forNode.iterable);
        Assert.NotEmpty(forNode.children);
    }

    /// <summary>
    ///     解析 for 块（等价于 loop）
    /// </summary>
    [Fact]
    public void Parse_ForBlock_ShouldWorkLikeLoop()
    {
        const string source = @"
<widget>
    <for each={product} in={products}>
        <Text>{product.name}</Text>
    </for>
</widget>";

        var result = _parser.parse(source);

        var forNode = result.template_nodes[0] as AwslForNode;
        Assert.NotNull(forNode);
        Assert.Equal("product", forNode.iterator);
        Assert.Equal("products", forNode.iterable);
    }

    #endregion

    #region 属性绑定测试

    /// <summary>
    ///     解析普通类属性 class="foo"
    /// </summary>
    [Fact]
    public void Parse_ClassAttribute_ShouldExtractValue()
    {
        const string source = @"
<widget>
    <Text class=""title"">Hello</Text>
</widget>";

        var result = _parser.parse(source);

        var text = result.template_nodes[0] as AwslElementNode;
        Assert.NotNull(text);
        Assert.True(text.attributes.ContainsKey("class"));
        Assert.Equal("title", text.attributes["class"]);
    }

    /// <summary>
    ///     解析布尔属性（无值）
    /// </summary>
    [Fact]
    public void Parse_BooleanAttribute_ShouldDefaultToTrue()
    {
        const string source = @"
<widget>
    <Button disabled>Click</Button>
</widget>";

        var result = _parser.parse(source);

        var button = result.template_nodes[0] as AwslElementNode;
        Assert.NotNull(button);
        Assert.Equal("true", button.attributes["disabled"]);
    }

    /// <summary>
    ///     解析混合属性：字符串、表达式、布尔
    /// </summary>
    [Fact]
    public void Parse_MixedAttributes_ShouldExtractAll()
    {
        const string source = @"
<widget>
    <Button class=""primary"" disabled onClick={handleClick}>
        Submit
    </Button>
</widget>";

        var result = _parser.parse(source);

        var button = result.template_nodes[0] as AwslElementNode;
        Assert.NotNull(button);
        Assert.Equal("primary", button.attributes["class"]);
        Assert.Equal("true", button.attributes["disabled"]);
        Assert.Equal("handleClick", button.attributes["onClick"]);
    }

    #endregion

    #region 嵌套与复杂结构测试

    /// <summary>
    ///     解析深层嵌套结构
    /// </summary>
    [Fact]
    public void Parse_DeeplyNestedStructure_ShouldParseCorrectly()
    {
        const string source = @"
<widget>
    <VStack>
        <HStack>
            <Text>Label</Text>
            <Input @bind=""value"" />
        </HStack>
        <Button @click=""submit"">OK</Button>
    </VStack>
</widget>";

        var result = _parser.parse(source);

        var vstack = result.template_nodes[0] as AwslElementNode;
        Assert.NotNull(vstack);
        Assert.Equal(2, vstack.children.Count);

        var hstack = vstack.children[0] as AwslElementNode;
        Assert.NotNull(hstack);
        Assert.Equal("HStack", hstack.tag_name);
        Assert.Equal(2, hstack.children.Count);

        var button = vstack.children[1] as AwslElementNode;
        Assert.NotNull(button);
        Assert.Equal("Button", button.tag_name);
        Assert.Equal("submit", button.attributes["@click"]);
    }

    /// <summary>
    ///     解析循环中的条件
    /// </summary>
    [Fact]
    public void Parse_IfInsideFor_ShouldWork()
    {
        const string source = @"
<widget>
    <loop item in {items}>
        <if {item.active}>
            <Text>{item.name}</Text>
        </if>
    </loop>
</widget>";

        var result = _parser.parse(source);

        var forNode = result.template_nodes[0] as AwslForNode;
        Assert.NotNull(forNode);
        Assert.Single(forNode.children);
        var ifNode = forNode.children[0] as AwslIfNode;
        Assert.NotNull(ifNode);
        Assert.Equal("item.active", ifNode.condition);
    }

    /// <summary>
    ///     解析自闭合元素
    /// </summary>
    [Fact]
    public void Parse_SelfClosingElement_ShouldHaveIsSelfClosingTrue()
    {
        const string source = @"
<widget>
    <Image src=""avatar.png"" />
    <Divider />
</widget>";

        var result = _parser.parse(source);

        var image = result.template_nodes[0] as AwslElementNode;
        Assert.NotNull(image);
        Assert.True(image.is_self_closing);
        Assert.Equal("avatar.png", image.attributes["src"]);

        var divider = result.template_nodes[1] as AwslElementNode;
        Assert.NotNull(divider);
        Assert.True(divider.is_self_closing);
    }

    /// <summary>
    ///     解析完整 AWSL 组件（包含所有 Islands 和语法特性）
    /// </summary>
    [Fact]
    public void Parse_FullAwslComponent_ShouldParseAllFeatures()
    {
        const string source = @"
<script>
    let title: string = ""AWSL Demo""
    let items: list = []
    const maxItems: i32 = 10

    micro addItem(name: string): void {
        items.push(name)
    }
</script>

<widget>
    <VStack>
        <Text class=""title"">{title}</Text>
        <if {items.length > 0}>
            <loop item in {items}>
                <HStack>
                    <Text>{item}</Text>
                    <Button @click=""{() => removeItem(item)}"">X</Button>
                </HStack>
            </loop>
            <else/>
            <Text class=""empty"">No items</Text>
        </if>
        <HStack>
            <Input @bind=""newItemName"" placeholder=""New item"" />
            <Button @click=""addItem"" disabled={items.length >= maxItems}>
                Add
            </Button>
        </HStack>
    </VStack>
</widget>

<style>
    .title { font-size: 24px; }
    .empty { color: gray; }
</style>";

        var result = _parser.parse(source, "TodoList.awsl");

        Assert.Equal("TodoList", result.name);
        Assert.Equal(3, result.properties.Count);
        Assert.Single(result.methods);
        Assert.NotEmpty(result.template_nodes);
        Assert.Equal(2, result.styles.Count);
    }

    #endregion

    #region 边界情况测试

    /// <summary>
    ///     解析空 widget 块
    /// </summary>
    [Fact]
    public void Parse_EmptyWidget_ShouldReturnEmptyTemplateNodes()
    {
        const string source = "<widget></widget>";

        var result = _parser.parse(source);

        Assert.Empty(result.template_nodes);
    }

    /// <summary>
    ///     解析空 script 块
    /// </summary>
    [Fact]
    public void Parse_EmptyScript_ShouldReturnEmptyProperties()
    {
        const string source = "<script></script>";

        var result = _parser.parse(source);

        Assert.Empty(result.properties);
        Assert.Empty(result.methods);
    }

    /// <summary>
    ///     解析空 style 块
    /// </summary>
    [Fact]
    public void Parse_EmptyStyle_ShouldReturnEmptyStyles()
    {
        const string source = "<style></style>";

        var result = _parser.parse(source);

        Assert.Empty(result.styles);
    }

    #endregion
}