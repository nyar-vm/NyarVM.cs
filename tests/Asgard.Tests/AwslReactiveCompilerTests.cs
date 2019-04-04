using Xunit;

namespace VOA.ToolChain.Tests;

public sealed class AwslReactiveCompilerTests
{
    private readonly AwslReactiveCompiler _compiler = new();

    [Fact]
    public void Compile_SimpleComponent_GeneratesFunction()
    {
        var parseResult = new AwslParseResult
        {
            name = "hello",
            template_nodes = [new AwslElementNode { tag_name = "div" }]
        };

        var result = _compiler.compile(parseResult, "app");

        Assert.Equal("hello", result.component_name);
        Assert.Contains("function Hello(props)", result.java_script);
        Assert.Contains("createElement('div')", result.java_script);
    }

    [Fact]
    public void Compile_SignalProperty_GeneratesCreateSignal()
    {
        var parseResult = new AwslParseResult
        {
            name = "counter",
            properties =
            [
                new AwslProperty
                {
                    name = "count", type_name = "i32", default_value = "0", default_value_kind = AwslValueKind.number
                }
            ],
            template_nodes = [new AwslElementNode { tag_name = "span" }]
        };

        var result = _compiler.compile(parseResult, "app");

        Assert.Contains("createSignal(0)", result.java_script);
        Assert.Contains("getcount", result.java_script);
        Assert.Contains("setcount", result.java_script);
    }

    [Fact]
    public void Compile_BooleanProperty_GeneratesCorrectDefault()
    {
        var parseResult = new AwslParseResult
        {
            name = "toggle",
            properties =
            [
                new AwslProperty
                {
                    name = "active", type_name = "bool", default_value = "true",
                    default_value_kind = AwslValueKind.boolean
                }
            ],
            template_nodes = [new AwslElementNode { tag_name = "div" }]
        };

        var result = _compiler.compile(parseResult, "app");

        Assert.Contains("createSignal(true)", result.java_script);
    }

    [Fact]
    public void Compile_StringProperty_GeneratesQuotedDefault()
    {
        var parseResult = new AwslParseResult
        {
            name = "greeting",
            properties =
            [
                new AwslProperty
                {
                    name = "name", type_name = "string", default_value = "World",
                    default_value_kind = AwslValueKind.@string
                }
            ],
            template_nodes = [new AwslElementNode { tag_name = "div" }]
        };

        var result = _compiler.compile(parseResult, "app");

        Assert.Contains("createSignal(\"World\")", result.java_script);
    }

    [Fact]
    public void Compile_ReadonlyPropertyWithoutDefault_GeneratesProp()
    {
        var parseResult = new AwslParseResult
        {
            name = "card",
            properties =
            [
                new AwslProperty
                {
                    name = "title", type_name = "string", is_readonly = true, default_value = null,
                    default_value_kind = AwslValueKind.none
                }
            ],
            template_nodes = [new AwslElementNode { tag_name = "div" }]
        };

        var result = _compiler.compile(parseResult, "app");

        Assert.Contains("createSignal(props?.title)", result.java_script);
    }

    [Fact]
    public void Compile_ComputedProperty_GeneratesCreateMemo()
    {
        var parseResult = new AwslParseResult
        {
            name = "calc",
            properties =
            [
                new AwslProperty
                {
                    name = "count", type_name = "i32", default_value = "0", default_value_kind = AwslValueKind.number
                },
                new AwslProperty
                {
                    name = "double_count", type_name = "i32", default_value = "count * 2",
                    default_value_kind = AwslValueKind.expression
                }
            ],
            template_nodes = [new AwslElementNode { tag_name = "div" }]
        };

        var result = _compiler.compile(parseResult, "app");

        Assert.Contains("createMemo", result.java_script);
        Assert.Contains("getcount()", result.java_script);
    }

