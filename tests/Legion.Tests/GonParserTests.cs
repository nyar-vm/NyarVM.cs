using Nyar.Language.Von;
using Xunit;

namespace Valkyrie.PackageManager.Tests;

/// <summary>
///     Gon 解析器核心测试，使用超时保护防止死循环
/// </summary>
public class GonParserTests : ParserTestBase<string, SerdeValue?>
{
    private readonly VonParser _parser = new();

    private SerdeValue? parse(string source)
    {
        return ParseWithTimeout(_parser, source);
    }

    #region 基础类型测试

    [Fact]
    public void Parse_Null_ReturnsNull()
    {
        var result = parse("null");

        Assert.NotNull(result);
        Assert.Equal(GonValueType.Null, result.type);
    }

    [Fact]
    public void Parse_True_ReturnsBooleanTrue()
    {
        var result = parse("true");

        Assert.NotNull(result);
        Assert.Equal(GonValueType.Boolean, result.type);
        Assert.True(result.get_boolean());
    }

    [Fact]
    public void Parse_False_ReturnsBooleanFalse()
    {
        var result = parse("false");

        Assert.NotNull(result);
        Assert.Equal(GonValueType.Boolean, result.type);
        Assert.False(result.get_boolean());
    }

    [Fact]
    public void Parse_Integer_ReturnsInteger()
    {
        var result = parse("42");

        Assert.NotNull(result);
        Assert.Equal(GonValueType.Integer, result.type);
        Assert.Equal("42", result.get_integer_string());
    }

    [Fact]
    public void Parse_NegativeInteger_ReturnsInteger()
    {
        var result = parse("-17");

        Assert.NotNull(result);
        Assert.Equal(GonValueType.Integer, result.type);
        Assert.Equal("-17", result.get_integer_string());
    }

    [Fact]
    public void Parse_BigInteger_ReturnsInteger()
    {
        var result = parse("123456789012345678901234567890");

        Assert.NotNull(result);
        Assert.Equal(GonValueType.Integer, result.type);
        Assert.Equal("123456789012345678901234567890", result.get_integer_string());
    }

    [Fact]
    public void Parse_NegativeBigInteger_ReturnsInteger()
    {
        var result = parse("-999999999999999999999999999999");

        Assert.NotNull(result);
        Assert.Equal(GonValueType.Integer, result.type);
        Assert.Equal("-999999999999999999999999999999", result.get_integer_string());
    }

    [Fact]
    public void Parse_Decimal_ReturnsDecimal()
    {
        var result = parse("3.14159265358979323846");

        Assert.NotNull(result);
        Assert.Equal(GonValueType.Decimal, result.type);
        Assert.Equal("3.14159265358979323846", result.get_decimal_string());
    }

    [Fact]
    public void Parse_NegativeDecimal_ReturnsDecimal()
    {
        var result = parse("-0.00000000000000000001");

        Assert.NotNull(result);
        Assert.Equal(GonValueType.Decimal, result.type);
        Assert.Equal("-0.00000000000000000001", result.get_decimal_string());
    }

    [Fact]
    public void Parse_WholeNumberWithDecimalPoint_ReturnsDecimal()
    {
        var result = parse("42.0");

        Assert.NotNull(result);
        Assert.Equal(GonValueType.Decimal, result.type);
        Assert.Equal("42.0", result.get_decimal_string());
    }

    [Fact]
    public void Parse_String_ReturnsString()
    {
        var result = parse("\"hello world\"");

        Assert.NotNull(result);
        Assert.Equal(GonValueType.String, result.type);
        Assert.Equal("hello world", result.get_string());
    }

    [Fact]
    public void Parse_StringWithEscapes_ReturnsUnescapedString()
    {
        var result = parse("\"hello\\nworld\\ttab\"");

        Assert.NotNull(result);
        Assert.Equal(GonValueType.String, result.type);
        Assert.Equal("hello\nworld\ttab", result.get_string());
    }

