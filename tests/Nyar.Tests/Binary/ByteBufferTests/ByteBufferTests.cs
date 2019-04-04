namespace Nyar.Tests.Binary.ByteBufferTests;

public class ByteBufferBasicTests
{
    [Fact]
    public void Constructor_SetsPositionToZero()
    {
        var data = new byte[] { 1, 2, 3, 4 };
        var buffer = new ByteBuffer(data);

        Assert.Equal(0, buffer.Position);
        Assert.Equal(4, buffer.Length);
        Assert.Equal(4, buffer.Remaining);
        Assert.False(buffer.IsEnd);
    }

    [Fact]
    public void Constructor_EmptyData()
    {
        var buffer = new ByteBuffer([]);

        Assert.Equal(0, buffer.Position);
        Assert.Equal(0, buffer.Length);
        Assert.True(buffer.IsEnd);
    }

    [Fact]
    public void Position_CanBeSetAndRead()
    {
        var data = new byte[] { 1, 2, 3, 4, 5 };
        var buffer = new ByteBuffer(data)
        {
            Position = 3
        };

        Assert.Equal(3, buffer.Position);
        Assert.Equal(2, buffer.Remaining);
    }

    [Fact]
    public void RemainingSpan_ReturnsDataFromCurrentPosition()
    {
        var data = new byte[] { 10, 20, 30, 40, 50 };
        var buffer = new ByteBuffer(data)
        {
            Position = 2
        };

        var remaining = buffer.RemainingSpan;

        Assert.Equal(3, remaining.Length);
        Assert.Equal(30, remaining[0]);
        Assert.Equal(40, remaining[1]);
        Assert.Equal(50, remaining[2]);
    }

    [Fact]
    public void Data_ReturnsFullUnderlyingSpan()
    {
        var data = new byte[] { 10, 20, 30 };
        var buffer = new ByteBuffer(data);

        var fullData = buffer.Data;
        Assert.Equal(3, fullData.Length);
        Assert.Equal(10, fullData[0]);
    }
}

public class ByteBufferAdvanceAndPeekTests
{
    [Fact]
    public void Advance_MovesPositionForward()
    {
        var data = new byte[] { 1, 2, 3, 4 };
        var buffer = new ByteBuffer(data);

        buffer.Advance(2);
        Assert.Equal(2, buffer.Position);
    }

    [Fact]
    public void Advance_ThrowsOnNegative()
    {
        var buffer = new ByteBuffer(new byte[4]);
        var threw = false;
        try
        {
            buffer.Advance(-1);
        }
        catch (ArgumentOutOfRangeException)
        {
            threw = true;
        }

        Assert.True(threw);
    }

    [Fact]
    public void Advance_ThrowsWhenExceedingDataLength()
    {
        var buffer = new ByteBuffer(new byte[4]);
        var threw = false;
        try
        {
            buffer.Advance(5);
        }
        catch (InvalidOperationException)
        {
            threw = true;
        }

        Assert.True(threw);
    }

    [Fact]
    public void Peek_ReturnsDataWithoutMovingPosition()
    {
        var data = new byte[] { 10, 20, 30, 40 };
        var buffer = new ByteBuffer(data);

        var peeked = buffer.Peek(2);
        Assert.Equal(2, peeked.Length);
        Assert.Equal(10, peeked[0]);
        Assert.Equal(20, peeked[1]);
        Assert.Equal(0, buffer.Position);
    }

    [Fact]
    public void Peek_ThrowsOnNegative()
    {
        var buffer = new ByteBuffer(new byte[4]);
        var threw = false;
        try
        {
            buffer.Peek(-1);
        }
        catch (ArgumentOutOfRangeException)
        {
            threw = true;
        }

        Assert.True(threw);
    }

    [Fact]
    public void Peek_ThrowsWhenExceedingBoundary()
    {
        var buffer = new ByteBuffer(new byte[4]);
        var threw = false;
        try
        {
            buffer.Peek(5);
        }
        catch (InvalidOperationException)
        {
            threw = true;
        }

        Assert.True(threw);
    }

    [Fact]
    public void ReadBytes_ReturnsDataAndAdvancesPosition()
    {
        var data = new byte[] { 10, 20, 30, 40 };
        var buffer = new ByteBuffer(data);

        var read = buffer.ReadBytes(2);
        Assert.Equal(10, read[0]);
        Assert.Equal(20, read[1]);
        Assert.Equal(2, buffer.Position);
    }

    [Fact]
    public void ReadBytes_ThrowsOnNegative()
    {
        var buffer = new ByteBuffer(new byte[4]);
        var threw = false;
        try
        {
            buffer.ReadBytes(-1);
        }
        catch (ArgumentOutOfRangeException)
        {
            threw = true;
        }

        Assert.True(threw);
    }

