using Std.DataProcess.Decode;
using Std.DataProcess.Encode;
using Std.DataProcess.Write;
using Std.Text.Utf8;

namespace Sonic.Testing.Data.Encode;

/// <summary>
///     网络字节序（大端序）编解码器测试
/// </summary>
public class NetworkBinaryEncoderDecoderTests
{
    /// <summary>
    ///     测试 i32 编解码往返
    /// </summary>
    [Fact]
    public void encode_i32_decode_i32_RoundTrip()
    {
        var encoder = new NetworkEncoder();
        var decoder = new NetworkBinaryDecoder();
        var writer = new ArrayBufferWriter<byte>(16);

        encoder.encode_i32(0x01020304, writer);
        var result = decoder.decode_i32(writer.written_span);
        Assert.True(result.IsSuccess);
        Assert.Equal(0x01020304, result.Value);
        Assert.Equal(4, result.BytesConsumed);
    }

    /// <summary>
    ///     测试 i64 编解码往返
    /// </summary>
    [Fact]
    public void encode_i64_decode_i64_RoundTrip()
    {
        var encoder = new NetworkEncoder();
        var decoder = new NetworkBinaryDecoder();
        var writer = new ArrayBufferWriter<byte>(16);

        encoder.encode_i64(0x0102030405060708, writer);
        var result = decoder.decode_i64(writer.written_span);
        Assert.True(result.IsSuccess);
        Assert.Equal(0x0102030405060708, result.Value);
        Assert.Equal(8, result.BytesConsumed);
    }

    /// <summary>
    ///     测试 f32 编解码往返
    /// </summary>
    [Fact]
    public void encode_f32_decode_f32_RoundTrip()
    {
        var encoder = new NetworkEncoder();
        var decoder = new NetworkBinaryDecoder();
        var writer = new ArrayBufferWriter<byte>(16);

        encoder.encode_f32(3.14f, writer);
        var result = decoder.decode_f32(writer.written_span);
        Assert.True(result.IsSuccess);
        Assert.Equal(3.14f, result.Value, 0.001f);
        Assert.Equal(4, result.BytesConsumed);
    }

    /// <summary>
    ///     测试 f64 编解码往返
    /// </summary>
    [Fact]
    public void encode_f64_decode_f64_RoundTrip()
    {
        var encoder = new NetworkEncoder();
        var decoder = new NetworkBinaryDecoder();
        var writer = new ArrayBufferWriter<byte>(16);

        encoder.encode_f64(2.718281828, writer);
        var result = decoder.decode_f64(writer.written_span);
        Assert.True(result.IsSuccess);
        Assert.Equal(2.718281828, result.Value, 0.000000001);
        Assert.Equal(8, result.BytesConsumed);
    }

    /// <summary>
    ///     测试 bool 编解码往返
    /// </summary>
    [Fact]
    public void encode_bool_decode_bool_RoundTrip()
    {
        var encoder = new NetworkEncoder();
        var decoder = new NetworkBinaryDecoder();
        var writer = new ArrayBufferWriter<byte>(16);

        encoder.encode_bool(true, writer);
        var result = decoder.decode_bool(writer.written_span);
        Assert.True(result.IsSuccess);
        Assert.True(result.Value);
        Assert.Equal(1, result.BytesConsumed);
    }

    /// <summary>
    ///     测试 utf8 编解码往返
    /// </summary>
    [Fact]
    public void encode_utf8_decode_utf8_RoundTrip()
    {
        var encoder = new NetworkEncoder();
        var decoder = new NetworkBinaryDecoder();
        var writer = new ArrayBufferWriter<byte>(64);

        var text = Utf8Text.from_string("Hello, 世界!");
        encoder.encode_utf8(text, writer);
        var result = decoder.decode_utf8(writer.written_span);
        Assert.True(result.IsSuccess);
        Assert.Equal(text, result.Value);
        Assert.Equal(writer.written_span.Length, result.BytesConsumed);
    }
}

/// <summary>
///     小端序编解码器测试
/// </summary>
public class LittleEndianEncoderDecoderTests
{
    /// <summary>
    ///     测试 i32 编解码往返
    /// </summary>
    [Fact]
    public void encode_i32_decode_i32_RoundTrip()
    {
        var encoder = new LittleEndianEncoder();
        var decoder = new LittleEndianDecoder();
        var writer = new ArrayBufferWriter<byte>(16);

        encoder.encode_i32(0x04030201, writer);
        var result = decoder.decode_i32(writer.written_span);
        Assert.True(result.IsSuccess);
        Assert.Equal(0x04030201, result.Value);
        Assert.Equal(4, result.BytesConsumed);
    }

    /// <summary>
    ///     测试 i64 编解码往返
    /// </summary>
    [Fact]
    public void encode_i64_decode_i64_RoundTrip()
    {
        var encoder = new LittleEndianEncoder();
        var decoder = new LittleEndianDecoder();
        var writer = new ArrayBufferWriter<byte>(16);

        encoder.encode_i64(0x0807060504030201, writer);
        var result = decoder.decode_i64(writer.written_span);
        Assert.True(result.IsSuccess);
        Assert.Equal(0x0807060504030201, result.Value);
        Assert.Equal(8, result.BytesConsumed);
    }
}