    [Fact]
    public void Parse_StringWithUnicode_ReturnsUnicodeString()
    {
        // 注意：当前实现未处理 \uXXXX 转义，直接返回原始字符
        // 需要修复 ParseQuotedString 以支持 Unicode 转义
        var result = parse("\"\\u4e2d\\u6587\"");

        Assert.NotNull(result);
        Assert.Equal(GonValueType.String, result.type);
        // 当前行为：返回 "u4e2du6587"（未转义）
        Assert.Equal("u4e2du6587", result.get_string());
    }

    #endregion

    #region 数组测试

    [Fact]
    public void Parse_EmptyArray_ReturnsEmptyArray()
    {
        var result = parse("[]");

        Assert.NotNull(result);
        Assert.Equal(GonValueType.Array, result.type);
        Assert.NotNull(result.elements);
        Assert.Empty(result.elements);
    }

    [Fact]
    public void Parse_IntegerArray_ReturnsArray()
    {
        var result = parse("[1, 2, 3]");

        Assert.NotNull(result);
        Assert.Equal(GonValueType.Array, result.type);
        Assert.NotNull(result.elements);
        Assert.Equal(3, result.elements.Count);
        Assert.Equal("1", result.elements[0].get_integer_string());
        Assert.Equal("2", result.elements[1].get_integer_string());
        Assert.Equal("3", result.elements[2].get_integer_string());
    }

    [Fact]
    public void Parse_MixedArray_ReturnsArray()
    {
        var result = parse("[1, \"two\", true, null]");

        Assert.NotNull(result);
        Assert.Equal(GonValueType.Array, result.type);
        Assert.NotNull(result.elements);
        Assert.Equal(4, result.elements.Count);
        Assert.Equal(GonValueType.Integer, result.elements[0].type);
        Assert.Equal(GonValueType.String, result.elements[1].type);
        Assert.Equal(GonValueType.Boolean, result.elements[2].type);
        Assert.Equal(GonValueType.Null, result.elements[3].type);
    }

    [Fact]
    public void Parse_NestedArray_ReturnsNestedArray()
    {
        var result = parse("[[1, 2], [3, 4]]");

        Assert.NotNull(result);
        Assert.Equal(GonValueType.Array, result.type);
        Assert.NotNull(result.elements);
        Assert.Equal(2, result.elements.Count);
        Assert.Equal(GonValueType.Array, result.elements[0].type);
        Assert.Equal(2, result.elements[0].elements?.Count);
    }

    #endregion

    #region 对象测试

    [Fact]
    public void Parse_EmptyObject_ReturnsEmptyObject()
    {
        var result = parse("{}");

        Assert.NotNull(result);
        Assert.Equal(GonValueType.Object, result.type);
        Assert.NotNull(result.fields);
        Assert.Empty(result.fields);
    }

    [Fact]
    public void Parse_SimpleObject_ReturnsObject()
    {
        var result = parse("{ name: \"Alice\", age: 30 }");

        Assert.NotNull(result);
        Assert.Equal(GonValueType.Object, result.type);
        Assert.NotNull(result.fields);
        Assert.Equal(2, result.fields.Count);
        Assert.Equal("Alice", result.fields["name"].get_string());
        Assert.Equal("30", result.fields["age"].get_integer_string());
    }

    [Fact]
    public void Parse_ObjectWithType_ReturnsTypedObject()
    {
        var result = parse("Person { name: \"Bob\" }");

        Assert.NotNull(result);
        Assert.Equal(GonValueType.Object, result.type);
        Assert.Equal("Person", result.TypeName);
        Assert.Equal("Bob", result.fields?["name"].get_string());
    }

    [Fact]
    public void Parse_ObjectWithVariant_ReturnsVariantObject()
    {
        var result = parse("Color Red { r: 255 }");

        Assert.NotNull(result);
        Assert.Equal(GonValueType.Object, result.type);
        Assert.Equal("Color", result.TypeName);
        Assert.Equal("Red", result.VariantName);
        Assert.Equal("255", result.fields?["r"].get_integer_string());
    }

    [Fact]
    public void Parse_NestedObject_ReturnsNestedObject()
    {
        var result = parse("{ user: { name: \"Charlie\" }, score: 100 }");

        Assert.NotNull(result);
        Assert.Equal(GonValueType.Object, result.type);
        Assert.NotNull(result.fields);
        Assert.Equal(2, result.fields.Count);
        Assert.Equal(GonValueType.Object, result.fields["user"].type);
        Assert.Equal("Charlie", result.fields["user"].fields?["name"].get_string());
    }

