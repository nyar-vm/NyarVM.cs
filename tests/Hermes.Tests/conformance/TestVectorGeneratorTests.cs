using Xunit;

namespace Hermes.Tests.Conformance;

public class TestVectorGeneratorTests
{
    #region 生成二进制测试向量

    [Fact]
    public void GenerateBinaryVectors_AllPrimitiveTypes()
    {
        var vectors = new List<object>
        {
            new { Id = "int8-zero", Type = "Int8", Hex = BytesToHex(Serialize(HermesValue.FromInt8(0))) },
            new { Id = "int8-max", Type = "Int8", Hex = BytesToHex(Serialize(HermesValue.FromInt8(127))) },
            new { Id = "int8-min", Type = "Int8", Hex = BytesToHex(Serialize(HermesValue.FromInt8(-128))) },
            new { Id = "int16-zero", Type = "Int16", Hex = BytesToHex(Serialize(HermesValue.FromInt16(0))) },
            new { Id = "int32-zero", Type = "Int32", Hex = BytesToHex(Serialize(HermesValue.FromInt32(0))) },
            new { Id = "int64-zero", Type = "Int64", Hex = BytesToHex(Serialize(HermesValue.FromInt64(0))) },
            new { Id = "uint8-zero", Type = "UInt8", Hex = BytesToHex(Serialize(HermesValue.FromUInt8(0))) },
            new { Id = "uint16-zero", Type = "UInt16", Hex = BytesToHex(Serialize(HermesValue.FromUInt16(0))) },
            new { Id = "uint32-zero", Type = "UInt32", Hex = BytesToHex(Serialize(HermesValue.FromUInt32(0))) },
            new { Id = "uint64-zero", Type = "UInt64", Hex = BytesToHex(Serialize(HermesValue.FromUInt64(0))) },
            new { Id = "float32-zero", Type = "Float32", Hex = BytesToHex(Serialize(HermesValue.FromFloat32(0.0f))) },
            new { Id = "float64-zero", Type = "Float64", Hex = BytesToHex(Serialize(HermesValue.FromFloat64(0.0))) },
            new { Id = "bool-true", Type = "Bool", Hex = BytesToHex(Serialize(HermesValue.FromBool(true))) },
            new { Id = "bool-false", Type = "Bool", Hex = BytesToHex(Serialize(HermesValue.FromBool(false))) },
            new { Id = "utf8-empty", Type = "Utf8", Hex = BytesToHex(Serialize(HermesValue.FromUtf8(""))) },
            new { Id = "unit", Type = "Unit", Hex = BytesToHex(Serialize(HermesValue.FromUnit())) }
        };

        Assert.All(vectors, v => { Assert.NotEmpty(v.GetType().GetProperty("Hex")!.GetValue(v)!.ToString()); });
    }

    #endregion

    #region 生成 JSON 测试向量

    [Fact]
    public void GenerateJsonVectors_SpecialTypes()
    {
        var resultOk = new HermesResultValue { IsOk = true, Value = HermesValue.FromInt32(42) };
        var resultErr = new HermesResultValue { IsOk = false, Error = HermesValue.FromUtf8("fail") };
        var union = new HermesUnionValue
        {
            VariantName = "Circle",
            Fields = new Dictionary<string, HermesValue> { ["radius"] = HermesValue.FromInt32(10) }
        };

        var vectors = new Dictionary<string, string>
        {
            ["result-ok"] = SerializeJson(HermesValue.FromResult(resultOk)),
            ["result-err"] = SerializeJson(HermesValue.FromResult(resultErr)),
            ["option-some"] = SerializeJson(HermesValue.FromOption(HermesValue.FromInt32(42))),
            ["option-none"] = SerializeJson(HermesValue.Null),
            ["unit"] = SerializeJson(HermesValue.FromUnit()),
            ["union"] = SerializeJson(HermesValue.FromUnion(union)),
            ["class"] = SerializeJson(HermesValue.FromClass(new Dictionary<string, HermesValue>
            {
                ["name"] = HermesValue.FromUtf8("Alice"),
                ["age"] = HermesValue.FromInt32(30)
            })),
            ["enum"] = SerializeJson(HermesValue.FromEnum("Active")),
            ["flags"] = SerializeJson(HermesValue.FromFlags(5L))
        };

        Assert.Contains("\"ok\":true", vectors["result-ok"]);
        Assert.Contains("\"ok\":false", vectors["result-err"]);
        Assert.Contains("\"variant\"", vectors["union"]);
        Assert.Equal("null", vectors["option-none"]);
        Assert.Equal("null", vectors["unit"]);
    }

