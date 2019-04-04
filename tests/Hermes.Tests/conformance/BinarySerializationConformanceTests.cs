using Xunit;

namespace Hermes.Tests.Conformance;

public class BinarySerializationConformanceTests
{
    #region Int8 类型标签验证

    [Fact]
    public void BinarySerialize_Int8_TypeTag_Is_0x00()
    {
        var value = HermesValue.FromInt8(0);
        var bytes = Serialize(value);
        Assert.Equal(0x00, bytes[0]);
    }

    #endregion

    #region Int16 类型标签验证

    [Fact]
    public void BinarySerialize_Int16_TypeTag_Is_0x01()
    {
        var value = HermesValue.FromInt16(0);
        var bytes = Serialize(value);
        Assert.Equal(0x01, bytes[0]);
    }

    #endregion

    #region Int32 类型标签验证

    [Fact]
    public void BinarySerialize_Int32_TypeTag_Is_0x02()
    {
        var value = HermesValue.FromInt32(0);
        var bytes = Serialize(value);
        Assert.Equal(0x02, bytes[0]);
    }

    #endregion

    #region Int64 类型标签验证

    [Fact]
    public void BinarySerialize_Int64_TypeTag_Is_0x03()
    {
        var value = HermesValue.FromInt64(0);
        var bytes = Serialize(value);
        Assert.Equal(0x03, bytes[0]);
    }

    #endregion

    #region UInt8 类型标签验证

    [Fact]
    public void BinarySerialize_UInt8_TypeTag_Is_0x04()
    {
        var value = HermesValue.FromUInt8(0);
        var bytes = Serialize(value);
        Assert.Equal(0x04, bytes[0]);
    }

    #endregion

    #region UInt16 类型标签验证

    [Fact]
    public void BinarySerialize_UInt16_TypeTag_Is_0x05()
    {
        var value = HermesValue.FromUInt16(0);
        var bytes = Serialize(value);
        Assert.Equal(0x05, bytes[0]);
    }

    #endregion

    #region UInt32 类型标签验证

    [Fact]
    public void BinarySerialize_UInt32_TypeTag_Is_0x06()
    {
        var value = HermesValue.FromUInt32(0);
        var bytes = Serialize(value);
        Assert.Equal(0x06, bytes[0]);
    }

    #endregion

    #region UInt64 类型标签验证

    [Fact]
    public void BinarySerialize_UInt64_TypeTag_Is_0x07()
    {
        var value = HermesValue.FromUInt64(0);
        var bytes = Serialize(value);
        Assert.Equal(0x07, bytes[0]);
    }

    #endregion

    #region Float32 类型标签验证

    [Fact]
    public void BinarySerialize_Float32_TypeTag_Is_0x08()
    {
        var value = HermesValue.FromFloat32(0);
        var bytes = Serialize(value);
        Assert.Equal(0x08, bytes[0]);
    }

    #endregion

    #region Float64 类型标签验证

    [Fact]
    public void BinarySerialize_Float64_TypeTag_Is_0x09()
    {
        var value = HermesValue.FromFloat64(0);
        var bytes = Serialize(value);
        Assert.Equal(0x09, bytes[0]);
    }

    #endregion

    #region Bool 类型标签验证

    [Fact]
    public void BinarySerialize_Bool_TypeTag_Is_0x0a()
    {
        var value = HermesValue.FromBool(true);
        var bytes = Serialize(value);
        Assert.Equal(0x0a, bytes[0]);
    }

    #endregion

    #region Utf8 类型标签验证

    [Fact]
    public void BinarySerialize_Utf8_TypeTag_Is_0x0b()
    {
        var value = HermesValue.FromUtf8("");
        var bytes = Serialize(value);
        Assert.Equal(0x0b, bytes[0]);
    }

    #endregion

    #region Utf16 类型标签验证

    [Fact]
    public void BinarySerialize_Utf16_TypeTag_Is_0x0c()
    {
        var value = HermesValue.FromUtf16("");
        var bytes = Serialize(value);
        Assert.Equal(0x0c, bytes[0]);
    }

