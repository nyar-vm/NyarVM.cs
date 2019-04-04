using Xunit;
using HermesJsonSerializer = Hermes.Runtime.Serialization.JsonSerializer;

namespace Hermes.Tests.Conformance;

public class TestVectorExporter
{
    [Fact]
    public void ExportAllTestVectors()
    {
        var registry = new TypeRegistry();
        var binarySerializer = new BinarySerializer(registry);
        var jsonSerializer = new HermesJsonSerializer(registry);

        var vectors = new List<object>();

        AddPrimitiveVectors(vectors, binarySerializer, jsonSerializer);
        AddCollectionVectors(vectors, binarySerializer, jsonSerializer);
        AddSpecialVectors(vectors, binarySerializer, jsonSerializer);
        AddUserDefinedVectors(vectors, binarySerializer, jsonSerializer);

        Assert.NotEmpty(vectors);
        Assert.True(vectors.Count >= 30);
    }

    private static void AddPrimitiveVectors(List<object> vectors, BinarySerializer binary, HermesJsonSerializer json)
    {
        Add(vectors, "int8-zero", binary, json, HermesValue.FromInt8(0));
        Add(vectors, "int8-max", binary, json, HermesValue.FromInt8(127));
        Add(vectors, "int8-min", binary, json, HermesValue.FromInt8(-128));
        Add(vectors, "int8-neg", binary, json, HermesValue.FromInt8(-42));
        Add(vectors, "int16-pos", binary, json, HermesValue.FromInt16(1000));
        Add(vectors, "int32-pos", binary, json, HermesValue.FromInt32(100000));
        Add(vectors, "int32-neg", binary, json, HermesValue.FromInt32(-1));
        Add(vectors, "int64-large", binary, json, HermesValue.FromInt64(9999999999L));
        Add(vectors, "uint8-max", binary, json, HermesValue.FromUInt8(200));
        Add(vectors, "uint16-pos", binary, json, HermesValue.FromUInt16(50000));
        Add(vectors, "uint32-pos", binary, json, HermesValue.FromUInt32(3000000000u));
        Add(vectors, "uint64-max", binary, json, HermesValue.FromUInt64(ulong.MaxValue));
        Add(vectors, "float32-pi", binary, json, HermesValue.FromFloat32(3.14f));
        Add(vectors, "float64-pi", binary, json, HermesValue.FromFloat64(3.141592653589793));
        Add(vectors, "bool-true", binary, json, HermesValue.FromBool(true));
        Add(vectors, "bool-false", binary, json, HermesValue.FromBool(false));
        Add(vectors, "utf8-ascii", binary, json, HermesValue.FromUtf8("hello"));
        Add(vectors, "utf8-unicode", binary, json, HermesValue.FromUtf8("你好"));
        Add(vectors, "utf16-ascii", binary, json, HermesValue.FromUtf16("hello"));
        Add(vectors, "unit", binary, json, HermesValue.FromUnit());
    }

    private static void AddCollectionVectors(List<object> vectors, BinarySerializer binary, HermesJsonSerializer json)
    {
        Add(vectors, "list-empty", binary, json, HermesValue.FromList(new List<HermesValue>()));
        var listItems = new List<HermesValue>
        {
            HermesValue.FromInt32(1),
            HermesValue.FromInt32(2),
            HermesValue.FromInt32(3)
        };
        Add(vectors, "list-int32", binary, json, HermesValue.FromList(listItems));
        Add(vectors, "dict-simple", binary, json, HermesValue.FromDict(
            new Dictionary<string, HermesValue> { ["a"] = HermesValue.FromInt32(1) }));
        Add(vectors, "record-simple", binary, json, HermesValue.FromRecord(
            new Dictionary<string, HermesValue> { ["key1"] = HermesValue.FromFloat64(3.14) }));
    }

    private static void AddSpecialVectors(List<object> vectors, BinarySerializer binary, HermesJsonSerializer json)
    {
        Add(vectors, "option-none", binary, json, HermesValue.Null);
        Add(vectors, "option-some-int32", binary, json, HermesValue.FromOption(HermesValue.FromInt32(42)));
        Add(vectors, "result-ok-int32", binary, json,
            HermesValue.FromResult(new HermesResultValue { IsOk = true, Value = HermesValue.FromInt32(42) }));
        Add(vectors, "result-err-utf8", binary, json,
            HermesValue.FromResult(new HermesResultValue { IsOk = false, Error = HermesValue.FromUtf8("fail") }));
    }

    private static void AddUserDefinedVectors(List<object> vectors, BinarySerializer binary, HermesJsonSerializer json)
    {
        Add(vectors, "class-simple", binary, json, HermesValue.FromClass(
            new Dictionary<string, HermesValue>
            {
                ["name"] = HermesValue.FromUtf8("Alice"),
                ["age"] = HermesValue.FromInt32(30)
            }));
        Add(vectors, "enum-by-name", binary, json, HermesValue.FromEnum("Active"));
        Add(vectors, "enum-by-value", binary, json, HermesValue.FromEnum(2));
        Add(vectors, "flags-bitmask", binary, json, HermesValue.FromFlags(5L));
        Add(vectors, "union-simple", binary, json,
            HermesValue.FromUnion(new HermesUnionValue
            {
                VariantName = "Circle",
                Fields = new Dictionary<string, HermesValue> { ["radius"] = HermesValue.FromInt32(10) }
            }));
    }

    private static void Add(List<object> vectors, string id, BinarySerializer binary, HermesJsonSerializer json,
        HermesValue value)
    {
        var binaryBytes = binary.Serialize(value);
        var jsonStr = json.Serialize(value);

        var entry = new Dictionary<string, object>();
        entry["id"] = id;
        entry["SchemaType"] = value.type.ToString();
        entry["typeTag"] = (int)value.type;
        entry["binaryHex"] = Convert.ToHexString(binaryBytes).ToLowerInvariant();
        entry["binaryLength"] = binaryBytes.Length;
        entry["json"] = jsonStr;
        vectors.Add(entry);
    }
}