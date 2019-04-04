namespace Nyar.Tests.Binary.FrameTests;

public class ByteBufferWriterBasicTests
{
    [Fact]
    public void Constructor_SetsPositionToZero()
    {
        var writer = new ByteBufferWriter(16);

        Assert.Equal(0, writer.Position);
        Assert.Equal(16, writer.Length);
        Assert.Equal(16, writer.Remaining);
    }

    [Fact]
    public void WriteU8_AdvancesPosition()
    {
        var writer = new ByteBufferWriter(16);

        writer.WriteU8(0x42);
        Assert.Equal(1, writer.Position);
        Assert.Equal(0x42, writer.WrittenData[0]);
    }

    [Fact]
    public void WriteU16LE_WritesCorrectBytes()
    {
        var writer = new ByteBufferWriter(16);

        writer.WriteU16LE(0x0201);
        Assert.Equal(0x01, writer.WrittenData[0]);
        Assert.Equal(0x02, writer.WrittenData[1]);
        Assert.Equal(2, writer.Position);
    }

    [Fact]
    public void WriteU16BE_WritesCorrectBytes()
    {
        var writer = new ByteBufferWriter(16);

        writer.WriteU16BE(0x0201);
        Assert.Equal(0x02, writer.WrittenData[0]);
        Assert.Equal(0x01, writer.WrittenData[1]);
        Assert.Equal(2, writer.Position);
    }

    [Fact]
    public void WriteU32LE_WritesCorrectBytes()
    {
        var writer = new ByteBufferWriter(16);

        writer.WriteU32LE(0x04030201u);
        Assert.Equal(0x01, writer.WrittenData[0]);
        Assert.Equal(0x04, writer.WrittenData[3]);
        Assert.Equal(4, writer.Position);
    }

    [Fact]
    public void WriteU32BE_WritesCorrectBytes()
    {
        var writer = new ByteBufferWriter(16);

        writer.WriteU32BE(0x04030201u);
        Assert.Equal(0x04, writer.WrittenData[0]);
        Assert.Equal(0x01, writer.WrittenData[3]);
        Assert.Equal(4, writer.Position);
    }

    [Fact]
    public void WriteU64LE_WritesCorrectBytes()
    {
        var writer = new ByteBufferWriter(16);

        writer.WriteU64LE(0x0807060504030201ul);
        Assert.Equal(0x01, writer.WrittenData[0]);
        Assert.Equal(0x08, writer.WrittenData[7]);
        Assert.Equal(8, writer.Position);
    }

    [Fact]
    public void WriteU64BE_WritesCorrectBytes()
    {
        var writer = new ByteBufferWriter(16);

        writer.WriteU64BE(0x0807060504030201ul);
        Assert.Equal(0x08, writer.WrittenData[0]);
        Assert.Equal(0x01, writer.WrittenData[7]);
        Assert.Equal(8, writer.Position);
    }
}

public class ByteBufferWriterSignedTests
{
    [Fact]
    public void WriteI8_WritesCorrectByte()
    {
        var writer = new ByteBufferWriter(16);

        writer.WriteI8(-1);
        Assert.Equal(0xFF, writer.WrittenData[0]);
    }

    [Fact]
    public void WriteI16LE_WritesCorrectBytes()
    {
        var writer = new ByteBufferWriter(16);

        writer.WriteI16LE(-1);
        Assert.Equal(0xFF, writer.WrittenData[0]);
        Assert.Equal(0xFF, writer.WrittenData[1]);
    }

    [Fact]
    public void WriteI16BE_WritesCorrectBytes()
    {
        var writer = new ByteBufferWriter(16);

        writer.WriteI16BE(-1);
        Assert.Equal(0xFF, writer.WrittenData[0]);
        Assert.Equal(0xFF, writer.WrittenData[1]);
    }

