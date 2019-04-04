using Xunit;

namespace Hermes.Tests.Conformance;

public class JsonSerializationConformanceTests
{
    #region 辅助方法

    private static readonly TypeRegistry EmptyRegistry = new();

    private static string Serialize(HermesValue value)
    {
        var serializer = new JsonSerializer(EmptyRegistry);
        return serializer.Serialize(value);
    }

    private static HermesValue Deserialize(string json)
    {
        var serializer = new JsonSerializer(EmptyRegistry);
        return serializer.Deserialize(json);
    }

    #endregion

    #region JSON 无 Schema 反序列化类型降级规则

    [Fact]
    public void JsonNoSchema_DowngradeRule_IntegerBecomesInt64()
    {
        var json = Serialize(HermesValue.FromInt8(42));
        var restored = Deserialize(json);
        Assert.Equal(SchemaType.Int64, restored.type);
        Assert.Equal(42L, restored.AsInt64());
    }

    [Fact]
    public void JsonNoSchema_DowngradeRule_FloatBecomesFloat64()
    {
        var json = Serialize(HermesValue.FromFloat32(3.14f));
        var restored = Deserialize(json);
        Assert.Equal(SchemaType.Float64, restored.type);
        Assert.Equal(3.14f, restored.AsFloat64(), 0.001);
    }

    [Fact]
    public void JsonNoSchema_DowngradeRule_ClassBecomesDict()
    {
        var fields = new Dictionary<string, HermesValue> { ["x"] = HermesValue.FromInt32(1) };
        var json = Serialize(HermesValue.FromClass(fields));
        var restored = Deserialize(json);
        Assert.Equal(SchemaType.Dict, restored.type);
    }

    [Fact]
    public void JsonNoSchema_DowngradeRule_RecordBecomesDict()
    {
        var record = new Dictionary<string, HermesValue> { ["x"] = HermesValue.FromInt32(1) };
        var json = Serialize(HermesValue.FromRecord(record));
        var restored = Deserialize(json);
        Assert.Equal(SchemaType.Dict, restored.type);
    }

    [Fact]
    public void JsonNoSchema_DowngradeRule_FlagsBecomesInt64()
    {
        var json = Serialize(HermesValue.FromFlags(5L));
        var restored = Deserialize(json);
        Assert.Equal(SchemaType.Int64, restored.type);
        Assert.Equal(5L, restored.AsInt64());
    }

    [Fact]
    public void JsonNoSchema_DowngradeRule_OptionSomeBecomesInnerType()
    {
        var json = Serialize(HermesValue.FromOption(HermesValue.FromInt32(42)));
        var restored = Deserialize(json);
        Assert.Equal(SchemaType.Int64, restored.type);
        Assert.Equal(42L, restored.AsInt64());
    }

    [Fact]
    public void JsonNoSchema_DowngradeRule_UInt64LargeBecomesFloat64()
    {
        var json = Serialize(HermesValue.FromUInt64(ulong.MaxValue));
        var restored = Deserialize(json);
        Assert.Equal(SchemaType.Float64, restored.type);
    }

    #endregion

    #region JSON 无 Schema 保留精度的类型

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void JsonNoSchema_Bool_Preserved(bool expected)
    {
        var original = HermesValue.FromBool(expected);
        var json = Serialize(original);
        var restored = Deserialize(json);
        Assert.Equal(SchemaType.Bool, restored.type);
        Assert.Equal(expected, restored.AsBool());
    }

    [Theory]
    [InlineData("hello")]
    [InlineData("")]
    [InlineData("你好")]
    public void JsonNoSchema_Utf8_Preserved(string expected)
    {
        var original = HermesValue.FromUtf8(expected);
        var json = Serialize(original);
        var restored = Deserialize(json);
        Assert.Equal(SchemaType.Utf8, restored.type);
        Assert.Equal(expected, restored.AsUtf8());
    }

    [Fact]
    public void JsonNoSchema_Unit_BecomesNull()
    {
        var original = HermesValue.FromUnit();
        var json = Serialize(original);
        Assert.Equal("null", json);
        var restored = Deserialize(json);
        Assert.True(restored.IsNull);
    }

    [Fact]
    public void JsonNoSchema_OptionNone_BecomesNull()
    {
        var original = HermesValue.Null;
        var json = Serialize(original);
        Assert.Equal("null", json);
        var restored = Deserialize(json);
        Assert.True(restored.IsNull);
    }