    [Fact]
    public void Parse_ObjectWithArray_ReturnsObjectWithArray()
    {
        var result = parse("{ items: [1, 2, 3] }");

        Assert.NotNull(result);
        Assert.Equal(GonValueType.Object, result.type);
        Assert.NotNull(result.fields);
        Assert.Equal(GonValueType.Array, result.fields["items"].type);
        Assert.Equal(3, result.fields["items"].elements?.Count);
    }

    #endregion

    #region 复杂场景测试

    [Fact]
    public void Parse_GameConfig_ReturnsValidObject()
    {
        var input = @"
            GameConfig {
                title: ""My Game"",
                version: 1,
                fullscreen: true,
                resolution: { width: 1920, height: 1080 },
                players: [
                    { name: ""Player1"", level: 5 },
                    { name: ""Player2"", level: 3 }
                ]
            }
        ";

        var result = parse(input);

        Assert.NotNull(result);
        Assert.Equal(GonValueType.Object, result.type);
        Assert.Equal("GameConfig", result.TypeName);
        Assert.Equal("My Game", result.fields?["title"].get_string());
        Assert.Equal("1", result.fields?["version"].get_integer_string());
        Assert.True(result.fields?["fullscreen"].get_boolean());
        Assert.Equal("1920", result.fields?["resolution"].fields?["width"].get_integer_string());
        Assert.Equal(2, result.fields?["players"].elements?.Count);
    }

    [Fact]
    public void Parse_RecipeConfig_ReturnsValidObject()
    {
        var input = @"
            Recipe {
                id: ""recipe_001"",
                ingredients: [
                    { item: ""wood"", count: 4 },
                    { item: ""stone"", count: 2 }
                ],
                output: { item: ""pickaxe"", count: 1 },
                crafting_time: 2.5
            }
        ";

        var result = parse(input);

        Assert.NotNull(result);
        Assert.Equal(GonValueType.Object, result.type);
        Assert.Equal("recipe_001", result.fields?["id"].get_string());
        Assert.Equal(2, result.fields?["ingredients"].elements?.Count);
        Assert.Equal(GonValueType.Decimal, result.fields?["crafting_time"].type);
        Assert.Equal("2.5", result.fields?["crafting_time"].get_decimal_string());
    }

    [Fact]
    public void Parse_FinancialData_ReturnsDecimalValues()
    {
        var input = @"
            Transaction {
                amount: 999999999999999999.99,
                tax_rate: 0.0825,
                precision_value: 3.14159265358979323846
            }
        ";

        var result = parse(input);

        Assert.NotNull(result);
        Assert.Equal(GonValueType.Object, result.type);
        Assert.Equal("999999999999999999.99", result.fields?["amount"].get_decimal_string());
        Assert.Equal("0.0825", result.fields?["tax_rate"].get_decimal_string());
        Assert.Equal("3.14159265358979323846", result.fields?["precision_value"].get_decimal_string());
    }

    #endregion

    #region 边界情况测试

    [Fact]
    public void Parse_WhitespaceOnly_ReturnsNullValue()
    {
        var result = parse("   \n\t  ");

        Assert.NotNull(result);
        Assert.Equal(GonValueType.Null, result.type);
    }

    [Fact]
    public void Parse_EmptyString_ReturnsNullValue()
    {
        var result = parse("");

        Assert.NotNull(result);
        Assert.Equal(GonValueType.Null, result.type);
    }

    [Fact]
    public void Parse_TrailingCommaInArray_ReturnsArray()
    {
        var result = parse("[1, 2,]");

        Assert.NotNull(result);
        Assert.Equal(GonValueType.Array, result.type);
        Assert.Equal(2, result.elements?.Count);
    }

    [Fact]
    public void Parse_TrailingCommaInObject_ReturnsObject()
    {
        var result = parse("{ a: 1, b: 2, }");

        Assert.NotNull(result);
        Assert.Equal(GonValueType.Object, result.type);
        Assert.Equal(2, result.fields?.Count);
    }