    [Fact]
    public void Compile_TextInterpolation_GeneratesDynamicText()
    {
        var parseResult = new AwslParseResult
        {
            name = "display",
            properties =
            [
                new AwslProperty
                {
                    name = "message", type_name = "string", default_value = "\"hi\"",
                    default_value_kind = AwslValueKind.@string
                }
            ],
            template_nodes =
            [
                new AwslElementNode
                {
                    tag_name = "p",
                    children = [new AwslInterpolationNode { expression = "message" }]
                }
            ]
        };

        var result = _compiler.compile(parseResult, "app");

        Assert.Contains("dynamicText", result.java_script);
        Assert.Contains("getmessage()", result.java_script);
    }

    [Fact]
    public void Compile_IfConditional_GeneratesConditional()
    {
        var parseResult = new AwslParseResult
        {
            name = "cond",
            properties =
            [
                new AwslProperty
                {
                    name = "visible", type_name = "bool", default_value = "true",
                    default_value_kind = AwslValueKind.boolean
                }
            ],
            template_nodes =
            [
                new AwslIfNode
                {
                    condition = "visible",
                    children = [new AwslTextNode { text = "shown" }],
                    else_children = [new AwslTextNode { text = "hidden" }]
                }
            ]
        };

        var result = _compiler.compile(parseResult, "app");

        Assert.Contains("conditional(", result.java_script);
        Assert.Contains("getvisible()", result.java_script);
    }

    [Fact]
    public void Compile_ForLoop_GeneratesListMap()
    {
        var parseResult = new AwslParseResult
        {
            name = "list",
            properties =
            [
                new AwslProperty
                {
                    name = "items", type_name = "list", default_value = "[]", default_value_kind = AwslValueKind.array
                }
            ],
            template_nodes =
            [
                new AwslForNode
                {
                    iterator = "item",
                    iterable = "items",
                    children = [new AwslInterpolationNode { expression = "item" }]
                }
            ]
        };

        var result = _compiler.compile(parseResult, "app");

        Assert.Contains("listMap", result.java_script);
        Assert.Contains("getitems()", result.java_script);
    }

    [Fact]
    public void Compile_EventBinding_GeneratesAddEventListener()
    {
        var parseResult = new AwslParseResult
        {
            name = "clickable",
            methods = [new AwslMethod { name = "handleClick", parameters = "", body = "count = count + 1" }],
            properties =
            [
                new AwslProperty
                {
                    name = "count", type_name = "i32", default_value = "0", default_value_kind = AwslValueKind.number
                }
            ],
            template_nodes =
            [
                new AwslElementNode
                {
                    tag_name = "button",
                    attributes = new Dictionary<string, string> { { "on:click", "handleClick" } },
                    children = [new AwslTextNode { text = "Click" }]
                }
            ]
        };

        var result = _compiler.compile(parseResult, "app");

        Assert.Contains("addEventListener('click', handleClick)", result.java_script);
        Assert.Contains("function handleClick()", result.java_script);
        Assert.Contains("setcount(getcount() + 1)", result.java_script);
    }

    [Fact]
    public void Compile_VModel_GeneratesTwoWayBinding()
    {
        var parseResult = new AwslParseResult
        {
            name = "input",
            properties =
            [
                new AwslProperty
                {
                    name = "text", type_name = "string", default_value = "\"\"",
                    default_value_kind = AwslValueKind.@string
                }
            ],
            template_nodes =
            [
                new AwslElementNode
                {
                    tag_name = "input",
                    attributes = new Dictionary<string, string> { { "v-model", "text" } }
                }
            ]
        };

        var result = _compiler.compile(parseResult, "app");

        Assert.Contains("createEffect", result.java_script);
        Assert.Contains("addEventListener('input'", result.java_script);
        Assert.Contains("settext(", result.java_script);
    }

    [Fact]
    public void Compile_Styles_GeneratesScopedCss()
    {
        var parseResult = new AwslParseResult
        {
            name = "styled",
            styles = new Dictionary<string, string>
                { { "container", "padding: 10px" }, { "title", "font-size: 14px" } },
            template_nodes = [new AwslElementNode { tag_name = "div" }]
        };

        var result = _compiler.compile(parseResult, "app");

        Assert.Contains(".voa-styled .container { padding: 10px }", result.css);
        Assert.Contains(".voa-styled .title { font-size: 14px }", result.css);
    }