    #endregion

    #region Unit 类型标签验证

    [Fact]
    public void BinarySerialize_Unit_TypeTag_Is_0x0d()
    {
        var value = HermesValue.FromUnit();
        var bytes = Serialize(value);
        Assert.Equal(0x0d, bytes[0]);
        Assert.Single(bytes);
    }

    #endregion

    #region List 类型标签验证

    [Fact]
    public void BinarySerialize_List_TypeTag_Is_0x0e()
    {
        var value = HermesValue.FromList(new List<HermesValue>());
        var bytes = Serialize(value);
        Assert.Equal(0x0e, bytes[0]);
    }

    #endregion

    #region Array 类型标签验证

    [Fact]
    public void BinarySerialize_Array_TypeTag_Is_0x0f()
    {
        var value = HermesValue.FromArray(new List<HermesValue>());
        var bytes = Serialize(value);
        Assert.Equal(0x0f, bytes[0]);
    }

    #endregion

    #region Dict 类型标签验证

    [Fact]
    public void BinarySerialize_Dict_TypeTag_Is_0x10()
    {
        var value = HermesValue.FromDict(new Dictionary<string, HermesValue>());
        var bytes = Serialize(value);
        Assert.Equal(0x10, bytes[0]);
    }

    #endregion

    #region Record 类型标签验证

    [Fact]
    public void BinarySerialize_Record_TypeTag_Is_0x11()
    {
        var value = HermesValue.FromRecord(new Dictionary<string, HermesValue>());
        var bytes = Serialize(value);
        Assert.Equal(0x11, bytes[0]);
    }

    #endregion

    #region Option 类型标签验证

    [Fact]
    public void BinarySerialize_Option_None_TypeTag_Is_0x12()
    {
        var value = HermesValue.Null;
        var bytes = Serialize(value);
        Assert.Equal(0x12, bytes[0]);
    }

    #endregion

    #region Result 类型标签验证

    [Fact]
    public void BinarySerialize_Result_TypeTag_Is_0x13()
    {
        var result = new HermesResultValue { IsOk = true, Value = HermesValue.FromUnit() };
        var value = HermesValue.FromResult(result);
        var bytes = Serialize(value);
        Assert.Equal(0x13, bytes[0]);
    }

    #endregion

    #region Class 类型标签验证

    [Fact]
    public void BinarySerialize_Class_TypeTag_Is_0x15()
    {
        var value = HermesValue.FromClass(new Dictionary<string, HermesValue>());
        var bytes = Serialize(value);
        Assert.Equal(0x15, bytes[0]);
    }

    #endregion

    #region Enum 类型标签验证

    [Fact]
    public void BinarySerialize_Enum_TypeTag_Is_0x16()
    {
        var value = HermesValue.FromEnum("Test");
        var bytes = Serialize(value);
        Assert.Equal(0x16, bytes[0]);
    }

    #endregion

    #region Flags 类型标签验证

    [Fact]
    public void BinarySerialize_Flags_TypeTag_Is_0x17()
    {
        var value = HermesValue.FromFlags(0L);
        var bytes = Serialize(value);
        Assert.Equal(0x17, bytes[0]);
    }

    #endregion

    #region Union 类型标签验证

    [Fact]
    public void BinarySerialize_Union_TypeTag_Is_0x18()
    {
        var union = new HermesUnionValue { VariantName = "A", Fields = new Dictionary<string, HermesValue>() };
        var value = HermesValue.FromUnion(union);
        var bytes = Serialize(value);
        Assert.Equal(0x18, bytes[0]);
    }

    #endregion

    #region 辅助方法

    private static byte[] Serialize(HermesValue value)
    {
        var registry = new TypeRegistry();
        var serializer = new BinarySerializer(registry);
        return serializer.Serialize(value);
    }

    private static HermesValue Deserialize(byte[] data)
    {
        var registry = new TypeRegistry();
        var serializer = new BinarySerializer(registry);
        return serializer.Deserialize(data);
    }