    [Fact]
    public void WriteI32LE_WritesCorrectBytes()
    {
        var writer = new ByteBufferWriter(16);

        writer.WriteI32LE(int.MinValue);
        var reader = new ByteBuffer(writer.WrittenData);
        Assert.Equal(int.MinValue, reader.ReadI32LE());
    }

    [Fact]
    public void WriteI32BE_WritesCorrectBytes()
    {
        var writer = new ByteBufferWriter(16);

        writer.WriteI32BE(int.MinValue);
        var reader = new ByteBuffer(writer.WrittenData);
        Assert.Equal(int.MinValue, reader.ReadI32BE());
    }

    [Fact]
    public void WriteI64LE_WritesCorrectBytes()
    {
        var writer = new ByteBufferWriter(16);

        writer.WriteI64LE(long.MinValue);
        var reader = new ByteBuffer(writer.WrittenData);
        Assert.Equal(long.MinValue, reader.ReadI64LE());
    }

    [Fact]
    public void WriteI64BE_WritesCorrectBytes()
    {
        var writer = new ByteBufferWriter(16);

        writer.WriteI64BE(long.MinValue);
        var reader = new ByteBuffer(writer.WrittenData);
        Assert.Equal(long.MinValue, reader.ReadI64BE());
    }
}

public class ByteBufferWriterFloatTests
{
    [Fact]
    public void WriteF32LE_ReadBack()
    {
        var writer = new ByteBufferWriter(16);
        writer.WriteF32LE(1.0f);

        var reader = new ByteBuffer(writer.WrittenData);
        Assert.Equal(1.0f, reader.ReadF32LE());
    }

    [Fact]
    public void WriteF32BE_ReadBack()
    {
        var writer = new ByteBufferWriter(16);
        writer.WriteF32BE(1.0f);

        var reader = new ByteBuffer(writer.WrittenData);
        Assert.Equal(1.0f, reader.ReadF32BE());
    }

    [Fact]
    public void WriteF64LE_ReadBack()
    {
        var writer = new ByteBufferWriter(16);
        writer.WriteF64LE(1.0);

        var reader = new ByteBuffer(writer.WrittenData);
        Assert.Equal(1.0, reader.ReadF64LE());
    }

    [Fact]
    public void WriteF64BE_ReadBack()
    {
        var writer = new ByteBufferWriter(16);
        writer.WriteF64BE(1.0);

        var reader = new ByteBuffer(writer.WrittenData);
        Assert.Equal(1.0, reader.ReadF64BE());
    }
}

public class ByteBufferWriterLeb128Tests
{
    [Fact]
    public void WriteLeb128U32_ReadBack()
    {
        var writer = new ByteBufferWriter(16);
        writer.WriteLeb128U32(624485u);

        var reader = new ByteBuffer(writer.WrittenData);
        Assert.Equal(624485u, reader.ReadLeb128U32());
    }

    [Fact]
    public void WriteLeb128U64_ReadBack()
    {
        var writer = new ByteBufferWriter(16);
        writer.WriteLeb128U64(624485ul);

        var reader = new ByteBuffer(writer.WrittenData);
        Assert.Equal(624485ul, reader.ReadLeb128U64());
    }

    [Fact]
    public void WriteLeb128I32_ReadBack()
    {
        var writer = new ByteBufferWriter(16);
        writer.WriteLeb128I32(-1);

        var reader = new ByteBuffer(writer.WrittenData);
        Assert.Equal(-1, reader.ReadLeb128I32());
    }

    [Fact]
    public void WriteZigZagLeb128I32_ReadBack()
    {
        var writer = new ByteBufferWriter(16);
        writer.WriteZigZagLeb128I32(-42);

        var reader = new ByteBuffer(writer.WrittenData);
        Assert.Equal(-42, reader.ReadZigZagLeb128I32());
    }

