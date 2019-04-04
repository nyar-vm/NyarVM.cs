namespace Nyar.Tests.Binary.CodecTests;

public class IntegerCodecTests
{
    [Fact]
    public void U8_EncodeDecode()
    {
        var codec = new U8();
        var buf = new byte[1];
        codec.Encode(0x42, buf);
        Assert.Equal(0x42, buf[0]);
        Assert.Equal(0x42, codec.Decode(buf));
        Assert.Equal(1, codec.GetSize(0));
    }

    [Fact]
    public void I8_EncodeDecode()
    {
        var codec = new I8();
        var buf = new byte[1];
        codec.Encode(-1, buf);
        Assert.Equal(0xFF, buf[0]);
        Assert.Equal(-1, codec.Decode(buf));
    }

    [Fact]
    public void U16LE_EncodeDecode()
    {
        var codec = new U16LE();
        var buf = new byte[2];
        codec.Encode(0x0201, buf);
        Assert.Equal(0x01, buf[0]);
        Assert.Equal(0x02, buf[1]);
        Assert.Equal(0x0201, codec.Decode(buf));
        Assert.Equal(2, codec.GetSize(0));
    }

    [Fact]
    public void U16BE_EncodeDecode()
    {
        var codec = new U16BE();
        var buf = new byte[2];
        codec.Encode(0x0201, buf);
        Assert.Equal(0x02, buf[0]);
        Assert.Equal(0x01, buf[1]);
        Assert.Equal(0x0201, codec.Decode(buf));
    }

    [Fact]
    public void I16LE_EncodeDecode()
    {
        var codec = new I16LE();
        var buf = new byte[2];
        codec.Encode(-1, buf);
        Assert.Equal(-1, codec.Decode(buf));
    }

    [Fact]
    public void I16BE_EncodeDecode()
    {
        var codec = new I16BE();
        var buf = new byte[2];
        codec.Encode(-1, buf);
        Assert.Equal(-1, codec.Decode(buf));
    }

    [Fact]
    public void U32LE_EncodeDecode()
    {
        var codec = new U32LE();
        var buf = new byte[4];
        codec.Encode(0x04030201u, buf);
        Assert.Equal(0x01, buf[0]);
        Assert.Equal(0x04, buf[3]);
        Assert.Equal(0x04030201u, codec.Decode(buf));
        Assert.Equal(4, codec.GetSize(0));
    }

    [Fact]
    public void U32BE_EncodeDecode()
    {
        var codec = new U32BE();
        var buf = new byte[4];
        codec.Encode(0x04030201u, buf);
        Assert.Equal(0x04, buf[0]);
        Assert.Equal(0x01, buf[3]);
        Assert.Equal(0x04030201u, codec.Decode(buf));
    }

    [Fact]
    public void I32LE_EncodeDecode()
    {
        var codec = new I32LE();
        var buf = new byte[4];
        codec.Encode(int.MinValue, buf);
        Assert.Equal(int.MinValue, codec.Decode(buf));
    }

    [Fact]
    public void I32BE_EncodeDecode()
    {
        var codec = new I32BE();
        var buf = new byte[4];
        codec.Encode(int.MinValue, buf);
        Assert.Equal(int.MinValue, codec.Decode(buf));
    }

    [Fact]
    public void U64LE_EncodeDecode()
    {
        var codec = new U64LE();
        var buf = new byte[8];
        codec.Encode(0x0807060504030201ul, buf);
        Assert.Equal(0x01, buf[0]);
        Assert.Equal(0x08, buf[7]);
        Assert.Equal(0x0807060504030201ul, codec.Decode(buf));
        Assert.Equal(8, codec.GetSize(0));
    }

    [Fact]
    public void U64BE_EncodeDecode()
    {
        var codec = new U64BE();
        var buf = new byte[8];
        codec.Encode(0x0807060504030201ul, buf);
        Assert.Equal(0x08, buf[0]);
        Assert.Equal(0x01, buf[7]);
        Assert.Equal(0x0807060504030201ul, codec.Decode(buf));
    }