    [Fact]
    public void ReadBytes_ThrowsWhenExceedingBoundary()
    {
        var buffer = new ByteBuffer(new byte[4]);
        var threw = false;
        try
        {
            buffer.ReadBytes(5);
        }
        catch (InvalidOperationException)
        {
            threw = true;
        }

        Assert.True(threw);
    }
}

public class ByteBufferMagicTests
{
    [Fact]
    public void MatchMagic_ReturnsTrueWhenMatching()
    {
        var data = new byte[] { 0x89, 0x50, 0x4E, 0x47 };
        var buffer = new ByteBuffer(data);

        Assert.True(buffer.MatchMagic([0x89, 0x50]));
    }

    [Fact]
    public void MatchMagic_ReturnsFalseWhenNotMatching()
    {
        var data = new byte[] { 0x89, 0x50, 0x4E, 0x47 };
        var buffer = new ByteBuffer(data);

        Assert.False(buffer.MatchMagic([0xFF, 0x50]));
    }

    [Fact]
    public void MatchMagic_ReturnsTrueForEmptyMagic()
    {
        var buffer = new ByteBuffer([1, 2, 3]);
        Assert.True(buffer.MatchMagic([]));
    }

    [Fact]
    public void MatchMagic_ReturnsFalseWhenNotEnoughData()
    {
        var buffer = new ByteBuffer([0x89]);
        Assert.False(buffer.MatchMagic([0x89, 0x50]));
    }

    [Fact]
    public void ConsumeMagic_AdvancesPositionOnMatch()
    {
        var data = new byte[] { 0x89, 0x50, 0x4E, 0x47 };
        var buffer = new ByteBuffer(data);

        Assert.True(buffer.ConsumeMagic([0x89, 0x50]));
        Assert.Equal(2, buffer.Position);
    }

    [Fact]
    public void ConsumeMagic_DoesNotAdvanceOnMismatch()
    {
        var data = new byte[] { 0x89, 0x50, 0x4E, 0x47 };
        var buffer = new ByteBuffer(data);

        Assert.False(buffer.ConsumeMagic([0xFF, 0x50]));
        Assert.Equal(0, buffer.Position);
    }
}

public class ByteBufferIntegerReadTests
{
    private readonly byte[] _le_data = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08];
    private readonly byte[] _be_data = [0x08, 0x07, 0x06, 0x05, 0x04, 0x03, 0x02, 0x01];

    [Fact]
    public void ReadU8_ReturnsCorrectValue()
    {
        var buffer = new ByteBuffer([0x42]);
        Assert.Equal(0x42, buffer.ReadU8());
        Assert.Equal(1, buffer.Position);
    }

    [Fact]
    public void ReadU8_ThrowsAtEnd()
    {
        var buffer = new ByteBuffer([]);
        var threw = false;
        try
        {
            buffer.ReadU8();
        }
        catch (InvalidOperationException)
        {
            threw = true;
        }

        Assert.True(threw);
    }

    [Fact]
    public void ReadU16LE_ReturnsCorrectValue()
    {
        var buffer = new ByteBuffer(_le_data);
        Assert.Equal(0x0201, buffer.ReadU16LE());
        Assert.Equal(2, buffer.Position);
    }

    [Fact]
    public void ReadU16BE_ReturnsCorrectValue()
    {
        var buffer = new ByteBuffer(_be_data);
        Assert.Equal(0x0807, buffer.ReadU16BE());
        Assert.Equal(2, buffer.Position);
    }

    [Fact]
    public void ReadU32LE_ReturnsCorrectValue()
    {
        var buffer = new ByteBuffer(_le_data);
        Assert.Equal(0x04030201u, buffer.ReadU32LE());
        Assert.Equal(4, buffer.Position);
    }

    [Fact]
    public void ReadU32BE_ReturnsCorrectValue()
    {
        var buffer = new ByteBuffer(_be_data);
        Assert.Equal(0x08070605u, buffer.ReadU32BE());
        Assert.Equal(4, buffer.Position);
    }

    [Fact]
    public void ReadU64LE_ReturnsCorrectValue()
    {
        var buffer = new ByteBuffer(_le_data);
        Assert.Equal(0x0807060504030201ul, buffer.ReadU64LE());
        Assert.Equal(8, buffer.Position);
    }

    [Fact]
    public void ReadU64BE_ReturnsCorrectValue()
    {
        var buffer = new ByteBuffer(_be_data);
        Assert.Equal(0x0807060504030201ul, buffer.ReadU64BE());
        Assert.Equal(8, buffer.Position);
    }

    [Fact]
    public void ReadI8_ReturnsCorrectValue()
    {
        var buffer = new ByteBuffer([0xFF]);
        Assert.Equal(-1, buffer.ReadI8());
    }

    [Fact]
    public void ReadI16LE_ReturnsCorrectValue()
    {
        var buffer = new ByteBuffer([0x00, 0x80]);
        Assert.Equal(-32768, buffer.ReadI16LE());
    }

    [Fact]
    public void ReadI32LE_ReturnsCorrectValue()
    {
        var buffer = new ByteBuffer([0x00, 0x00, 0x00, 0x80]);
        Assert.Equal(int.MinValue, buffer.ReadI32LE());
    }

    [Fact]
    public void ReadI64LE_ReturnsCorrectValue()
    {
        var buffer = new ByteBuffer([0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80]);
        Assert.Equal(long.MinValue, buffer.ReadI64LE());
    }

    [Fact]
    public void ReadI16BE_ReturnsCorrectValue()
    {
        var buffer = new ByteBuffer([0x80, 0x00]);
        Assert.Equal(-32768, buffer.ReadI16BE());
    }

    [Fact]
    public void ReadI32BE_ReturnsCorrectValue()
    {
        var buffer = new ByteBuffer([0x80, 0x00, 0x00, 0x00]);
        Assert.Equal(int.MinValue, buffer.ReadI32BE());
    }

    [Fact]
    public void ReadI64BE_ReturnsCorrectValue()
    {
        var buffer = new ByteBuffer([0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00]);
        Assert.Equal(long.MinValue, buffer.ReadI64BE());
    }
}