    [Fact]
    public void Compile_ComponentReference_GeneratesCreateComponent()
    {
        _compiler.register_component_names(["SearchBox"]);

        var parseResult = new AwslParseResult
        {
            name = "app",
            template_nodes =
            [
                new AwslElementNode
                {
                    tag_name = "SearchBox",
                    attributes = new Dictionary<string, string> { { "query", "searchQuery" } }
                }
            ]
        };

        var result = _compiler.compile(parseResult, "app");

        Assert.Contains("createComponent(SearchBox", result.java_script);
    }

    [Fact]
    public void Compile_MicroMethod_GeneratesFunction()
    {
        var parseResult = new AwslParseResult
        {
            name = "counter",
            properties =
            [
                new AwslProperty
                {
                    name = "count", type_name = "i32", default_value = "0", default_value_kind = AwslValueKind.number
                }
            ],
            methods =
            [
                new AwslMethod { name = "increment", parameters = "", body = "count = count + 1", is_micro = true }
            ],
            template_nodes = [new AwslElementNode { tag_name = "div" }]
        };

        var result = _compiler.compile(parseResult, "app");

        Assert.Contains("function increment()", result.java_script);
        Assert.Contains("setcount(getcount() + 1)", result.java_script);
    }

    [Fact]
    public void Compile_DecrementOperator_GeneratesSignalUpdate()
    {
        var parseResult = new AwslParseResult
        {
            name = "counter",
            properties =
            [
                new AwslProperty
                {
                    name = "count", type_name = "i32", default_value = "0", default_value_kind = AwslValueKind.number
                }
            ],
            methods = [new AwslMethod { name = "decrement", parameters = "", body = "count--" }],
            template_nodes = [new AwslElementNode { tag_name = "div" }]
        };

        var result = _compiler.compile(parseResult, "app");

        Assert.Contains("setcount(getcount() - 1)", result.java_script);
    }

    [Fact]
    public void Compile_StaticAttribute_GeneratesSetAttribute()
    {
        var parseResult = new AwslParseResult
        {
            name = "link",
            template_nodes =
            [
                new AwslElementNode
                {
                    tag_name = "a",
                    attributes = new Dictionary<string, string> { { "href", "https://example.com" } },
                    children = [new AwslTextNode { text = "Link" }]
                }
            ]
        };

        var result = _compiler.compile(parseResult, "app");

        Assert.Contains("setAttribute(", result.java_script);
        Assert.Contains("'href'", result.java_script);
        Assert.Contains("'https://example.com'", result.java_script);
    }

    [Fact]
    public void Compile_EmptyTemplate_GeneratesDivRoot()
    {
        var parseResult = new AwslParseResult
        {
            name = "empty",
            template_nodes = []
        };

        var result = _compiler.compile(parseResult, "app");

        Assert.Contains("createElement('div')", result.java_script);
    }

    [Fact]
    public void Compile_KebabCaseName_GeneratesPascalCaseFunction()
    {
        var parseResult = new AwslParseResult
        {
            name = "my-component",
            template_nodes = [new AwslElementNode { tag_name = "div" }]
        };

        var result = _compiler.compile(parseResult, "app");

        Assert.Contains("function MyComponent(props)", result.java_script);
    }

    [Fact]
    public void Compile_CamelCaseProperty_GeneratesSnakeCaseAccessor()
    {
        var parseResult = new AwslParseResult
        {
            name = "test",
            properties =
            [
                new AwslProperty
                {
                    name = "isActive", type_name = "bool", default_value = "false",
                    default_value_kind = AwslValueKind.boolean
                }
            ],
            template_nodes =
            [
                new AwslElementNode
                {
                    tag_name = "span",
                    children = [new AwslInterpolationNode { expression = "isActive" }]
                }
            ]
        };

        var result = _compiler.compile(parseResult, "app");

        Assert.Contains("getis_active()", result.java_script);
        Assert.Contains("setis_active", result.java_script);
    }
}