    private static byte[] HexToBytes(string hex)
    {
        var bytes = new byte[hex.Length / 2];

        for (var i = 0; i < hex.Length; i += 2) bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);

        return bytes;
    }

    private static string BytesToHex(byte[] bytes)
    {
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    #endregion

    #region 原始类型 Round-Trip

    [Theory]
    [InlineData((sbyte)-42)]
    [InlineData((sbyte)0)]
    [InlineData((sbyte)127)]
    [InlineData((sbyte)-128)]
    public void BinaryRoundTrip_Int8(sbyte expected)
    {
        var original = HermesValue.FromInt8(expected);
        var bytes = Serialize(original);
        var restored = Deserialize(bytes);
        Assert.Equal(SchemaType.Int8, restored.type);
        Assert.Equal(expected, restored.AsInt8());
    }

    [Theory]
    [InlineData((short)1000)]
    [InlineData((short)-1000)]
    [InlineData(short.MaxValue)]
    [InlineData(short.MinValue)]
    [InlineData((short)0)]
    public void BinaryRoundTrip_Int16(short expected)
    {
        var original = HermesValue.FromInt16(expected);
        var bytes = Serialize(original);
        var restored = Deserialize(bytes);
        Assert.Equal(SchemaType.Int16, restored.type);
        Assert.Equal(expected, restored.AsInt16());
    }

    [Theory]
    [InlineData(100000)]
    [InlineData(-1)]
    [InlineData(int.MaxValue)]
    [InlineData(int.MinValue)]
    [InlineData(0)]
    public void BinaryRoundTrip_Int32(int expected)
    {
        var original = HermesValue.FromInt32(expected);
        var bytes = Serialize(original);
        var restored = Deserialize(bytes);
        Assert.Equal(SchemaType.Int32, restored.type);
        Assert.Equal(expected, restored.AsInt32());
    }

    [Theory]
    [InlineData(9999999999L)]
    [InlineData(long.MaxValue)]
    [InlineData(long.MinValue)]
    [InlineData(0L)]
    public void BinaryRoundTrip_Int64(long expected)
    {
        var original = HermesValue.FromInt64(expected);
        var bytes = Serialize(original);
        var restored = Deserialize(bytes);
        Assert.Equal(SchemaType.Int64, restored.type);
        Assert.Equal(expected, restored.AsInt64());
    }

    [Theory]
    [InlineData((byte)200)]
    [InlineData(byte.MaxValue)]
    [InlineData((byte)0)]
    public void BinaryRoundTrip_UInt8(byte expected)
    {
        var original = HermesValue.FromUInt8(expected);
        var bytes = Serialize(original);
        var restored = Deserialize(bytes);
        Assert.Equal(SchemaType.UInt8, restored.type);
        Assert.Equal(expected, restored.AsUInt8());
    }

    [Theory]
    [InlineData((ushort)50000)]
    [InlineData(ushort.MaxValue)]
    [InlineData((ushort)0)]
    public void BinaryRoundTrip_UInt16(ushort expected)
    {
        var original = HermesValue.FromUInt16(expected);
        var bytes = Serialize(original);
        var restored = Deserialize(bytes);
        Assert.Equal(SchemaType.UInt16, restored.type);
        Assert.Equal(expected, restored.AsUInt16());
    }

    [Theory]
    [InlineData(3000000000u)]
    [InlineData(uint.MaxValue)]
    [InlineData((uint)0)]
    public void BinaryRoundTrip_UInt32(uint expected)
    {
        var original = HermesValue.FromUInt32(expected);
        var bytes = Serialize(original);
        var restored = Deserialize(bytes);
        Assert.Equal(SchemaType.UInt32, restored.type);
        Assert.Equal(expected, restored.AsUInt32());
    }

    [Fact]
    public void BinaryRoundTrip_UInt64_MaxValue()
    {
        var expected = ulong.MaxValue;
        var original = HermesValue.FromUInt64(expected);
        var bytes = Serialize(original);
        var restored = Deserialize(bytes);
        Assert.Equal(SchemaType.UInt64, restored.type);
        Assert.Equal(expected, restored.AsUInt64());
    }

    [Fact]
    public void BinaryRoundTrip_Float32_Pi()
    {
        var expected = 3.14f;
        var original = HermesValue.FromFloat32(expected);
        var bytes = Serialize(original);
        var restored = Deserialize(bytes);
        Assert.Equal(SchemaType.Float32, restored.type);
        Assert.Equal(expected, restored.AsFloat32());
    }

    [Fact]
    public void BinaryRoundTrip_Float32_Zero()
    {
        var original = HermesValue.FromFloat32(0.0f);
        var bytes = Serialize(original);
        var restored = Deserialize(bytes);
        Assert.Equal(SchemaType.Float32, restored.type);
        Assert.Equal(0.0f, restored.AsFloat32());
    }

    [Fact]
    public void BinaryRoundTrip_Float64_Pi()
    {
        var expected = 3.141592653589793;
        var original = HermesValue.FromFloat64(expected);
        var bytes = Serialize(original);
        var restored = Deserialize(bytes);
        Assert.Equal(SchemaType.Float64, restored.type);
        Assert.Equal(expected, restored.AsFloat64());
    }

    [Fact]
    public void BinaryRoundTrip_Float64_Zero()
    {
        var original = HermesValue.FromFloat64(0.0);
        var bytes = Serialize(original);
        var restored = Deserialize(bytes);
        Assert.Equal(SchemaType.Float64, restored.type);
        Assert.Equal(0.0, restored.AsFloat64());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void BinaryRoundTrip_Bool(bool expected)
    {
        var original = HermesValue.FromBool(expected);
        var bytes = Serialize(original);
        var restored = Deserialize(bytes);
        Assert.Equal(SchemaType.Bool, restored.type);
        Assert.Equal(expected, restored.AsBool());
    }

    [Theory]
    [InlineData("hello")]
    [InlineData("")]
    [InlineData("你好")]
    [InlineData("Hello, 世界! 🌍")]
    public void BinaryRoundTrip_Utf8(string expected)
    {
        var original = HermesValue.FromUtf8(expected);
        var bytes = Serialize(original);
        var restored = Deserialize(bytes);
        Assert.Equal(SchemaType.Utf8, restored.type);
        Assert.Equal(expected, restored.AsUtf8());
    }

    [Theory]
    [InlineData("hello")]
    [InlineData("")]
    public void BinaryRoundTrip_Utf16(string expected)
    {
        var original = HermesValue.FromUtf16(expected);
        var bytes = Serialize(original);
        var restored = Deserialize(bytes);
        Assert.Equal(SchemaType.Utf16, restored.type);
        Assert.Equal(expected, restored.AsUtf16());
    }

    [Fact]
    public void BinaryRoundTrip_Unit()
    {
        var original = HermesValue.FromUnit();
        var bytes = Serialize(original);
        Assert.Single(bytes);
        Assert.Equal(0x0d, bytes[0]);
        var restored = Deserialize(bytes);
        Assert.Equal(SchemaType.Unit, restored.type);
    }

    #endregion

    #region 集合类型 Round-Trip

    [Fact]
    public void BinaryRoundTrip_List_Int32()
    {
        var list = new List<HermesValue>
        {
            HermesValue.FromInt32(1),
            HermesValue.FromInt32(2),
            HermesValue.FromInt32(3)
        };
        var original = HermesValue.FromList(list);
        var bytes = Serialize(original);
        var restored = Deserialize(bytes);
        Assert.Equal(SchemaType.List, restored.type);

        var restoredList = (IList<HermesValue>)restored.AsList();
        Assert.Equal(3, restoredList.Count);
        Assert.Equal(1, restoredList[0].AsInt32());
        Assert.Equal(2, restoredList[1].AsInt32());
        Assert.Equal(3, restoredList[2].AsInt32());
    }

    [Fact]
    public void BinaryRoundTrip_List_Empty()
    {
        var original = HermesValue.FromList(new List<HermesValue>());
        var bytes = Serialize(original);
        var restored = Deserialize(bytes);
        Assert.Equal(SchemaType.List, restored.type);

        var restoredList = (IList<HermesValue>)restored.AsList();
        Assert.Empty(restoredList);
    }

    [Fact]
    public void BinaryRoundTrip_Dict()
    {
        var dict = new Dictionary<string, HermesValue>
        {
            ["a"] = HermesValue.FromInt32(1),
            ["b"] = HermesValue.FromUtf8("hello")
        };
        var original = HermesValue.FromDict(dict);
        var bytes = Serialize(original);
        var restored = Deserialize(bytes);
        Assert.Equal(SchemaType.Dict, restored.type);

        var restoredDict = (IDictionary<string, HermesValue>)restored.AsDict();
        Assert.Equal(2, restoredDict.Count);
        Assert.Equal(1, restoredDict["a"].AsInt32());
        Assert.Equal("hello", restoredDict["b"].AsUtf8());
    }

    [Fact]
    public void BinaryRoundTrip_Record()
    {
        var record = new Dictionary<string, HermesValue>
        {
            ["key1"] = HermesValue.FromFloat64(3.14)
        };
        var original = HermesValue.FromRecord(record);
        var bytes = Serialize(original);
        Assert.Equal(0x11, bytes[0]);
        var restored = Deserialize(bytes);
        Assert.Equal(SchemaType.Record, restored.type);

        var restoredRecord = (IDictionary<string, HermesValue>)restored.AsRecord();
        Assert.Single(restoredRecord);
        Assert.Equal(3.14, restoredRecord["key1"].AsFloat64());
    }

    #endregion

    #region 特殊类型 Round-Trip

    [Fact]
    public void BinaryRoundTrip_Option_Some()
    {
        var original = HermesValue.FromOption(HermesValue.FromInt32(42));
        var bytes = Serialize(original);
        var restored = Deserialize(bytes);
        Assert.Equal(SchemaType.Option, restored.type);
        Assert.False(restored.IsNull);

        var inner = (HermesValue?)restored.AsOption();
        Assert.NotNull(inner);
        Assert.Equal(42, inner!.Value.AsInt32());
    }

    [Fact]
    public void BinaryRoundTrip_Option_None()
    {
        var original = HermesValue.Null;
        var bytes = Serialize(original);
        var restored = Deserialize(bytes);
        Assert.Equal(SchemaType.Option, restored.type);
        Assert.True(restored.IsNull);
    }

    [Fact]
    public void BinaryRoundTrip_Result_Ok()
    {
        var result = new HermesResultValue { IsOk = true, Value = HermesValue.FromInt32(42) };
        var original = HermesValue.FromResult(result);
        var bytes = Serialize(original);
        var restored = Deserialize(bytes);
        Assert.Equal(SchemaType.Result, restored.type);

        var restoredResult = (HermesResultValue)restored.AsResult();
        Assert.True(restoredResult.IsOk);
        Assert.Equal(42, restoredResult.Value!.Value.AsInt32());
    }

    [Fact]
    public void BinaryRoundTrip_Result_Err()
    {
        var result = new HermesResultValue { IsOk = false, Error = HermesValue.FromUtf8("fail") };
        var original = HermesValue.FromResult(result);
        var bytes = Serialize(original);
        var restored = Deserialize(bytes);
        Assert.Equal(SchemaType.Result, restored.type);

        var restoredResult = (HermesResultValue)restored.AsResult();
        Assert.False(restoredResult.IsOk);
        Assert.Equal("fail", restoredResult.Error!.Value.AsUtf8());
    }

    #endregion

    #region 用户定义类型 Round-Trip

    [Fact]
    public void BinaryRoundTrip_Class()
    {
        var fields = new Dictionary<string, HermesValue>
        {
            ["name"] = HermesValue.FromUtf8("Alice"),
            ["age"] = HermesValue.FromInt32(30)
        };
        var original = HermesValue.FromClass(fields);
        var bytes = Serialize(original);
        var restored = Deserialize(bytes);
        Assert.Equal(SchemaType.Class, restored.type);

        var restoredFields = (IDictionary<string, HermesValue>)restored.AsClass();
        Assert.Equal(2, restoredFields.Count);
        Assert.Equal("Alice", restoredFields["name"].AsUtf8());
        Assert.Equal(30, restoredFields["age"].AsInt32());
    }

    [Fact]
    public void BinaryRoundTrip_Enum_ByName()
    {
        var original = HermesValue.FromEnum("Active");
        var bytes = Serialize(original);
        var restored = Deserialize(bytes);
        Assert.Equal(SchemaType.Enum, restored.type);
        Assert.Equal("Active", restored.AsEnum());
    }

    [Fact]
    public void BinaryRoundTrip_Enum_ByValue()
    {
        var original = HermesValue.FromEnum(2);
        var bytes = Serialize(original);
        var restored = Deserialize(bytes);
        Assert.Equal(SchemaType.Enum, restored.type);
        Assert.Equal(2, restored.AsEnum());
    }

    [Fact]
    public void BinaryRoundTrip_Flags()
    {
        var original = HermesValue.FromFlags(5L);
        var bytes = Serialize(original);
        var restored = Deserialize(bytes);
        Assert.Equal(SchemaType.Flags, restored.type);
        Assert.Equal(5L, restored.AsFlags());
    }

    [Fact]
    public void BinaryRoundTrip_Union()
    {
        var union = new HermesUnionValue
        {
            VariantName = "Circle",
            Fields = new Dictionary<string, HermesValue> { ["radius"] = HermesValue.FromInt32(10) }
        };
        var original = HermesValue.FromUnion(union);
        var bytes = Serialize(original);
        var restored = Deserialize(bytes);
        Assert.Equal(SchemaType.Union, restored.type);

        var restoredUnion = (HermesUnionValue)restored.AsUnion();
        Assert.Equal("Circle", restoredUnion.VariantName);
        Assert.Single(restoredUnion.fields);
        Assert.Equal(10, restoredUnion.fields["radius"].AsInt32());
    }

    #endregion

    #region Schema 感知序列化 Round-Trip

    [Fact]
    public void BinaryRoundTrip_SchemaAware_Class()
    {
        var registry = new TypeRegistry();
        registry.RegisterClass("User",
        [
            new("name", SchemaType.Utf8),
            new("age", SchemaType.Int32),
            new("email", SchemaType.Utf8, isOptional: true)
        ]);

        var serializer = new BinarySerializer(registry);
        var fields = new Dictionary<string, HermesValue>
        {
            ["name"] = HermesValue.FromUtf8("Bob"),
            ["age"] = HermesValue.FromInt32(25)
        };
        var original = HermesValue.FromClass(fields);
        var bytes = serializer.Serialize(original, "User");
        var restored = serializer.Deserialize(bytes, "User");

        Assert.Equal(SchemaType.Class, restored.type);
        var restoredFields = (IDictionary<string, HermesValue>)restored.AsClass();
        Assert.Equal("Bob", restoredFields["name"].AsUtf8());
        Assert.Equal(25, restoredFields["age"].AsInt32());
    }

    [Fact]
    public void BinaryRoundTrip_SchemaAware_Enum()
    {
        var registry = new TypeRegistry();
        registry.RegisterEnum("Status", ["Active", "Inactive", "Pending"]);

        var serializer = new BinarySerializer(registry);
        var original = HermesValue.FromEnum("Active");
        var bytes = serializer.Serialize(original, "Status");
        var restored = serializer.Deserialize(bytes, "Status");

        Assert.Equal(SchemaType.Enum, restored.type);
        Assert.Equal("Active", restored.AsEnum());
    }

    [Fact]
    public void BinaryRoundTrip_SchemaAware_Union()
    {
        var registry = new TypeRegistry();
        registry.RegisterUnion("Shape", ["Circle", "Rectangle"]);

        var serializer = new BinarySerializer(registry);
        var union = new HermesUnionValue
        {
            VariantName = "Circle",
            Fields = new Dictionary<string, HermesValue> { ["radius"] = HermesValue.FromInt32(10) }
        };
        var original = HermesValue.FromUnion(union);
        var bytes = serializer.Serialize(original, "Shape");
        var restored = serializer.Deserialize(bytes, "Shape");

        Assert.Equal(SchemaType.Union, restored.type);
        var restoredUnion = (HermesUnionValue)restored.AsUnion();
        Assert.Equal("Circle", restoredUnion.VariantName);
    }

    #endregion

    #region 嵌套结构 Round-Trip

    [Fact]
    public void BinaryRoundTrip_Nested_ListOfDict()
    {
        var list = new List<HermesValue>
        {
            HermesValue.FromDict(new Dictionary<string, HermesValue>
            {
                ["x"] = HermesValue.FromInt32(1)
            }),
            HermesValue.FromDict(new Dictionary<string, HermesValue>
            {
                ["y"] = HermesValue.FromInt32(2)
            })
        };
        var original = HermesValue.FromList(list);
        var bytes = Serialize(original);
        var restored = Deserialize(bytes);

        var restoredList = (IList<HermesValue>)restored.AsList();
        Assert.Equal(2, restoredList.Count);
        Assert.Equal(1, ((IDictionary<string, HermesValue>)restoredList[0].AsDict())["x"].AsInt32());
        Assert.Equal(2, ((IDictionary<string, HermesValue>)restoredList[1].AsDict())["y"].AsInt32());
    }

    [Fact]
    public void BinaryRoundTrip_OptionalInClass()
    {
        var registry = new TypeRegistry();
        registry.RegisterClass("Person",
        [
            new("name", SchemaType.Utf8),
            new("nickname", SchemaType.Utf8, isOptional: true)
        ]);

        var serializer = new BinarySerializer(registry);

        var withNickname = new Dictionary<string, HermesValue>
        {
            ["name"] = HermesValue.FromUtf8("Alice"),
            ["nickname"] = HermesValue.FromUtf8("Ali")
        };
        var bytesWith = serializer.Serialize(HermesValue.FromClass(withNickname), "Person");
        var restoredWith = serializer.Deserialize(bytesWith, "Person");
        var fieldsWith = (IDictionary<string, HermesValue>)restoredWith.AsClass();
        Assert.Equal("Alice", fieldsWith["name"].AsUtf8());
        Assert.Equal("Ali", fieldsWith["nickname"].AsUtf8());

        var withoutNickname = new Dictionary<string, HermesValue>
        {
            ["name"] = HermesValue.FromUtf8("Bob")
        };
        var bytesWithout = serializer.Serialize(HermesValue.FromClass(withoutNickname), "Person");
        var restoredWithout = serializer.Deserialize(bytesWithout, "Person");
        var fieldsWithout = (IDictionary<string, HermesValue>)restoredWithout.AsClass();
        Assert.Equal("Bob", fieldsWithout["name"].AsUtf8());
        Assert.False(fieldsWithout.ContainsKey("nickname"));
    }

    #endregion

    #region 字节精确验证

    [Fact]
    public void BinarySerialize_Bool_True_ExactBytes()
    {
        var bytes = Serialize(HermesValue.FromBool(true));
        Assert.Equal("0a01", BytesToHex(bytes));
    }

    [Fact]
    public void BinarySerialize_Bool_False_ExactBytes()
    {
        var bytes = Serialize(HermesValue.FromBool(false));
        Assert.Equal("0a00", BytesToHex(bytes));
    }

    [Fact]
    public void BinarySerialize_Unit_ExactBytes()
    {
        var bytes = Serialize(HermesValue.FromUnit());
        Assert.Equal("0d", BytesToHex(bytes));
    }

    [Fact]
    public void BinarySerialize_Int32_42_ExactBytes()
    {
        var bytes = Serialize(HermesValue.FromInt32(42));
        Assert.Equal("022a000000", BytesToHex(bytes));
    }

    [Fact]
    public void BinarySerialize_Option_None_ExactBytes()
    {
        var bytes = Serialize(HermesValue.Null);
        Assert.Equal("1200", BytesToHex(bytes));
    }

    #endregion
}