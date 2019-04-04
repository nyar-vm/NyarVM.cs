using Xunit;

namespace Hermes.Tests.Conformance;

public class TypeCheckerConformanceTests
{
    #region 辅助方法

    private static TypeRegistry CreateTestRegistry()
    {
        var registry = new TypeRegistry();
        registry.RegisterClass("User",
        [
            new("name", SchemaType.Utf8),
            new("age", SchemaType.Int32),
            new("email", SchemaType.Utf8, isOptional: true)
        ]);
        registry.RegisterEnum("Status", ["Active", "Inactive", "Pending"]);
        registry.RegisterFlags("Permission", ["Read", "Write", "Execute"]);
        registry.RegisterUnion("Shape", ["Circle", "Rectangle"]);
        return registry;
    }

    #endregion

    #region Unknown 类型

    [Fact]
    public void TypeCheck_Unknown_AlwaysMatches()
    {
        var checker = new TypeChecker(new TypeRegistry());
        var result = checker.Validate(HermesValue.FromInt32(42), SchemaType.Unknown);
        Assert.True(result.IsValid);
    }

    #endregion

    #region 原始类型匹配

    [Fact]
    public void TypeCheck_Int8_Matches_Int8()
    {
        var checker = new TypeChecker(new TypeRegistry());
        var result = checker.Validate(HermesValue.FromInt8(42), SchemaType.Int8);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void TypeCheck_Int32_Matches_Int32()
    {
        var checker = new TypeChecker(new TypeRegistry());
        var result = checker.Validate(HermesValue.FromInt32(42), SchemaType.Int32);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void TypeCheck_Bool_Matches_Bool()
    {
        var checker = new TypeChecker(new TypeRegistry());
        var result = checker.Validate(HermesValue.FromBool(true), SchemaType.Bool);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void TypeCheck_Utf8_Matches_Utf8()
    {
        var checker = new TypeChecker(new TypeRegistry());
        var result = checker.Validate(HermesValue.FromUtf8("hello"), SchemaType.Utf8);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void TypeCheck_Float64_Matches_Float64()
    {
        var checker = new TypeChecker(new TypeRegistry());
        var result = checker.Validate(HermesValue.FromFloat64(3.14), SchemaType.Float64);
        Assert.True(result.IsValid);
    }

    #endregion

    #region 类型宽化规则

    [Fact]
    public void TypeCheck_Int8_Widens_To_Int32()
    {
        var checker = new TypeChecker(new TypeRegistry());
        var result = checker.Validate(HermesValue.FromInt8(42), SchemaType.Int32);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void TypeCheck_Int16_Widens_To_Int64()
    {
        var checker = new TypeChecker(new TypeRegistry());
        var result = checker.Validate(HermesValue.FromInt16(1000), SchemaType.Int64);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void TypeCheck_UInt8_Widens_To_UInt32()
    {
        var checker = new TypeChecker(new TypeRegistry());
        var result = checker.Validate(HermesValue.FromUInt8(200), SchemaType.UInt32);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void TypeCheck_Float32_Widens_To_Float64()
    {
        var checker = new TypeChecker(new TypeRegistry());
        var result = checker.Validate(HermesValue.FromFloat32(3.14f), SchemaType.Float64);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void TypeCheck_Utf8_Widens_To_Utf16()
    {
        var checker = new TypeChecker(new TypeRegistry());
        var result = checker.Validate(HermesValue.FromUtf8("hello"), SchemaType.Utf16);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void TypeCheck_Utf16_Widens_To_Utf8()
    {
        var checker = new TypeChecker(new TypeRegistry());
        var result = checker.Validate(HermesValue.FromUtf16("hello"), SchemaType.Utf8);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void TypeCheck_AnyType_Matches_Option()
    {
        var checker = new TypeChecker(new TypeRegistry());
        var result = checker.Validate(HermesValue.FromInt32(42), SchemaType.Option);
        Assert.True(result.IsValid);
    }

    #endregion

    #region 类型不匹配

    [Fact]
    public void TypeCheck_Bool_DoesNotMatch_Int32()
    {
        var checker = new TypeChecker(new TypeRegistry());
        var result = checker.Validate(HermesValue.FromBool(true), SchemaType.Int32);
        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
    }

    [Fact]
    public void TypeCheck_Utf8_DoesNotMatch_Int64()
    {
        var checker = new TypeChecker(new TypeRegistry());
        var result = checker.Validate(HermesValue.FromUtf8("hello"), SchemaType.Int64);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void TypeCheck_Int32_DoesNotMatch_Bool()
    {
        var checker = new TypeChecker(new TypeRegistry());
        var result = checker.Validate(HermesValue.FromInt32(1), SchemaType.Bool);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void TypeCheck_Float64_DoesNotMatch_Int32()
    {
        var checker = new TypeChecker(new TypeRegistry());
        var result = checker.Validate(HermesValue.FromFloat64(3.14), SchemaType.Int32);
        Assert.False(result.IsValid);
    }

    #endregion

    #region Schema 类型验证

    [Fact]
    public void TypeCheck_Class_Valid()
    {
        var registry = CreateTestRegistry();
        var checker = new TypeChecker(registry);
        var value = HermesValue.FromClass(new Dictionary<string, HermesValue>
        {
            ["name"] = HermesValue.FromUtf8("Alice"),
            ["age"] = HermesValue.FromInt32(30)
        });
        var result = checker.Validate(value, "User");
        Assert.True(result.IsValid);
    }

    [Fact]
    public void TypeCheck_Class_WrongType()
    {
        var registry = CreateTestRegistry();
        var checker = new TypeChecker(registry);
        var value = HermesValue.FromUtf8("not a class");
        var result = checker.Validate(value, "User");
        Assert.False(result.IsValid);
    }

    [Fact]
    public void TypeCheck_Class_UnknownTypeName()
    {
        var registry = CreateTestRegistry();
        var checker = new TypeChecker(registry);
        var value = HermesValue.FromClass(new Dictionary<string, HermesValue>());
        var result = checker.Validate(value, "NonExistent");
        Assert.False(result.IsValid);
    }

    [Fact]
    public void TypeCheck_Enum_Valid()
    {
        var registry = CreateTestRegistry();
        var checker = new TypeChecker(registry);
        var value = HermesValue.FromEnum("Active");
        var result = checker.Validate(value, "Status");
        Assert.True(result.IsValid);
    }

    [Fact]
    public void TypeCheck_Flags_Valid()
    {
        var registry = CreateTestRegistry();
        var checker = new TypeChecker(registry);
        var value = HermesValue.FromFlags(5L);
        var result = checker.Validate(value, "Permission");
        Assert.True(result.IsValid);
    }

    [Fact]
    public void TypeCheck_Union_Valid()
    {
        var registry = CreateTestRegistry();
        var checker = new TypeChecker(registry);
        var union = new HermesUnionValue
        {
            VariantName = "Circle",
            Fields = new Dictionary<string, HermesValue>()
        };
        var value = HermesValue.FromUnion(union);
        var result = checker.Validate(value, "Shape");
        Assert.True(result.IsValid);
    }

    #endregion

    #region 字段级验证

    [Fact]
    public void TypeCheck_Field_Valid()
    {
        var registry = CreateTestRegistry();
        var checker = new TypeChecker(registry);
        var result = checker.ValidateField(HermesValue.FromUtf8("Alice"), "User", "name");
        Assert.True(result.IsValid);
    }

    [Fact]
    public void TypeCheck_Field_OptionalNull()
    {
        var registry = CreateTestRegistry();
        var checker = new TypeChecker(registry);
        var result = checker.ValidateField(HermesValue.Null, "User", "email");
        Assert.True(result.IsValid);
    }

    [Fact]
    public void TypeCheck_Field_RequiredNull_Invalid()
    {
        var registry = CreateTestRegistry();
        var checker = new TypeChecker(registry);
        var result = checker.ValidateField(HermesValue.Null, "User", "name");
        Assert.False(result.IsValid);
    }

    [Fact]
    public void TypeCheck_Field_TypeMismatch()
    {
        var registry = CreateTestRegistry();
        var checker = new TypeChecker(registry);
        var result = checker.ValidateField(HermesValue.FromBool(true), "User", "age");
        Assert.False(result.IsValid);
    }

    [Fact]
    public void TypeCheck_Field_UnknownField()
    {
        var registry = CreateTestRegistry();
        var checker = new TypeChecker(registry);
        var result = checker.ValidateField(HermesValue.FromInt32(1), "User", "nonexistent");
        Assert.False(result.IsValid);
    }

    #endregion

    #region 跨语言一致性规则验证

    [Fact]
    public void TypeCheck_IntegerWidening_AllCombinations()
    {
        var checker = new TypeChecker(new TypeRegistry());
        var integerTypes = new[]
        {
            SchemaType.Int8, SchemaType.Int16, SchemaType.Int32, SchemaType.Int64,
            SchemaType.UInt8, SchemaType.UInt16, SchemaType.UInt32, SchemaType.UInt64
        };

        foreach (var actualType in integerTypes)
        foreach (var expectedType in integerTypes)
        {
            var value = CreateZeroIntegerValue(actualType);
            var result = checker.Validate(value, expectedType);
            Assert.True(result.IsValid,
                $"整数宽化应允许 {actualType} → {expectedType}");
        }
    }

    [Fact]
    public void TypeCheck_FloatWidening_AllCombinations()
    {
        var checker = new TypeChecker(new TypeRegistry());
        var floatValue = HermesValue.FromFloat32(0.0f);

        var result32 = checker.Validate(floatValue, SchemaType.Float32);
        var result64 = checker.Validate(floatValue, SchemaType.Float64);
        Assert.True(result32.IsValid, "Float32 应匹配 Float32");
        Assert.True(result64.IsValid, "Float32 应宽化到 Float64");
    }

    [Fact]
    public void TypeCheck_IntegerDoesNotWidenToFloat()
    {
        var checker = new TypeChecker(new TypeRegistry());
        var intValue = HermesValue.FromInt32(42);
        var result = checker.Validate(intValue, SchemaType.Float64);
        Assert.False(result.IsValid, "整数不应宽化到浮点");
    }

    [Fact]
    public void TypeCheck_FloatDoesNotWidenToInteger()
    {
        var checker = new TypeChecker(new TypeRegistry());
        var floatValue = HermesValue.FromFloat64(3.14);
        var result = checker.Validate(floatValue, SchemaType.Int64);
        Assert.False(result.IsValid, "浮点不应宽化到整数");
    }

    private static HermesValue CreateZeroIntegerValue(SchemaType type)
    {
        return type switch
        {
            SchemaType.Int8 => HermesValue.FromInt8(0),
            SchemaType.Int16 => HermesValue.FromInt16(0),
            SchemaType.Int32 => HermesValue.FromInt32(0),
            SchemaType.Int64 => HermesValue.FromInt64(0),
            SchemaType.UInt8 => HermesValue.FromUInt8(0),
            SchemaType.UInt16 => HermesValue.FromUInt16(0),
            SchemaType.UInt32 => HermesValue.FromUInt32(0),
            SchemaType.UInt64 => HermesValue.FromUInt64(0),
            _ => throw new ArgumentException($"不是整数类型：{type}")
        };
    }

    #endregion
}