    [Fact]
    public void JsonNoSchema_List_Preserved()
    {
        var list = new List<HermesValue>
        {
            HermesValue.FromInt32(1),
            HermesValue.FromInt32(2)
        };
        var original = HermesValue.FromList(list);
        var json = Serialize(original);
        var restored = Deserialize(json);
        Assert.Equal(SchemaType.List, restored.type);
        var restoredList = (IList<HermesValue>)restored.AsList();
        Assert.Equal(2, restoredList.Count);
    }

    [Fact]
    public void JsonNoSchema_Dict_Preserved()
    {
        var dict = new Dictionary<string, HermesValue>
        {
            ["key"] = HermesValue.FromUtf8("value")
        };
        var original = HermesValue.FromDict(dict);
        var json = Serialize(original);
        var restored = Deserialize(json);
        Assert.Equal(SchemaType.Dict, restored.type);
        var restoredDict = (IDictionary<string, HermesValue>)restored.AsDict();
        Assert.Equal("value", restoredDict["key"].AsUtf8());
    }

    [Fact]
    public void JsonNoSchema_Result_Preserved()
    {
        var result = new HermesResultValue { IsOk = true, Value = HermesValue.FromInt32(42) };
        var original = HermesValue.FromResult(result);
        var json = Serialize(original);
        var restored = Deserialize(json);
        Assert.Equal(SchemaType.Result, restored.type);
        var restoredResult = (HermesResultValue)restored.AsResult();
        Assert.True(restoredResult.IsOk);
    }

    [Fact]
    public void JsonNoSchema_Union_Preserved()
    {
        var union = new HermesUnionValue
        {
            VariantName = "Circle",
            Fields = new Dictionary<string, HermesValue> { ["radius"] = HermesValue.FromInt32(10) }
        };
        var original = HermesValue.FromUnion(union);
        var json = Serialize(original);
        var restored = Deserialize(json);
        Assert.Equal(SchemaType.Union, restored.type);
        var restoredUnion = (HermesUnionValue)restored.AsUnion();
        Assert.Equal("Circle", restoredUnion.VariantName);
    }

    [Fact]
    public void JsonNoSchema_Enum_ByName_BecomesUtf8()
    {
        var original = HermesValue.FromEnum("Active");
        var json = Serialize(original);
        var restored = Deserialize(json);
        Assert.Equal(SchemaType.Utf8, restored.type);
        Assert.Equal("Active", restored.AsUtf8());
    }

    #endregion

    #region JSON 格式一致性

    [Fact]
    public void JsonFormat_Result_Ok_HasOkField()
    {
        var result = new HermesResultValue { IsOk = true, Value = HermesValue.FromInt32(42) };
        var original = HermesValue.FromResult(result);
        var json = Serialize(original);
        Assert.Contains("\"ok\":true", json);
        Assert.Contains("\"value\"", json);
    }

    [Fact]
    public void JsonFormat_Result_Err_HasOkField()
    {
        var result = new HermesResultValue { IsOk = false, Error = HermesValue.FromUtf8("fail") };
        var original = HermesValue.FromResult(result);
        var json = Serialize(original);
        Assert.Contains("\"ok\":false", json);
        Assert.Contains("\"error\"", json);
    }

    [Fact]
    public void JsonFormat_Union_HasVariantField()
    {
        var union = new HermesUnionValue
        {
            VariantName = "Circle",
            Fields = new Dictionary<string, HermesValue>()
        };
        var original = HermesValue.FromUnion(union);
        var json = Serialize(original);
        Assert.Contains("\"variant\":\"Circle\"", json);
    }

    [Fact]
    public void JsonFormat_Unit_IsNull()
    {
        var original = HermesValue.FromUnit();
        var json = Serialize(original);
        Assert.Equal("null", json);
    }

    [Fact]
    public void JsonFormat_Option_None_IsNull()
    {
        var original = HermesValue.Null;
        var json = Serialize(original);
        Assert.Equal("null", json);
    }

    #endregion

    #region Schema 感知 JSON Round-Trip

