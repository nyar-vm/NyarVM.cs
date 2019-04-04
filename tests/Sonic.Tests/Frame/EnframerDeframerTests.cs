using System.Text;
using Std.Data.Protocol.Http;
using Std.DataProcess.Deframe;
using Std.DataProcess.Enframe;
using Std.DataProcess.Write;

namespace Sonic.Testing.Data.Frame;

/// <summary>
///     长度前缀封帧器测试
/// </summary>
public class LengthPrefixedEnframerTests
{
    /// <summary>
    ///     测试封帧与解帧往返
    /// </summary>
    [Fact]
    public void Frame_And_Deframe_RoundTrip()
    {
        var framer = new LengthPrefixedEnframer();
        var deframer = new LengthPrefixedDeframer();

        var payload = "Hello, World!"u8.ToArray();
        var writer = new ArrayBufferWriter<byte>(64);
        framer.enframe(payload, writer);

        deframer.feed([.. writer.written_span]);
        Assert.True(deframer.try_get_next_frame());
        var result = deframer.current_frame;
        Assert.True(payload.AsSpan().SequenceEqual(result));
    }

    /// <summary>
    ///     测试解帧器处理部分数据
    /// </summary>
    [Fact]
    public void Deframer_HandlesPartialData()
    {
        var framer = new LengthPrefixedEnframer();
        var deframer = new LengthPrefixedDeframer();

        var payload = "test"u8.ToArray();
        var writer = new ArrayBufferWriter<byte>(64);
        framer.enframe(payload, writer);
        var framed = writer.written_span.ToArray();

        deframer.feed([.. framed.AsSpan(0, 2)]);
        Assert.False(deframer.try_get_next_frame());

        deframer.feed([.. framed.AsSpan(2)]);
        Assert.True(deframer.try_get_next_frame());
        var result = deframer.current_frame;
        Assert.True(payload.AsSpan().SequenceEqual(result));
    }

    /// <summary>
    ///     测试解帧器处理多帧数据
    /// </summary>
    [Fact]
    public void Deframer_HandlesMultipleFrames()
    {
        var framer = new LengthPrefixedEnframer();
        var deframer = new LengthPrefixedDeframer();

        var payload1 = "first"u8.ToArray();
        var payload2 = "second"u8.ToArray();

        var writer1 = new ArrayBufferWriter<byte>(64);
        framer.enframe(payload1, writer1);
        deframer.feed([.. writer1.written_span]);

        var writer2 = new ArrayBufferWriter<byte>(64);
        framer.enframe(payload2, writer2);
        deframer.feed([.. writer2.written_span]);

        Assert.True(deframer.try_get_next_frame());
        var r1 = deframer.current_frame;
        Assert.True(payload1.AsSpan().SequenceEqual(r1));

        Assert.True(deframer.try_get_next_frame());
        var r2 = deframer.current_frame;
        Assert.True(payload2.AsSpan().SequenceEqual(r2));

        Assert.False(deframer.try_get_next_frame());
    }
}

/// <summary>
///     分隔符封帧器测试
/// </summary>
public class DelimiterEnframerTests
{
    /// <summary>
    ///     测试换行符封帧与解帧
    /// </summary>
    [Fact]
    public void Frame_And_Deframe_Newline()
    {
        var framer = new DelimiterEnframer([.. "\n"u8]);
        var deframer = new DelimiterDeframer([.. "\n"u8]);

        var payload = "Hello"u8.ToArray();
        var writer = new ArrayBufferWriter<byte>(64);
        framer.enframe(payload, writer);

        deframer.feed([.. writer.written_span]);
        Assert.True(deframer.try_get_next_frame());
        var result = deframer.current_frame;
        Assert.True(payload.AsSpan().SequenceEqual(result));
    }

    /// <summary>
    ///     测试解帧器处理多行数据
    /// </summary>
    [Fact]
    public void Deframer_HandlesMultipleLines()
    {
        var delimiter = "\n"u8.ToArray();
        var deframer = new DelimiterDeframer(delimiter);

        var input = "line1\nline2\nline3\n"u8.ToArray();
        deframer.feed(input);

        Assert.True(deframer.try_get_next_frame());
        var r1 = deframer.current_frame;
        Assert.True("line1"u8.SequenceEqual(r1));

        Assert.True(deframer.try_get_next_frame());
        var r2 = deframer.current_frame;
        Assert.True("line2"u8.SequenceEqual(r2));

        Assert.True(deframer.try_get_next_frame());
        var r3 = deframer.current_frame;
        Assert.True("line3"u8.SequenceEqual(r3));

        Assert.False(deframer.try_get_next_frame());
    }
}

/// <summary>
///     固定长度解帧器测试
/// </summary>
public class FixedLengthDeframerTests
{
    /// <summary>
    ///     测试解帧器提取固定长度帧
    /// </summary>
    [Fact]
    public void Deframer_ExtractsFixedFrames()
    {
        var deframer = new FixedLengthDeframer(4);

        var input = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
        deframer.feed(input);

        Assert.True(deframer.try_get_next_frame());
        var r1 = deframer.current_frame;
        Assert.Equal(4, r1.Length);
        Assert.Equal(1, r1[0]);

        Assert.True(deframer.try_get_next_frame());
        var r2 = deframer.current_frame;
        Assert.Equal(4, r2.Length);
        Assert.Equal(5, r2[0]);

        Assert.False(deframer.try_get_next_frame());
    }
}

/// <summary>
///     HTTP 帧封帧与解帧测试
/// </summary>
public class HttpFrameTests
{
    /// <summary>
    ///     测试 HTTP 帧封装包含正确头部
    /// </summary>
    [Fact]
    public void Frame_WrapsWithHttpHeader()
    {
        var framer = new HttpFrameEnframer();
        var payload = "Hello"u8.ToArray();
        var writer = new ArrayBufferWriter<byte>(64);
        framer.enframe(payload, writer);

        var result = writer.written_span;
        var asString = Encoding.UTF8.GetString(result);
        Assert.Contains("HTTP/1.1 200 OK", asString);
        Assert.Contains("Content-Length: 5", asString);
    }

    /// <summary>
    ///     测试 HTTP 帧解封装提取载荷
    /// </summary>
    [Fact]
    public void Unframe_ExtractsPayload()
    {
        var framer = new HttpFrameEnframer();
        var unframer = new HttpFrameDeframer();
        var payload = "Hello"u8.ToArray();
        var writer = new ArrayBufferWriter<byte>(64);
        framer.enframe(payload, writer);

        unframer.feed(writer.written_span);
        Assert.True(unframer.try_get_next_frame());
        var result = unframer.current_frame;
        Assert.True(payload.AsSpan().SequenceEqual(result));
    }
}