    #endregion

    #region 辅助方法

    private static string BytesToHex(byte[] bytes)
    {
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static byte[] Serialize(HermesValue value)
    {
        var registry = new TypeRegistry();
        var serializer = new BinarySerializer(registry);
        return serializer.Serialize(value);
    }

    private static string SerializeJson(HermesValue value)
    {
        var registry = new TypeRegistry();
        var serializer = new JsonSerializer(registry);
        return serializer.Serialize(value);
    }

    #endregion

    #region 类型标签映射验证

    [Fact]
    public void TypeTagMap_Matches_CSharp_Enum()
    {
        var expectedMap = new Dictionary<string, byte>
        {
            ["Int8"] = 0, ["Int16"] = 1, ["Int32"] = 2, ["Int64"] = 3,
            ["UInt8"] = 4, ["UInt16"] = 5, ["UInt32"] = 6, ["UInt64"] = 7,
            ["Float32"] = 8, ["Float64"] = 9,
            ["Bool"] = 10, ["Utf8"] = 11, ["Utf16"] = 12, ["Unit"] = 13,
            ["List"] = 14, ["Array"] = 15, ["Dict"] = 16, ["Record"] = 17,
            ["Option"] = 18, ["Result"] = 19, ["Stream"] = 20,
            ["Class"] = 21, ["Enum"] = 22, ["Flags"] = 23, ["Union"] = 24,
            ["Model"] = 25, ["Cache"] = 26, ["StreamStorage"] = 27,
            ["Service"] = 28, ["Micro"] = 29, ["Unknown"] = 30
        };

        foreach (var kvp in expectedMap)
        {
            var enumValue = Enum.Parse<SchemaType>(kvp.Key);
            Assert.Equal(kvp.Value, (byte)enumValue);
        }
    }

    [Fact]
    public void TypeTagMap_SerializedFirstByte_Matches()
    {
        var testCases = new List<(HermesValue value, byte expectedTag)>
        {
            (HermesValue.FromInt8(0), 0),
            (HermesValue.FromInt16(0), 1),
            (HermesValue.FromInt32(0), 2),
            (HermesValue.FromInt64(0), 3),
            (HermesValue.FromUInt8(0), 4),
            (HermesValue.FromUInt16(0), 5),
            (HermesValue.FromUInt32(0), 6),
            (HermesValue.FromUInt64(0), 7),
            (HermesValue.FromFloat32(0), 8),
            (HermesValue.FromFloat64(0), 9),
            (HermesValue.FromBool(false), 10),
            (HermesValue.FromUtf8(""), 11),
            (HermesValue.FromUtf16(""), 12),
            (HermesValue.FromUnit(), 13),
            (HermesValue.FromList(new List<HermesValue>()), 14),
            (HermesValue.FromArray(new List<HermesValue>()), 15),
            (HermesValue.FromDict(new Dictionary<string, HermesValue>()), 16),
            (HermesValue.FromRecord(new Dictionary<string, HermesValue>()), 17),
            (HermesValue.Null, 18),
            (HermesValue.FromResult(new HermesResultValue { IsOk = true }), 19),
            (HermesValue.FromClass(new Dictionary<string, HermesValue>()), 21),
            (HermesValue.FromEnum("x"), 22),
            (HermesValue.FromFlags(0L), 23),
            (HermesValue.FromUnion(new HermesUnionValue { VariantName = "x" }), 24)
        };

        foreach (var (value, expectedTag) in testCases)
        {
            var bytes = Serialize(value);
            Assert.Equal(expectedTag, bytes[0]);
        }
    }

    #endregion
}