public class ByteBufferFloatReadTests
{
    [Fact]
    public void ReadF32LE_ReturnsCorrectValue()
    {
        var data = new byte[] { 0x00, 0x00, 0x80, 0x3F };
        var buffer = new ByteBuffer(data);
        Assert.Equal(1.0f, buffer.ReadF32LE());
    }

    [Fact]
    public void ReadF32BE_ReturnsCorrectValue()
    {
        var data = new byte[] { 0x3F, 0x80, 0x00, 0x00 };
        var buffer = new ByteBuffer(data);
        Assert.Equal(1.0f, buffer.ReadF32BE());
    }

    [Fact]
    public void ReadF64LE_ReturnsCorrectValue()
    {
        var data = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xF0, 0x3F };
        var buffer = new ByteBuffer(data);
        Assert.Equal(1.0, buffer.ReadF64LE());
    }

    [Fact]
    public void ReadF64BE_ReturnsCorrectValue()
    {
        var data = new byte[] { 0x3F, 0xF0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
        var buffer = new ByteBuffer(data);
        Assert.Equal(1.0, buffer.ReadF64BE());
    }
}

public class ByteBufferLeb128ReadTests
{
    [Fact]
    public void ReadLeb128U32_SingleByte()
    {
        var buffer = new ByteBuffer([0x05]);
        Assert.Equal(5u, buffer.ReadLeb128U32());
    }

    [Fact]
    public void ReadLeb128U32_MultiByte()
    {
        var buffer = new ByteBuffer([0xE5, 0x8E, 0x26]);
        Assert.Equal(624485u, buffer.ReadLeb128U32());
    }

    [Fact]
    public void ReadLeb128U64_MultiByte()
    {
        var buffer = new ByteBuffer([0xE5, 0x8E, 0x26]);
        Assert.Equal(624485ul, buffer.ReadLeb128U64());
    }

    [Fact]
    public void ReadLeb128I32_Negative()
    {
        var buffer = new ByteBuffer([0x7F]);
        Assert.Equal(-1, buffer.ReadLeb128I32());
    }

    [Fact]
    public void ReadZigZagLeb128I32_Positive()
    {
        var buffer = new ByteBuffer([0x00]);
        Assert.Equal(0, buffer.ReadZigZagLeb128I32());
    }

    [Fact]
    public void ReadZigZagLeb128I32_NegativeOne()
    {
        var buffer = new ByteBuffer([0x01]);
        Assert.Equal(-1, buffer.ReadZigZagLeb128I32());
    }

    [Fact]
    public void ReadZigZagLeb128I32_One()
    {
        var buffer = new ByteBuffer([0x02]);
        Assert.Equal(1, buffer.ReadZigZagLeb128I32());
    }

    [Fact]
    public void ReadZigZagLeb128I64_NegativeOne()
    {
        var buffer = new ByteBuffer([0x01]);
        Assert.Equal(-1L, buffer.ReadZigZagLeb128I64());
    }

    [Fact]
    public void ReadLeb128U32_ThrowsOnIncompleteData()
    {
        var buffer = new ByteBuffer([0x80]);
        var threw = false;
        try
        {
            buffer.ReadLeb128U32();
        }
        catch (InvalidOperationException)
        {
            threw = true;
        }

        Assert.True(threw);
    }

    [Fact]
    public void ReadLeb128U32_ThrowsOnOverflow()
    {
        var buffer = new ByteBuffer([0x80, 0x80, 0x80, 0x80, 0x80]);
        var threw = false;
        try
        {
            buffer.ReadLeb128U32();
        }
        catch (InvalidDataException)
        {
            threw = true;
        }

        Assert.True(threw);
    }

