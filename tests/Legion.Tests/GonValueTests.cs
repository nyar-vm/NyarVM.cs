using Xunit;

namespace Valkyrie.PackageManager.Tests;

/// <summary>
///     SerdeValue 值对象测试
/// </summary>
public class GonValueTests
{
    #region 工厂方法测试

    [Fact]
    public void Null_CreatesNullValue()
    {
        var value = SerdeValue.@null();

        Assert.Equal(GonValueType.Null, value.type);
        Assert.Null(value.RawValue);
    }

    [Fact]
    public void Boolean_CreatesBooleanValue()
    {
        var value = SerdeValue.boolean(true);

        Assert.Equal(GonValueType.Boolean, value.type);
        Assert.True(value.get_boolean());
    }

    [Fact]
    public void Integer_CreatesIntegerValue()
    {
        var value = SerdeValue.integer("42");

        Assert.Equal(GonValueType.Integer, value.type);
        Assert.Equal("42", value.get_integer_string());
    }

    [Fact]
    public void Decimal_CreatesDecimalValue()
    {
        var value = SerdeValue.@decimal("3.14159265358979323846");

        Assert.Equal(GonValueType.Decimal, value.type);
        Assert.Equal("3.14159265358979323846", value.get_decimal_string());
    }

    [Fact]
    public void String_CreatesStringValue()
    {
        var value = SerdeValue.@string("hello");

        Assert.Equal(GonValueType.String, value.type);
        Assert.Equal("hello", value.get_string());
    }

    [Fact]
    public void Object_CreatesObjectValue()
    {
        var fields = new Dictionary<string, SerdeValue>
        {
            ["name"] = SerdeValue.@string("test")
        };
        var value = SerdeValue.@object("Person", null, fields);

        Assert.Equal(GonValueType.Object, value.type);
        Assert.Equal("Person", value.TypeName);
        Assert.Equal("test", value.fields?["name"].get_string());
    }

    [Fact]
    public void Array_CreatesArrayValue()
    {
        var elements = new List<SerdeValue>
        {
            SerdeValue.integer("1"),
            SerdeValue.integer("2")
        };
        var value = SerdeValue.array(elements);

        Assert.Equal(GonValueType.Array, value.type);
        Assert.Equal(2, value.elements?.Count);
    }

    #endregion

    #region ToString 测试

    [Fact]
    public void ToString_Null_ReturnsNull()
    {
        var value = SerdeValue.@null();

        Assert.Equal("null", value.ToString());
    }

    [Fact]
    public void ToString_True_ReturnsTrue()
    {
        var value = SerdeValue.boolean(true);

        Assert.Equal("true", value.ToString());
    }

    [Fact]
    public void ToString_False_ReturnsFalse()
    {
        var value = SerdeValue.boolean(false);

        Assert.Equal("false", value.ToString());
    }

    [Fact]
    public void ToString_Integer_ReturnsString()
    {
        var value = SerdeValue.integer("42");

        Assert.Equal("42", value.ToString());
    }

    [Fact]
    public void ToString_Decimal_ReturnsString()
    {
        var value = SerdeValue.@decimal("3.14159265358979323846");

        Assert.Equal("3.14159265358979323846", value.ToString());
    }

    [Fact]
    public void ToString_String_ReturnsQuotedString()
    {
        var value = SerdeValue.@string("hello");

        Assert.Equal("\"hello\"", value.ToString());
    }

    [Fact]
    public void ToString_Object_ReturnsFormattedObject()
    {
        var fields = new Dictionary<string, SerdeValue>
        {
            ["name"] = SerdeValue.@string("Alice")
        };
        var value = SerdeValue.@object(null, null, fields);

        Assert.Equal("{ name: \"Alice\" }", value.ToString());
    }

    [Fact]
    public void ToString_TypedObject_ReturnsTypedObject()
    {
        var fields = new Dictionary<string, SerdeValue>
        {
            ["x"] = SerdeValue.integer("10")
        };
        var value = SerdeValue.@object("Point", null, fields);

        Assert.Equal("Point { x: 10 }", value.ToString());
    }

    [Fact]
    public void ToString_VariantObject_ReturnsVariantObject()
    {
        var fields = new Dictionary<string, SerdeValue>
        {
            ["r"] = SerdeValue.integer("255")
        };
        var value = SerdeValue.@object("Color", "Red", fields);

        Assert.Equal("Color Red { r: 255 }", value.ToString());
    }

    [Fact]
    public void ToString_Array_ReturnsFormattedArray()
    {
        var elements = new List<SerdeValue>
        {
            SerdeValue.integer("1"),
            SerdeValue.integer("2")
        };
        var value = SerdeValue.array(elements);

        Assert.Equal("[ 1, 2 ]", value.ToString());
    }