    [Fact]
    public void I64LE_EncodeDecode()
    {
        var codec = new I64LE();
        var buf = new byte[8];
        codec.Encode(long.MinValue, buf);
        Assert.Equal(long.MinValue, codec.Decode(buf));
    }

    [Fact]
    public void I64BE_EncodeDecode()
    {
        var codec = new I64BE();
        var buf = new byte[8];
        codec.Encode(long.MinValue, buf);
        Assert.Equal(long.MinValue, codec.Decode(buf));
    }
}

public class FloatCodecTests
{
    [Fact]
    public void F32LE_EncodeDecode()
    {
        var codec = new F32LE();
        var buf = new byte[4];
        codec.Encode(1.0f, buf);
        Assert.Equal(1.0f, codec.Decode(buf));
        Assert.Equal(4, codec.GetSize(0));
    }

    [Fact]
    public void F32BE_EncodeDecode()
    {
        var codec = new F32BE();
        var buf = new byte[4];
        codec.Encode(1.0f, buf);
        Assert.Equal(1.0f, codec.Decode(buf));
    }

    [Fact]
    public void F64LE_EncodeDecode()
    {
        var codec = new F64LE();
        var buf = new byte[8];
        codec.Encode(1.0, buf);
        Assert.Equal(1.0, codec.Decode(buf));
        Assert.Equal(8, codec.GetSize(0));
    }

    [Fact]
    public void F64BE_EncodeDecode()
    {
        var codec = new F64BE();
        var buf = new byte[8];
        codec.Encode(1.0, buf);
        Assert.Equal(1.0, codec.Decode(buf));
    }

    [Fact]
    public void F32LE_NegativeValue()
    {
        var codec = new F32LE();
        var buf = new byte[4];
        codec.Encode(-3.14f, buf);
        Assert.Equal(-3.14f, codec.Decode(buf));
    }

    [Fact]
    public void F64LE_NegativeValue()
    {
        var codec = new F64LE();
        var buf = new byte[8];
        codec.Encode(-3.14, buf);
        Assert.Equal(-3.14, codec.Decode(buf));
    }
}

public class Leb128CodecTests
{
    [Fact]
    public void Leb128UInt32_SingleByte()
    {
        var codec = new Leb128UInt32();
        var buf = new byte[1];
        codec.Encode(5, buf);
        Assert.Equal(5u, codec.Decode(buf));
        Assert.Equal(-1, codec.GetSize(0));
    }

    [Fact]
    public void Leb128UInt32_MultiByte()
    {
        var codec = new Leb128UInt32();
        var buf = new byte[10];
        codec.Encode(624485u, buf);
        Assert.Equal(624485u, codec.Decode(buf));
    }

    [Fact]
    public void Leb128UInt64_MultiByte()
    {
        var codec = new Leb128UInt64();
        var buf = new byte[10];
        codec.Encode(624485ul, buf);
        Assert.Equal(624485ul, codec.Decode(buf));
    }

    [Fact]
    public void Leb128Int32_Positive()
    {
        var codec = new Leb128Int32();
        var buf = new byte[10];
        codec.Encode(42, buf);
        Assert.Equal(42, codec.Decode(buf));
    }

    [Fact]
    public void Leb128Int32_Negative()
    {
        var codec = new Leb128Int32();
        var buf = new byte[10];
        codec.Encode(-1, buf);
        Assert.Equal(-1, codec.Decode(buf));
    }

    [Fact]
    public void Leb128Int64_Positive()
    {
        var codec = new Leb128Int64();
        var buf = new byte[10];
        codec.Encode(42L, buf);
        Assert.Equal(42L, codec.Decode(buf));
    }

    [Fact]
    public void Leb128Int64_Negative()
    {
        var codec = new Leb128Int64();
        var buf = new byte[10];
        codec.Encode(-1L, buf);
        Assert.Equal(-1L, codec.Decode(buf));
    }

    [Fact]
    public void Leb128UInt32_Zero()
    {
        var codec = new Leb128UInt32();
        var buf = new byte[10];
        codec.Encode(0, buf);
        Assert.Equal(0u, codec.Decode(buf));
    }