    [Fact]
    public void JsonSchemaAware_Class_RestoresExactTypes()
    {
        var registry = new TypeRegistry();
        registry.RegisterClass("User",
        [
            new("name", SchemaType.Utf8),
            new("age", SchemaType.Int32),
            new("score", SchemaType.Float64),
            new("active", SchemaType.Bool),
            new("email", SchemaType.Utf8, isOptional: true)
        ]);

        var serializer = new JsonSerializer(registry);
        var fields = new Dictionary<string, HermesValue>
        {
            ["name"] = HermesValue.FromUtf8("Bob"),
            ["age"] = HermesValue.FromInt32(25),
            ["score"] = HermesValue.FromFloat64(95.5),
            ["active"] = HermesValue.FromBool(true)
        };
        var original = HermesValue.FromClass(fields);
        var json = serializer.Serialize(original, "User");
        var restored = serializer.Deserialize(json, "User");

        var restoredFields = (IDictionary<string, HermesValue>)restored.AsClass();
        Assert.Equal("Bob", restoredFields["name"].AsUtf8());
        Assert.Equal(25, restoredFields["age"].AsInt32());
        Assert.Equal(95.5, restoredFields["score"].AsFloat64());
        Assert.True(restoredFields["active"].AsBool());
    }

    [Fact]
    public void JsonSchemaAware_Enum_RestoresByName()
    {
        var registry = new TypeRegistry();
        registry.RegisterEnum("Status", ["Active", "Inactive", "Pending"]);

        var serializer = new JsonSerializer(registry);
        var original = HermesValue.FromEnum("Active");
        var json = serializer.Serialize(original, "Status");
        Assert.Contains("Active", json);
        var restored = serializer.Deserialize(json, "Status");
        Assert.Equal("Active", restored.AsEnum());
    }

    [Fact]
    public void JsonSchemaAware_Flags_RestoresBitmask()
    {
        var registry = new TypeRegistry();
        registry.RegisterFlags("Permission", ["Read", "Write", "Execute"]);

        var serializer = new JsonSerializer(registry);
        var original = HermesValue.FromFlags(5L);
        var json = serializer.Serialize(original, "Permission");
        var restored = serializer.Deserialize(json, "Permission");
        Assert.Equal(SchemaType.Flags, restored.type);
        Assert.Equal(5L, restored.AsFlags());
    }

    [Fact]
    public void JsonSchemaAware_Union_RestoresVariant()
    {
        var registry = new TypeRegistry();
        registry.RegisterUnion("Shape", ["Circle", "Rectangle"]);

        var serializer = new JsonSerializer(registry);
        var union = new HermesUnionValue
        {
            VariantName = "Circle",
            Fields = new Dictionary<string, HermesValue> { ["radius"] = HermesValue.FromInt32(10) }
        };
        var original = HermesValue.FromUnion(union);
        var json = serializer.Serialize(original, "Shape");
        var restored = serializer.Deserialize(json, "Shape");

        var restoredUnion = (HermesUnionValue)restored.AsUnion();
        Assert.Equal("Circle", restoredUnion.VariantName);
    }

    #endregion

    #region 跨语言 JSON 格式契约

    [Fact]
    public void JsonContract_Int64_SerializedAsNumber()
    {
        var original = HermesValue.FromInt64(42);
        var json = Serialize(original);
        Assert.Equal("42", json);
    }

    [Fact]
    public void JsonContract_Float64_SerializedAsNumber()
    {
        var original = HermesValue.FromFloat64(3.14);
        var json = Serialize(original);
        Assert.DoesNotContain("\"", json);
    }

    [Fact]
    public void JsonContract_Bool_SerializedAsLiteral()
    {
        Assert.Equal("true", Serialize(HermesValue.FromBool(true)));
        Assert.Equal("false", Serialize(HermesValue.FromBool(false)));
    }

    [Fact]
    public void JsonContract_String_SerializedAsQuoted()
    {
        var json = Serialize(HermesValue.FromUtf8("hello"));
        Assert.Equal("\"hello\"", json);
    }

    [Fact]
    public void JsonContract_List_SerializedAsArray()
    {
        var list = new List<HermesValue> { HermesValue.FromInt32(1) };
        var json = Serialize(HermesValue.FromList(list));
        Assert.StartsWith("[", json);
        Assert.EndsWith("]", json);
    }

    [Fact]
    public void JsonContract_Dict_SerializedAsObject()
    {
        var dict = new Dictionary<string, HermesValue> { ["k"] = HermesValue.FromInt32(1) };
        var json = Serialize(HermesValue.FromDict(dict));
        Assert.StartsWith("{", json);
        Assert.EndsWith("}", json);
    }

    #endregion
}