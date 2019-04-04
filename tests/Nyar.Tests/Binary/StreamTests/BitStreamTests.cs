namespace Nyar.Tests.Binary.StreamTests;

public class BitStreamWriteReadTests
{
    [Fact]
    public void WriteBit_ReadBit_RoundTrip()
    {
        var stream = new BitStream(8);
        stream.WriteBit(true);
        stream.WriteBit(false);
        stream.WriteBit(true);

        stream.Reset();
        Assert.True(stream.ReadBit());
        Assert.False(stream.ReadBit());
        Assert.True(stream.ReadBit());
    }

    [Fact]
    public void WriteBits_ReadBits_RoundTrip()
    {
        var stream = new BitStream(8);
        stream.WriteBits(0b1010, 4);

        stream.Reset();
        Assert.Equal(0b1010u, stream.ReadBits(4));
    }

    [Fact]
    public void WriteByte_ReadByte_RoundTrip()
    {
        var stream = new BitStream(4);
        stream.WriteByte(0x42);

        stream.Reset();
        Assert.Equal(0x42, stream.ReadByte());
    }

    [Fact]
    public void WriteUInt16_ReadUInt16_RoundTrip()
    {
        var stream = new BitStream(4);
        stream.WriteUInt16(0x1234);

        stream.Reset();
        Assert.Equal(0x1234, stream.ReadUInt16());
    }

    [Fact]
    public void WriteUInt32_ReadUInt32_RoundTrip()
    {
        var stream = new BitStream(8);
        stream.WriteUInt32(0x12345678);

        stream.Reset();
        Assert.Equal(0x12345678u, stream.ReadUInt32());
    }

    [Fact]
    public void WriteUInt64_ReadUInt64_RoundTrip()
    {
        var stream = new BitStream(16);
        stream.WriteUInt64(0x123456789ABCDEF0ul);

        stream.Reset();
        Assert.Equal(0x123456789ABCDEF0ul, stream.ReadUInt64());
    }

    [Fact]
    public void WriteInt32_ReadInt32_RoundTrip()
    {
        var stream = new BitStream(8);
        stream.WriteInt32(-42);

        stream.Reset();
        Assert.Equal(-42, stream.ReadInt32());
    }

    [Fact]
    public void WriteInt64_ReadInt64_RoundTrip()
    {
        var stream = new BitStream(16);
        stream.WriteInt64(-123456789L);

        stream.Reset();
        Assert.Equal(-123456789L, stream.ReadInt64());
    }

    [Fact]
    public void WriteFloat_ReadFloat_RoundTrip()
    {
        var stream = new BitStream(8);
        stream.WriteFloat(3.14f);

        stream.Reset();
        Assert.Equal(3.14f, stream.ReadFloat());
    }

    [Fact]
    public void WriteDouble_ReadDouble_RoundTrip()
    {
        var stream = new BitStream(16);
        stream.WriteDouble(3.14);

        stream.Reset();
        Assert.Equal(3.14, stream.ReadDouble());
    }

    [Fact]
    public void WriteBool_ReadBool_RoundTrip()
    {
        var stream = new BitStream(4);
        stream.WriteBool(true);
        stream.WriteBool(false);

        stream.Reset();
        Assert.True(stream.ReadBool());
        Assert.False(stream.ReadBool());
    }
}

public class BitStreamRangedTests
{
    [Fact]
    public void WriteRangedInt_ReadRangedInt_RoundTrip()
    {
        var stream = new BitStream(8);
        stream.WriteRangedInt(50, 0, 100);

        stream.Reset();
        Assert.Equal(50, stream.ReadRangedInt(0, 100));
    }

    [Fact]
    public void WriteRangedInt_BoundaryMin()
    {
        var stream = new BitStream(8);
        stream.WriteRangedInt(0, 0, 100);

        stream.Reset();
        Assert.Equal(0, stream.ReadRangedInt(0, 100));
    }