    [Fact]
    public void TryDecodeLeb128U32_ValidInput()
    {
        var data = new byte[] { 0xE5, 0x8E, 0x26 };
        Assert.True(ByteBuffer.TryDecodeLeb128U32(data, out var value, out var consumed));
        Assert.Equal(624485u, value);
        Assert.Equal(3, consumed);
    }

    [Fact]
    public void TryDecodeLeb128U32_IncompleteInput()
    {
        var data = new byte[] { 0x80 };
        Assert.False(ByteBuffer.TryDecodeLeb128U32(data, out _, out _));
    }

    [Fact]
    public void TryDecodeLeb128I32_ValidInput()
    {
        var data = new byte[] { 0x7F };
        Assert.True(ByteBuffer.TryDecodeLeb128I32(data, out var value, out var consumed));
        Assert.Equal(-1, value);
        Assert.Equal(1, consumed);
    }
}

public class ByteBufferStringReadTests
{
    [Fact]
    public void ReadString_ReturnsCorrectValue()
    {
        var data = "Hello"u8.ToArray();
        var buffer = new ByteBuffer(data);
        Assert.Equal("Hello", buffer.ReadString(5));
    }

    [Fact]
    public void ReadString_EmptyString()
    {
        var buffer = new ByteBuffer(new byte[10]);
        Assert.Equal(string.Empty, buffer.ReadString(0));
    }

    [Fact]
    public void ReadLeb128String_ReturnsCorrectValue()
    {
        var data = new byte[] { 0x05 }.Concat("Hello"u8.ToArray()).ToArray();
        var buffer = new ByteBuffer(data);
        Assert.Equal("Hello", buffer.ReadLeb128String());
    }

    [Fact]
    public void ReadNullTerminatedString_ReturnsCorrectValue()
    {
        var data = "Hello\0World"u8.ToArray();
        var buffer = new ByteBuffer(data);
        Assert.Equal("Hello", buffer.ReadNullTerminatedString());
        Assert.Equal(6, buffer.Position);
    }

    [Fact]
    public void ReadNullTerminatedString_NoTerminator()
    {
        var data = "Hello"u8.ToArray();
        var buffer = new ByteBuffer(data);
        Assert.Equal("Hello", buffer.ReadNullTerminatedString());
    }

    [Fact]
    public void ReadNullTerminatedString_EmptyString()
    {
        var data = new byte[] { 0x00, 0x01 };
        var buffer = new ByteBuffer(data);
        Assert.Equal(string.Empty, buffer.ReadNullTerminatedString());
        Assert.Equal(1, buffer.Position);
    }
}

public class ByteBufferReadAtTests
{
    private readonly byte[] _data = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08];

    [Fact]
    public void ReadU8At_ReturnsCorrectValue()
    {
        var buffer = new ByteBuffer(_data);
        Assert.Equal(0x05, buffer.ReadU8At(4));
        Assert.Equal(0, buffer.Position);
    }

    [Fact]
    public void ReadU8At_OutOfRange_ReturnsZero()
    {
        var buffer = new ByteBuffer(_data);
        Assert.Equal(0, buffer.ReadU8At(-1));
        Assert.Equal(0, buffer.ReadU8At(8));
    }

    [Fact]
    public void ReadI32At_LE_ReturnsCorrectValue()
    {
        var buffer = new ByteBuffer(_data);
        Assert.Equal(0x04030201, buffer.ReadI32At(0, false));
    }

    [Fact]
    public void ReadI32At_BE_ReturnsCorrectValue()
    {
        var buffer = new ByteBuffer(_data);
        Assert.Equal(0x01020304, buffer.ReadI32At(0, true));
    }

    [Fact]
    public void ReadU32At_ReturnsCorrectValue()
    {
        var buffer = new ByteBuffer(_data);
        Assert.Equal(0x04030201u, buffer.ReadU32At(0));
    }

    [Fact]
    public void ReadF32At_ReturnsCorrectValue()
    {
        var data = new byte[] { 0x00, 0x00, 0x80, 0x3F };
        var buffer = new ByteBuffer(data);
        Assert.Equal(1.0f, buffer.ReadF32At(0, false));
    }

    [Fact]
    public void ReadStringAt_ReturnsCorrectValue()
    {
        var data = "Hello\0World"u8.ToArray();
        var buffer = new ByteBuffer(data);
        Assert.Equal("Hello", buffer.ReadStringAt(0));
        Assert.Equal(0, buffer.Position);
    }

    [Fact]
    public void ReadStringAt_OutOfRange_ReturnsEmpty()
    {
        var buffer = new ByteBuffer(_data);
        Assert.Equal(string.Empty, buffer.ReadStringAt(-1));
    }
}