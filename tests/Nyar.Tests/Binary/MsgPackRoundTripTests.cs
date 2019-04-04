namespace Nyar.Tests.Binary;

public sealed class MsgPackRoundTripTests
{
    #region �������Ͳ���

    [Fact]
    public void EncodeDecode_Nil_Roundtrip()
    {
        var data = new MsgPackData { Root = new MsgPackValue { Type = MsgPackType.Nil } };
        var encoder = new MsgPackEncoder();
        var bytes = encoder.Encode(data);

        var decoder = new MsgPackDecoder(bytes);
        var result = decoder.Decode();

        Assert.Equal(MsgPackType.Nil, result.Root.type);
    }

    [Fact]
    public void EncodeDecode_True_Roundtrip()
    {
        var data = new MsgPackData { Root = new MsgPackValue { Type = MsgPackType.Boolean, RawValue = true } };
        var encoder = new MsgPackEncoder();
        var bytes = encoder.Encode(data);

        var decoder = new MsgPackDecoder(bytes);
        var result = decoder.Decode();

        Assert.Equal(MsgPackType.Boolean, result.Root.type);
        Assert.Equal(true, result.Root.RawValue);
    }

    [Fact]
    public void EncodeDecode_False_Roundtrip()
    {
        var data = new MsgPackData { Root = new MsgPackValue { Type = MsgPackType.Boolean, RawValue = false } };
        var encoder = new MsgPackEncoder();
        var bytes = encoder.Encode(data);

        var decoder = new MsgPackDecoder(bytes);
        var result = decoder.Decode();

        Assert.Equal(MsgPackType.Boolean, result.Root.type);
        Assert.Equal(false, result.Root.RawValue);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(42)]
    [InlineData(127)]
    public void EncodeDecode_PositiveFixInt_Roundtrip(long value)
    {
        var data = new MsgPackData { Root = new MsgPackValue { Type = MsgPackType.Integer, RawValue = value } };
        var encoder = new MsgPackEncoder();
        var bytes = encoder.Encode(data);

        var decoder = new MsgPackDecoder(bytes);
        var result = decoder.Decode();

        Assert.Equal(MsgPackType.Integer, result.Root.type);
        Assert.Equal(value, result.Root.RawValue);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-32)]
    public void EncodeDecode_NegativeFixInt_Roundtrip(long value)
    {
        var data = new MsgPackData { Root = new MsgPackValue { Type = MsgPackType.Integer, RawValue = value } };
        var encoder = new MsgPackEncoder();
        var bytes = encoder.Encode(data);

        var decoder = new MsgPackDecoder(bytes);
        var result = decoder.Decode();

        Assert.Equal(MsgPackType.Integer, result.Root.type);
        Assert.Equal(value, result.Root.RawValue);
    }

    [Fact]
    public void EncodeDecode_Int8_Roundtrip()
    {
        var data = new MsgPackData { Root = new MsgPackValue { Type = MsgPackType.Integer, RawValue = -50L } };
        var encoder = new MsgPackEncoder();
        var bytes = encoder.Encode(data);

        var decoder = new MsgPackDecoder(bytes);
        var result = decoder.Decode();

        Assert.Equal(MsgPackType.Integer, result.Root.type);
        Assert.Equal(-50L, result.Root.RawValue);
    }

    [Fact]
    public void EncodeDecode_UInt8_Roundtrip()
    {
        var data = new MsgPackData { Root = new MsgPackValue { Type = MsgPackType.UnsignedInteger, RawValue = 200UL } };
        var encoder = new MsgPackEncoder();
        var bytes = encoder.Encode(data);

        var decoder = new MsgPackDecoder(bytes);
        var result = decoder.Decode();

        Assert.Equal(MsgPackType.UnsignedInteger, result.Root.type);
        Assert.Equal(200L, result.Root.RawValue);
    }

    [Fact]
    public void EncodeDecode_Int32_Roundtrip()
    {
        var data = new MsgPackData { Root = new MsgPackValue { Type = MsgPackType.Integer, RawValue = 100000L } };
        var encoder = new MsgPackEncoder();
        var bytes = encoder.Encode(data);

        var decoder = new MsgPackDecoder(bytes);
        var result = decoder.Decode();

        Assert.Equal(MsgPackType.Integer, result.Root.type);
        Assert.Equal(100000L, result.Root.RawValue);
    }

    [Fact]
    public void EncodeDecode_UInt32_Roundtrip()
    {
        var data = new MsgPackData
            { Root = new MsgPackValue { Type = MsgPackType.UnsignedInteger, RawValue = 0xFFFFFFFFUL } };
        var encoder = new MsgPackEncoder();
        var bytes = encoder.Encode(data);

        var decoder = new MsgPackDecoder(bytes);
        var result = decoder.Decode();

        Assert.Equal(MsgPackType.UnsignedInteger, result.Root.type);
        Assert.Equal(0xFFFFFFFFL, result.Root.RawValue);
    }

    [Fact]
    public void EncodeDecode_Int64_Roundtrip()
    {
        var data = new MsgPackData { Root = new MsgPackValue { Type = MsgPackType.Integer, RawValue = 0x8000000000L } };
        var encoder = new MsgPackEncoder();
        var bytes = encoder.Encode(data);

        var decoder = new MsgPackDecoder(bytes);
        var result = decoder.Decode();

        Assert.Equal(MsgPackType.Integer, result.Root.type);
        Assert.Equal(0x8000000000L, result.Root.RawValue);
    }

    [Fact]
    public void EncodeDecode_Float64_Roundtrip()
    {
        var data = new MsgPackData
            { Root = new MsgPackValue { Type = MsgPackType.Float, RawValue = 3.141592653589793 } };
        var encoder = new MsgPackEncoder();
        var bytes = encoder.Encode(data);

        var decoder = new MsgPackDecoder(bytes);
        var result = decoder.Decode();

        Assert.Equal(MsgPackType.Float, result.Root.type);
        Assert.Equal(3.141592653589793, result.Root.RawValue);
    }

    #endregion

    #region �ַ����Ͷ����Ʋ���

    [Fact]
    public void EncodeDecode_FixStr_Roundtrip()
    {
        var data = new MsgPackData { Root = new MsgPackValue { Type = MsgPackType.String, RawValue = "hello" } };
        var encoder = new MsgPackEncoder();
        var bytes = encoder.Encode(data);

        var decoder = new MsgPackDecoder(bytes);
        var result = decoder.Decode();

        Assert.Equal(MsgPackType.String, result.Root.type);
        Assert.Equal("hello", result.Root.RawValue);
    }

    [Fact]
    public void EncodeDecode_Str16_Roundtrip()
    {
        var str = new string('A', 300);
        var data = new MsgPackData { Root = new MsgPackValue { Type = MsgPackType.String, RawValue = str } };
        var encoder = new MsgPackEncoder();
        var bytes = encoder.Encode(data);

        var decoder = new MsgPackDecoder(bytes);
        var result = decoder.Decode();

        Assert.Equal(MsgPackType.String, result.Root.type);
        Assert.Equal(str, result.Root.RawValue);
    }

    [Fact]
    public void EncodeDecode_Binary_Roundtrip()
    {
        var binData = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF };
        var data = new MsgPackData { Root = new MsgPackValue { Type = MsgPackType.Binary, RawValue = binData } };
        var encoder = new MsgPackEncoder();
        var bytes = encoder.Encode(data);

        var decoder = new MsgPackDecoder(bytes);
        var result = decoder.Decode();

        Assert.Equal(MsgPackType.Binary, result.Root.type);
        Assert.Equal(binData, result.Root.RawValue);
    }

    #endregion

    #region �����ӳ�����

    [Fact]
    public void EncodeDecode_FixArray_Roundtrip()
    {
        var items = new MsgPackValue[]
        {
            new() { Type = MsgPackType.Integer, RawValue = 1L },
            new() { Type = MsgPackType.Integer, RawValue = 2L },
            new() { Type = MsgPackType.Integer, RawValue = 3L }
        };

        var data = new MsgPackData
        {
            Root = new MsgPackValue { Type = MsgPackType.Array, ArrayItems = items }
        };

        var encoder = new MsgPackEncoder();
        var bytes = encoder.Encode(data);

        var decoder = new MsgPackDecoder(bytes);
        var result = decoder.Decode();

        Assert.Equal(MsgPackType.Array, result.Root.type);
        Assert.Equal(3, result.Root.ArrayItems.Count);
        Assert.Equal(1L, result.Root.ArrayItems[0].RawValue);
        Assert.Equal(2L, result.Root.ArrayItems[1].RawValue);
        Assert.Equal(3L, result.Root.ArrayItems[2].RawValue);
    }

    [Fact]
    public void EncodeDecode_FixMap_Roundtrip()
    {
        var entries = new MsgPackMapEntry[]
        {
            new()
            {
                Key = new MsgPackValue { Type = MsgPackType.String, RawValue = "name" },
                Value = new MsgPackValue { Type = MsgPackType.String, RawValue = "Alice" }
            },
            new()
            {
                Key = new MsgPackValue { Type = MsgPackType.String, RawValue = "age" },
                Value = new MsgPackValue { Type = MsgPackType.Integer, RawValue = 30L }
            }
        };

        var data = new MsgPackData
        {
            Root = new MsgPackValue { Type = MsgPackType.Map, MapEntries = entries }
        };

        var encoder = new MsgPackEncoder();
        var bytes = encoder.Encode(data);

        var decoder = new MsgPackDecoder(bytes);
        var result = decoder.Decode();

        Assert.Equal(MsgPackType.Map, result.Root.type);
        Assert.Equal(2, result.Root.MapEntries.Count);
        Assert.Equal("name", result.Root.MapEntries[0].Key.RawValue);
        Assert.Equal("Alice", result.Root.MapEntries[0].Value.RawValue);
        Assert.Equal("age", result.Root.MapEntries[1].Key.RawValue);
        Assert.Equal(30L, result.Root.MapEntries[1].Value.RawValue);
    }

    #endregion

    #region ��չ���Ͳ���

    [Fact]
    public void EncodeDecode_FixExt_Roundtrip()
    {
        var extData = new byte[] { 0x01, 0x02, 0x03, 0x04 };
        var data = new MsgPackData
        {
            Root = new MsgPackValue
            {
                Type = MsgPackType.Extension,
                ExtensionType = 0x05,
                ExtensionData = extData
            }
        };

        var encoder = new MsgPackEncoder();
        var bytes = encoder.Encode(data);

        var decoder = new MsgPackDecoder(bytes);
        var result = decoder.Decode();

        Assert.Equal(MsgPackType.Extension, result.Root.type);
        Assert.Equal(0x05, result.Root.ExtensionType);
        Assert.Equal(extData, result.Root.ExtensionData);
    }

    [Fact]
    public void EncodeDecode_Ext8_Roundtrip()
    {
        var extData = new byte[50];
        new Random(42).NextBytes(extData);
        var data = new MsgPackData
        {
            Root = new MsgPackValue
            {
                Type = MsgPackType.Extension,
                ExtensionType = -1,
                ExtensionData = extData
            }
        };

        var encoder = new MsgPackEncoder();
        var bytes = encoder.Encode(data);

        var decoder = new MsgPackDecoder(bytes);
        var result = decoder.Decode();

        Assert.Equal(MsgPackType.Extension, result.Root.type);
        Assert.Equal((sbyte)-1, result.Root.ExtensionType);
        Assert.Equal(extData, result.Root.ExtensionData);
    }

    #endregion

    #region Scanner ����

    [Fact]
    public void Scan_NilMessage_DetectsType()
    {
        var data = new MsgPackData { Root = new MsgPackValue { Type = MsgPackType.Nil } };
        var bytes = new MsgPackEncoder().Encode(data);

        var scanner = new MsgPackScanner(bytes);
        var stats = scanner.ScanStatistics();

        Assert.Equal(1, stats.NilCount);
        Assert.Equal(0, stats.BoolCount);
    }

    [Fact]
    public void Scan_IntegerMessages_DetectsType()
    {
        var data = new MsgPackData { Root = new MsgPackValue { Type = MsgPackType.Integer, RawValue = 42L } };
        var bytes = new MsgPackEncoder().Encode(data);

        var scanner = new MsgPackScanner(bytes);
        var stats = scanner.ScanStatistics();

        Assert.Equal(1, stats.IntegerCount);
    }

    [Fact]
    public void Scan_MixedTypes_AccumulatesAll()
    {
        var encoder = new MsgPackEncoder();
        var bytes1 = encoder.Encode(new MsgPackData { Root = new MsgPackValue { Type = MsgPackType.Nil } });
        var bytes2 = encoder.Encode(new MsgPackData
            { Root = new MsgPackValue { Type = MsgPackType.Integer, RawValue = 1L } });
        var bytes3 = encoder.Encode(new MsgPackData
            { Root = new MsgPackValue { Type = MsgPackType.String, RawValue = "hi" } });
        var combined = bytes1.Concat(bytes2).Concat(bytes3).ToArray();

        var scanner = new MsgPackScanner(combined);
        var stats = scanner.ScanStatistics();

        Assert.Equal(1, stats.NilCount);
        Assert.Equal(1, stats.IntegerCount);
        Assert.Equal(1, stats.StringCount);
    }

    #endregion

    #region Span API ����

    [Fact]
    public void Encode_SpanBuffer_WritesCorrectly()
    {
        var data = new MsgPackData { Root = new MsgPackValue { Type = MsgPackType.Integer, RawValue = 99L } };
        var encoder = new MsgPackEncoder();
        var buffer = new byte[32];

        var len = encoder.Encode(data, buffer);

        Assert.Equal(1, len);
        Assert.Equal(99, buffer[0]);
    }

    #endregion
}