    [Fact]
    public void Leb128UInt32_MaxValue()
    {
        var codec = new Leb128UInt32();
        var buf = new byte[10];
        codec.Encode(uint.MaxValue, buf);
        Assert.Equal(uint.MaxValue, codec.Decode(buf));
    }
}

public class ZigZagLeb128CodecTests
{
    [Fact]
    public void ZigZagLeb128Int32_Zero()
    {
        var codec = new ZigZagLeb128Int32();
        var buf = new byte[10];
        codec.Encode(0, buf);
        Assert.Equal(0, codec.Decode(buf));
    }

    [Fact]
    public void ZigZagLeb128Int32_NegativeOne()
    {
        var codec = new ZigZagLeb128Int32();
        var buf = new byte[10];
        codec.Encode(-1, buf);
        Assert.Equal(-1, codec.Decode(buf));
    }

    [Fact]
    public void ZigZagLeb128Int32_One()
    {
        var codec = new ZigZagLeb128Int32();
        var buf = new byte[10];
        codec.Encode(1, buf);
        Assert.Equal(1, codec.Decode(buf));
    }

    [Fact]
    public void ZigZagLeb128Int32_LargeNegative()
    {
        var codec = new ZigZagLeb128Int32();
        var buf = new byte[10];
        codec.Encode(-123456, buf);
        Assert.Equal(-123456, codec.Decode(buf));
    }

    [Fact]
    public void ZigZagLeb128Int64_Zero()
    {
        var codec = new ZigZagLeb128Int64();
        var buf = new byte[10];
        codec.Encode(0L, buf);
        Assert.Equal(0L, codec.Decode(buf));
    }

    [Fact]
    public void ZigZagLeb128Int64_NegativeOne()
    {
        var codec = new ZigZagLeb128Int64();
        var buf = new byte[10];
        codec.Encode(-1L, buf);
        Assert.Equal(-1L, codec.Decode(buf));
    }

    [Fact]
    public void ZigZagLeb128Int64_LargeValue()
    {
        var codec = new ZigZagLeb128Int64();
        var buf = new byte[10];
        codec.Encode(1234567890123L, buf);
        Assert.Equal(1234567890123L, codec.Decode(buf));
    }
}

public class ZigZagStaticTests
{
    [Fact]
    public void Encode_Int32_Zero()
    {
        Assert.Equal(0u, ZigZag.Encode(0));
    }

    [Fact]
    public void Encode_Int32_NegativeOne()
    {
        Assert.Equal(1u, ZigZag.Encode(-1));
    }

    [Fact]
    public void Encode_Int32_One()
    {
        Assert.Equal(2u, ZigZag.Encode(1));
    }

    [Fact]
    public void Encode_Int32_NegativeTwo()
    {
        Assert.Equal(3u, ZigZag.Encode(-2));
    }

    [Fact]
    public void Encode_Int32_Two()
    {
        Assert.Equal(4u, ZigZag.Encode(2));
    }

    [Fact]
    public void Encode_Int64_Zero()
    {
        Assert.Equal(0ul, ZigZag.Encode(0L));
    }

    [Fact]
    public void Encode_Int64_NegativeOne()
    {
        Assert.Equal(1ul, ZigZag.Encode(-1L));
    }

    [Fact]
    public void Encode_Int16_Zero()
    {
        Assert.Equal((ushort)0, ZigZag.Encode((short)0));
    }

    [Fact]
    public void Decode_Int32_RoundTrip()
    {
        for (var i = -100; i <= 100; i++)
        {
            Assert.Equal(i, ZigZag.Decode(ZigZag.Encode(i)));
        }
    }

    [Fact]
    public void Decode_Int64_RoundTrip()
    {
        long[] values = [-100, -1, 0, 1, 100, long.MinValue, long.MaxValue];
        foreach (var v in values)
        {
            Assert.Equal(v, ZigZag.Decode(ZigZag.Encode(v)));
        }
    }

    [Fact]
    public void Decode_Int16_RoundTrip()
    {
        for (short i = -100; i <= 100; i++)
        {
            Assert.Equal(i, ZigZag.Decode(ZigZag.Encode(i)));
        }
    }
}
