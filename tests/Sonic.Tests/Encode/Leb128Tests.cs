using Std.DataProcess.Decode;
using Std.DataProcess.Encode;
using Std.DataProcess.Write;

namespace Sonic.Testing.Data.Encode;

/// <summary>
///     LEB128 编码器测试
/// </summary>
public class Leb128EncoderTests
{
    /// <summary>
    ///     测试编码零值
    /// </summary>
    [Fact]
    public void encode_i32_EncodesZero()
    {
        var encoder = new Leb128Encoder();
        var writer = new ArrayBufferWriter<byte>(16);
        encoder.encode_i32(0, writer);

        var written = writer.written_span;
        Assert.Equal(1, written.Length);
        Assert.Equal(0, written[0]);
    }

    /// <summary>
    ///     测试编码单字节值
    /// </summary>
    [Fact]
    public void encode_i32_EncodesSingleByte()
    {
        var encoder = new Leb128Encoder();
        var writer = new ArrayBufferWriter<byte>(16);
        encoder.encode_i32(127, writer);

        var written = writer.written_span;
        Assert.Equal(1, written.Length);
        Assert.Equal(127, written[0]);
    }

    /// <summary>
    ///     测试编码双字节值
    /// </summary>
    [Fact]
    public void encode_i32_EncodesTwoBytes()
    {
        var encoder = new Leb128Encoder();
        var writer = new ArrayBufferWriter<byte>(16);
        encoder.encode_i32(128, writer);

        var written = writer.written_span;
        Assert.Equal(2, written.Length);
        Assert.Equal(0x80, written[0] & 0x80);
    }

    /// <summary>
    ///     测试编码 u64 最大值
    /// </summary>
    [Fact]
    public void encode_u64_EncodesMaxValue()
    {
        var encoder = new Leb128Encoder();
        var writer = new ArrayBufferWriter<byte>(16);
        encoder.encode_u64(ulong.MaxValue, writer);

        var written = writer.written_span;
        Assert.Equal(10, written.Length);
    }

    /// <summary>
    ///     测试编码负数
    /// </summary>
    [Fact]
    public void encode_i64_EncodesNegative()
    {
        var encoder = new Leb128Encoder();
        var writer = new ArrayBufferWriter<byte>(16);
        encoder.encode_i64(-1, writer);

        var written = writer.written_span;
        Assert.Equal(10, written.Length);
        Assert.Equal(0xFF, written[0]);
    }
}

/// <summary>
///     LEB128 解码器测试
/// </summary>
public class Leb128DecoderTests
{
    /// <summary>
    ///     测试解码零值
    /// </summary>
    [Fact]
    public void decode_i32_DecodesZero()
    {
        var decoder = new Leb128Decoder();
        var bytes = new byte[] { 0 };
        var result = decoder.decode_i32(bytes);
        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value);
        Assert.Equal(1, result.BytesConsumed);
    }

    /// <summary>
    ///     测试解码单字节值
    /// </summary>
    [Fact]
    public void decode_i32_DecodesSingleByte()
    {
        var decoder = new Leb128Decoder();
        var bytes = new byte[] { 42 };
        var result = decoder.decode_i32(bytes);
        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
        Assert.Equal(1, result.BytesConsumed);
    }

    /// <summary>
    ///     测试解码双字节值
    /// </summary>
    [Fact]
    public void decode_u64_DecodesTwoBytes()
    {
        var decoder = new Leb128Decoder();
        var bytes = new byte[] { 0x80, 0x01 };
        var result = decoder.decode_u64(bytes);
        Assert.True(result.IsSuccess);
        Assert.Equal(128UL, result.Value);
        Assert.Equal(2, result.BytesConsumed);
    }

    /// <summary>
    ///     测试 i32 编解码往返
    /// </summary>
    [Fact]
    public void decode_i32_RoundTrip()
    {
        var encoder = new Leb128Encoder();
        var decoder = new Leb128Decoder();
        var writer = new ArrayBufferWriter<byte>(16);

        encoder.encode_i32(12345, writer);
        var result = decoder.decode_i32(writer.written_span);
        Assert.True(result.IsSuccess);
        Assert.Equal(12345, result.Value);
        Assert.Equal(writer.written_span.Length, result.BytesConsumed);
    }

    /// <summary>
    ///     测试 u64 编解码往返
    /// </summary>
    [Fact]
    public void decode_u64_RoundTrip()
    {
        var encoder = new Leb128Encoder();
        var decoder = new Leb128Decoder();
        var writer = new ArrayBufferWriter<byte>(16);

        encoder.encode_u64(9876543210UL, writer);
        var result = decoder.decode_u64(writer.written_span);
        Assert.True(result.IsSuccess);
        Assert.Equal(9876543210UL, result.Value);
        Assert.Equal(writer.written_span.Length, result.BytesConsumed);
    }
}