    [Fact]
    public void WriteZigZagLeb128I64_ReadBack()
    {
        var writer = new ByteBufferWriter(16);
        writer.WriteZigZagLeb128I64(-123456L);

        var reader = new ByteBuffer(writer.WrittenData);
        Assert.Equal(-123456L, reader.ReadZigZagLeb128I64());
    }
}

public class ByteBufferWriterStringTests
{
    [Fact]
    public void WriteString_ReadBack()
    {
        var writer = new ByteBufferWriter(32);
        writer.WriteString("Hello"u8);

        var reader = new ByteBuffer(writer.WrittenData);
        Assert.Equal("Hello", reader.ReadString(5));
    }

    [Fact]
    public void WriteNullTerminatedString_ReadBack()
    {
        var writer = new ByteBufferWriter(32);
        writer.WriteNullTerminatedString("Hello");

        var reader = new ByteBuffer(writer.WrittenData);
        Assert.Equal("Hello", reader.ReadNullTerminatedString());
    }

    [Fact]
    public void WriteLeb128String_ReadBack()
    {
        var writer = new ByteBufferWriter(32);
        writer.WriteLeb128String("Hello");

        var reader = new ByteBuffer(writer.WrittenData);
        Assert.Equal("Hello", reader.ReadLeb128String());
    }
}

public class ByteBufferWriterAdvancedTests
{
    [Fact]
    public void Write_RawBytes()
    {
        var writer = new ByteBufferWriter(16);
        writer.Write([0x01, 0x02, 0x03]);

        Assert.Equal(3, writer.Position);
        Assert.Equal(0x01, writer.WrittenData[0]);
        Assert.Equal(0x02, writer.WrittenData[1]);
        Assert.Equal(0x03, writer.WrittenData[2]);
    }

    [Fact]
    public void Advance_MovesPosition()
    {
        var writer = new ByteBufferWriter(16);
        writer.Advance(4);
        Assert.Equal(4, writer.Position);
    }

    [Fact]
    public void Advance_ThrowsOnNegative()
    {
        var writer = new ByteBufferWriter(16);
        var threw = false;
        try
        {
            writer.Advance(-1);
        }
        catch (ArgumentOutOfRangeException)
        {
            threw = true;
        }

        Assert.True(threw);
    }

    [Fact]
    public void GetSpan_ReturnsWritableSpan()
    {
        var writer = new ByteBufferWriter(16);

        var span = writer.GetSpan(4);
        Assert.True(span.Length >= 4);
    }

    [Fact]
    public void AutoExpansion_WritesBeyondInitialCapacity()
    {
        var writer = new ByteBufferWriter(2);
        writer.WriteU8(0x42);
        writer.WriteU8(0x43);
        writer.WriteU8(0x44);

        Assert.Equal(3, writer.Position);
        Assert.Equal(0x42, writer.WrittenData[0]);
        Assert.Equal(0x43, writer.WrittenData[1]);
        Assert.Equal(0x44, writer.WrittenData[2]);
    }

    [Fact]
    public void AutoExpansion_WriteBeyondInitialCapacity()
    {
        var writer = new ByteBufferWriter(2);

        writer.Write([1, 2, 3]);
        Assert.Equal(3, writer.Position);
        Assert.Equal(1, writer.WrittenData[0]);
        Assert.Equal(2, writer.WrittenData[1]);
        Assert.Equal(3, writer.WrittenData[2]);
    }

    [Fact]
    public void RoundTrip_MultipleWrites()
    {
        var writer = new ByteBufferWriter(64);
        writer.WriteU8(0x01);
        writer.WriteU16LE(0x0201);
        writer.WriteU32LE(0x04030201u);
        writer.WriteF32LE(1.0f);

        var reader = new ByteBuffer(writer.WrittenData);
        Assert.Equal(0x01, reader.ReadU8());
        Assert.Equal(0x0201, reader.ReadU16LE());
        Assert.Equal(0x04030201u, reader.ReadU32LE());
        Assert.Equal(1.0f, reader.ReadF32LE());
    }
}