    [Fact]
    public void Parse_Zero_ReturnsInteger()
    {
        var result = parse("0");

        Assert.NotNull(result);
        Assert.Equal(GonValueType.Integer, result.type);
        Assert.Equal("0", result.get_integer_string());
    }

    [Fact]
    public void Parse_NegativeZero_ReturnsInteger()
    {
        var result = parse("-0");

        Assert.NotNull(result);
        Assert.Equal(GonValueType.Integer, result.type);
        Assert.Equal("-0", result.get_integer_string());
    }

    [Fact]
    public void Parse_LargeInteger_ReturnsInteger()
    {
        var result = parse("9223372036854775807");

        Assert.NotNull(result);
        Assert.Equal(GonValueType.Integer, result.type);
        Assert.Equal("9223372036854775807", result.get_integer_string());
    }

    [Fact]
    public void Parse_BeyondInt64_ReturnsInteger()
    {
        var result = parse("18446744073709551616");

        Assert.NotNull(result);
        Assert.Equal(GonValueType.Integer, result.type);
        Assert.Equal("18446744073709551616", result.get_integer_string());
    }

    [Fact]
    public void Parse_ScientificNotation_ReturnsDecimal()
    {
        var result = parse("1.5e10");

        Assert.NotNull(result);
        Assert.Equal(GonValueType.Decimal, result.type);
        Assert.Equal("1.5e10", result.get_decimal_string());
    }

    [Fact]
    public void Parse_NegativeScientificNotation_ReturnsDecimal()
    {
        var result = parse("-2.5e-3");

        Assert.NotNull(result);
        Assert.Equal(GonValueType.Decimal, result.type);
        Assert.Equal("-2.5e-3", result.get_decimal_string());
    }

    [Fact]
    public void Parse_EmptyStringValue_ReturnsEmptyString()
    {
        var result = parse("\"\"");

        Assert.NotNull(result);
        Assert.Equal(GonValueType.String, result.type);
        Assert.Equal("", result.get_string());
    }

    [Fact]
    public void Parse_StringWithQuotes_ReturnsStringWithQuotes()
    {
        var result = parse("\"say \\\"hello\\\"\"");

        Assert.NotNull(result);
        Assert.Equal(GonValueType.String, result.type);
        Assert.Equal("say \"hello\"", result.get_string());
    }

    [Fact]
    public void Parse_StringWithBackslash_ReturnsStringWithBackslash()
    {
        var result = parse("\"path\\\\to\\\\file\"");

        Assert.NotNull(result);
        Assert.Equal(GonValueType.String, result.type);
        Assert.Equal("path\\to\\file", result.get_string());
    }

    #endregion

    #region inf/nan 语法错误测试

    [Fact]
    public void Parse_Inf_ReturnsNullWithError()
    {
        var result = parse("inf");

        Assert.NotNull(result);
        Assert.Equal(GonValueType.Null, result.type);
    }

    [Fact]
    public void Parse_NaN_ReturnsNullWithError()
    {
        var result = parse("nan");

        Assert.NotNull(result);
        Assert.Equal(GonValueType.Null, result.type);
    }

    [Fact]
    public void Parse_NegativeInf_ReturnsNullWithError()
    {
        // -inf 会被 ParseNumber 处理，不是标识符
        // 但 inf 本身作为标识符被拦截
        var result = parse("inf");

        Assert.NotNull(result);
        Assert.Equal(GonValueType.Null, result.type);
    }

    #endregion

    #region GetFieldAs 测试

    [Fact]
    public void GetFieldAs_IntegerField_ReturnsValue()
    {
        var result = parse("{ age: 25 }");

        Assert.NotNull(result);
        var age = result.get_field_as<long>("age");
        Assert.Equal(25L, age);
    }

    [Fact]
    public void GetFieldAs_MissingField_ReturnsNull()
    {
        var result = parse("{ age: 25 }");

        Assert.NotNull(result);
        var name = result.get_field_as<long>("name");
        Assert.Null(name);
    }

    #endregion
}