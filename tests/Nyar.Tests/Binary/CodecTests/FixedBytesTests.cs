namespace Nyar.Tests.Binary.CodecTests;

public class FixedBytesTests
{
    [Fact]
    public void FixedBytes4_AsSpan_ReturnsCorrectData()
    {
        var data = new byte[] { 0x01, 0x02, 0x03, 0x04 };
        var fb = FixedBytes4.FromSpan(data);
        var span = fb.AsSpan();

        Assert.Equal(4, span.Length);
        Assert.Equal(0x01, span[0]);
        Assert.Equal(0x04, span[3]);
    }

    [Fact]
    public void FixedBytes4_Indexer_GetSet()
    {
        var fb = new FixedBytes4();
        fb[0] = 0x42;
        fb[3] = 0xFF;

        Assert.Equal(0x42, fb[0]);
        Assert.Equal(0xFF, fb[3]);
    }

    [Fact]
    public void FixedBytes4_FromSpan_CopiesData()
    {
        var data = new byte[] { 0x01, 0x02, 0x03, 0x04 };
        var fb = FixedBytes4.FromSpan(data);

        data[0] = 0xFF;
        Assert.Equal(0x01, fb[0]);
    }

    [Fact]
    public void FixedBytes8_AsSpan_ReturnsCorrectData()
    {
        var data = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08 };
        var fb = FixedBytes8.FromSpan(data);
        var span = fb.AsSpan();

        Assert.Equal(8, span.Length);
        Assert.Equal(0x01, span[0]);
        Assert.Equal(0x08, span[7]);
    }

    [Fact]
    public void FixedBytes8_Indexer_GetSet()
    {
        var fb = new FixedBytes8();
        fb[0] = 0x42;
        fb[7] = 0xFF;

        Assert.Equal(0x42, fb[0]);
        Assert.Equal(0xFF, fb[7]);
    }

    [Fact]
    public void FixedBytes16_AsSpan_ReturnsCorrectLength()
    {
        var fb = new FixedBytes16();
        Assert.Equal(16, fb.AsSpan().Length);
    }

    [Fact]
    public void FixedBytes32_AsSpan_ReturnsCorrectLength()
    {
        var fb = new FixedBytes32();
        Assert.Equal(32, fb.AsSpan().Length);
    }

    [Fact]
    public void FixedBytes56_AsSpan_ReturnsCorrectLength()
    {
        var fb = new FixedBytes56();
        Assert.Equal(56, fb.AsSpan().Length);
    }

    [Fact]
    public void FixedBytes64_AsSpan_ReturnsCorrectLength()
    {
        var fb = new FixedBytes64();
        Assert.Equal(64, fb.AsSpan().Length);
    }

    [Fact]
    public void FixedBytes4_RoundTrip_WithByteBuffer()
    {
        var writer = new Nyar.Binary.Frame.ByteBufferWriter(32);

        var original = new FixedBytes4();
        original[0] = 0x89;
        original[1] = 0x50;
        original[2] = 0x4E;
        original[3] = 0x47;
        writer.Write(original.AsSpan());

        var reader = new Nyar.Binary.Frame.ByteBuffer(writer.WrittenData);
        var read = FixedBytes4.FromSpan(reader.ReadBytes(4));

        Assert.Equal(0x89, read[0]);
        Assert.Equal(0x50, read[1]);
        Assert.Equal(0x4E, read[2]);
        Assert.Equal(0x47, read[3]);
    }

    [Fact]
    public void FixedBytes16_FromSpan_CopiesAllData()
    {
        var data = new byte[16];
        for (var i = 0; i < 16; i++) data[i] = (byte)i;

        var fb = FixedBytes16.FromSpan(data);
        for (var i = 0; i < 16; i++)
        {
            Assert.Equal((byte)i, fb[i]);
        }
    }
}