    [Fact]
    public void WriteRangedInt_BoundaryMax()
    {
        var stream = new BitStream(8);
        stream.WriteRangedInt(100, 0, 100);

        stream.Reset();
        Assert.Equal(100, stream.ReadRangedInt(0, 100));
    }

    [Fact]
    public void WriteRangedInt_ThrowsOnOutOfRange()
    {
        var stream = new BitStream(8);
        var threw1 = false;
        try
        {
            stream.WriteRangedInt(-1, 0, 100);
        }
        catch (ArgumentOutOfRangeException)
        {
            threw1 = true;
        }

        Assert.True(threw1);

        var stream2 = new BitStream(8);
        var threw2 = false;
        try
        {
            stream2.WriteRangedInt(101, 0, 100);
        }
        catch (ArgumentOutOfRangeException)
        {
            threw2 = true;
        }

        Assert.True(threw2);
    }

    [Fact]
    public void WriteRangedFloat_ReadRangedFloat_RoundTrip()
    {
        var stream = new BitStream(8);
        stream.WriteRangedFloat(0.5f, 0f, 1f, 8);

        stream.Reset();
        var result = stream.ReadRangedFloat(0f, 1f, 8);
        Assert.True(Math.Abs(result - 0.5f) < 0.02f);
    }

    [Fact]
    public void WriteRangedFloat_ThrowsOnOutOfRange()
    {
        var stream = new BitStream(8);
        var threw1 = false;
        try
        {
            stream.WriteRangedFloat(-0.1f, 0f, 1f, 8);
        }
        catch (ArgumentOutOfRangeException)
        {
            threw1 = true;
        }

        Assert.True(threw1);

        var stream2 = new BitStream(8);
        var threw2 = false;
        try
        {
            stream2.WriteRangedFloat(1.1f, 0f, 1f, 8);
        }
        catch (ArgumentOutOfRangeException)
        {
            threw2 = true;
        }

        Assert.True(threw2);
    }
}

public class BitStreamPropertyTests
{
    [Fact]
    public void BitPosition_TracksCorrectly()
    {
        var stream = new BitStream(4);
        Assert.Equal(0, stream.BitPosition);

        stream.WriteBit(true);
        Assert.Equal(1, stream.BitPosition);

        stream.WriteBits(5, 3);
        Assert.Equal(4, stream.BitPosition);
    }

    [Fact]
    public void BitLength_TracksWrittenBits()
    {
        var stream = new BitStream(4);
        Assert.Equal(0, stream.BitLength);

        stream.WriteBits(0xFF, 8);
        Assert.Equal(8, stream.BitLength);
    }

    [Fact]
    public void ByteLength_RoundsUp()
    {
        var stream = new BitStream(4);
        stream.WriteBits(0, 5);
        Assert.Equal(1, stream.ByteLength);

        stream.WriteBits(0, 3);
        Assert.Equal(1, stream.ByteLength);

        stream.WriteBits(0, 1);
        Assert.Equal(2, stream.ByteLength);
    }

    [Fact]
    public void RemainingBits_CalculatedCorrectly()
    {
        var stream = new BitStream(4);
        stream.WriteBits(0, 10);
        stream.Reset();
        Assert.Equal(10, stream.RemainingBits);

        stream.ReadBits(3);
        Assert.Equal(7, stream.RemainingBits);
    }

    [Fact]
    public void IsEnd_WhenAllBitsConsumed()
    {
        var stream = new BitStream(4);
        stream.WriteBits(0, 8);
        stream.Reset();

        Assert.False(stream.IsEnd);
        stream.ReadBits(8);
        Assert.True(stream.IsEnd);
    }
}

public class BitStreamAdvancedTests
{
    [Fact]
    public void AlignToByte_AlignsPosition()
    {
        var stream = new BitStream(4);
        stream.WriteBits(0, 5);
        Assert.Equal(5, stream.BitPosition);

        stream.AlignToByte();
        Assert.Equal(8, stream.BitPosition);
    }