    [Fact]
    public void ToString_EmptyArray_ReturnsEmptyBrackets()
    {
        var value = SerdeValue.array(new List<SerdeValue>());

        Assert.Equal("[  ]", value.ToString());
    }

    #endregion

    #region GetField 测试

    [Fact]
    public void GetField_ExistingField_ReturnsValue()
    {
        var fields = new Dictionary<string, SerdeValue>
        {
            ["name"] = SerdeValue.@string("Bob")
        };
        var value = SerdeValue.@object(null, null, fields);

        var field = value.get_field("name");

        Assert.NotNull(field);
        Assert.Equal("Bob", field.get_string());
    }

    [Fact]
    public void GetField_MissingField_ReturnsNull()
    {
        var fields = new Dictionary<string, SerdeValue>
        {
            ["name"] = SerdeValue.@string("Bob")
        };
        var value = SerdeValue.@object(null, null, fields);

        var field = value.get_field("age");

        Assert.Null(field);
    }

    [Fact]
    public void GetField_NullFields_ReturnsNull()
    {
        var value = SerdeValue.@object(null, null, new Dictionary<string, SerdeValue>());

        var field = value.get_field("anything");

        Assert.Null(field);
    }

    #endregion

    #region 类型安全获取测试

    [Fact]
    public void GetBoolean_OnNonBoolean_ReturnsFalse()
    {
        var value = SerdeValue.integer("1");

        Assert.False(value.get_boolean());
    }

    [Fact]
    public void GetIntegerString_OnNonInteger_ReturnsNull()
    {
        var value = SerdeValue.@string("42");

        Assert.Null(value.get_integer_string());
    }

    [Fact]
    public void GetDecimalString_OnNonDecimal_ReturnsNull()
    {
        var value = SerdeValue.@string("3.14");

        Assert.Null(value.get_decimal_string());
    }

    [Fact]
    public void GetString_OnNonString_ReturnsNull()
    {
        var value = SerdeValue.integer("42");

        Assert.Null(value.get_string());
    }

    #endregion

    #region 大整数测试

    [Fact]
    public void Integer_BigInteger_StoresCorrectly()
    {
        var bigValue = "1234567890123456789012345678901234567890";
        var value = SerdeValue.integer(bigValue);

        Assert.Equal(GonValueType.Integer, value.type);
        Assert.Equal(bigValue, value.get_integer_string());
        Assert.Equal(bigValue, value.ToString());
    }

    [Fact]
    public void Integer_NegativeBigInteger_StoresCorrectly()
    {
        var bigValue = "-999999999999999999999999999999999999999";
        var value = SerdeValue.integer(bigValue);

        Assert.Equal(GonValueType.Integer, value.type);
        Assert.Equal(bigValue, value.get_integer_string());
    }

    #endregion

    #region 大 Decimal 测试

    [Fact]
    public void Decimal_BigDecimal_StoresCorrectly()
    {
        var bigDecimal = "3.14159265358979323846264338327950288419716939937510";
        var value = SerdeValue.@decimal(bigDecimal);

        Assert.Equal(GonValueType.Decimal, value.type);
        Assert.Equal(bigDecimal, value.get_decimal_string());
        Assert.Equal(bigDecimal, value.ToString());
    }

    [Fact]
    public void Decimal_NegativeDecimal_StoresCorrectly()
    {
        var value = SerdeValue.@decimal("-0.00000000000000000000000000000000000001");

        Assert.Equal(GonValueType.Decimal, value.type);
        Assert.Equal("-0.00000000000000000000000000000000000001", value.get_decimal_string());
    }

    #endregion

    #region 无效数字序列化测试

    [Fact]
    public void ToString_EmptyInteger_ReturnsNull()
    {
        var value = SerdeValue.integer("");

        Assert.Equal("null", value.ToString());
    }

    [Fact]
    public void ToString_OnlyMinusInteger_ReturnsNull()
    {
        var value = SerdeValue.integer("-");

        Assert.Equal("null", value.ToString());
    }

    [Fact]
    public void ToString_EmptyDecimal_ReturnsNull()
    {
        var value = SerdeValue.@decimal("");

        Assert.Equal("null", value.ToString());
    }

    [Fact]
    public void ToString_OnlyMinusDecimal_ReturnsNull()
    {
        var value = SerdeValue.@decimal("-");

        Assert.Equal("null", value.ToString());
    }

    [Fact]
    public void ToString_OnlyDotDecimal_ReturnsNull()
    {
        var value = SerdeValue.@decimal(".");

        Assert.Equal("null", value.ToString());
    }

    [Fact]
    public void ToString_MinusDotDecimal_ReturnsNull()
    {
        var value = SerdeValue.@decimal("-.");

        Assert.Equal("null", value.ToString());
    }

    #endregion
}