    [Fact]
    public void AlignToByte_NoOpWhenAlreadyAligned()
    {
        var stream = new BitStream(4);
        stream.WriteBits(0, 8);
        stream.AlignToByte();
        Assert.Equal(8, stream.BitPosition);
    }

    [Fact]
    public void BitsRequired_ReturnsCorrectValue()
    {
        Assert.Equal(1, BitStream.BitsRequired(0));
        Assert.Equal(1, BitStream.BitsRequired(1));
        Assert.Equal(2, BitStream.BitsRequired(2));
        Assert.Equal(2, BitStream.BitsRequired(3));
        Assert.Equal(3, BitStream.BitsRequired(4));
        Assert.Equal(3, BitStream.BitsRequired(7));
        Assert.Equal(8, BitStream.BitsRequired(255));
    }

    [Fact]
    public void WriteBytes_ReadBytes_RoundTrip()
    {
        var stream = new BitStream(32);
        var data = new byte[] { 0x01, 0x02, 0x03 };
        stream.WriteBytes(data);

        stream.Reset();
        var read = stream.ReadBytes();
        Assert.Equal(data, read);
    }

    [Fact]
    public void WriteString_ReadString_RoundTrip()
    {
        var stream = new BitStream(64);
        stream.WriteString("Hello");

        stream.Reset();
        Assert.Equal("Hello", stream.ReadString());
    }

    [Fact]
    public void ReadBits_ThrowsWhenNotEnoughData()
    {
        var stream = new BitStream([0xFF]);
        var threw = false;
        try
        {
            stream.ReadBits(9);
        }
        catch (InvalidOperationException)
        {
            threw = true;
        }

        Assert.True(threw);
    }

    [Fact]
    public void ReadBit_ThrowsAtEnd()
    {
        var stream = new BitStream([0xFF]);
        stream.ReadBits(8);
        var threw = false;
        try
        {
            stream.ReadBit();
        }
        catch (InvalidOperationException)
        {
            threw = true;
        }

        Assert.True(threw);
    }

    [Fact]
    public void WriteBits_ThrowsOnInvalidBitCount()
    {
        var stream = new BitStream(4);
        var threw1 = false;
        try
        {
            stream.WriteBits(0, 0);
        }
        catch (ArgumentOutOfRangeException)
        {
            threw1 = true;
        }

        Assert.True(threw1);

        var stream2 = new BitStream(4);
        var threw2 = false;
        try
        {
            stream2.WriteBits(0, 33);
        }
        catch (ArgumentOutOfRangeException)
        {
            threw2 = true;
        }

        Assert.True(threw2);
    }

    [Fact]
    public void ReadBits_ThrowsOnInvalidBitCount()
    {
        var stream = new BitStream([0xFF]);
        var threw1 = false;
        try
        {
            stream.ReadBits(0);
        }
        catch (ArgumentOutOfRangeException)
        {
            threw1 = true;
        }

        Assert.True(threw1);

        var stream2 = new BitStream([0xFF, 0xFF, 0xFF, 0xFF, 0xFF]);
        var threw2 = false;
        try
        {
            stream2.ReadBits(33);
        }
        catch (ArgumentOutOfRangeException)
        {
            threw2 = true;
        }

        Assert.True(threw2);
    }

    [Fact]
    public void ToArray_ReturnsWrittenData()
    {
        var stream = new BitStream(4);
        stream.WriteByte(0x42);
        var arr = stream.ToArray();
        Assert.Equal(0x42, arr[0]);
    }

    [Fact]
    public void Constructor_FromReadOnlySpan()
    {
        var data = new byte[] { 0x42, 0x43 };
        var stream = new BitStream(new ReadOnlySpan<byte>(data));
        Assert.Equal(0x42, stream.ReadByte());
        Assert.Equal(0x43, stream.ReadByte());
    }
}
