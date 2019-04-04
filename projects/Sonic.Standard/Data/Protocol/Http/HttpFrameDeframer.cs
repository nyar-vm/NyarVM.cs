using Std.DataProcess.Deframe;
using Std.Text;

namespace Std.Data.Protocol.Http;

/// <summary>
///     <see cref="IDeframer" /> �?HTTP 响应解帧实现�?/// 解析 HTTP/1.1 响应，通过 Content-Length 头提取载荷�?/// 内部维护缓冲区以处理粘包/半包�?///
/// </summary>
public sealed class HttpFrameDeframer : IDeframer
{
    private byte[] _buffer;
    private int _count;
    private int _current_frame_length;
    private int _current_frame_start;

    /// <summary>
    ///     初始化一个新�?HTTP 响应解帧器实例�?    ///
    /// </summary>
    public HttpFrameDeframer()
    {
        _buffer = [];
        _count = 0;
    }

    /// <inheritdoc />
    public void feed(ReadOnlySpan<byte> data)
    {
        ensure_capacity(_count + data.Length);
        data.CopyTo(_buffer.AsSpan(_count));
        _count += data.Length;
    }

    /// <inheritdoc />
    public bool try_get_next_frame()
    {
        var bufferSpan = _buffer.AsSpan(0, _count);
        var headerEndIndex = bufferSpan.IndexOf(new byte[] { 0x0D, 0x0A, 0x0D, 0x0A });

        if (headerEndIndex < 0) return false;

        // TODO: �?AsciiText 支持 ReadOnlySpan<byte> 构造后替换�?AsciiText
        var header = SonicEncoding.decode_ascii(bufferSpan[..headerEndIndex]);
        var contentLength = parse_content_length(header);

        if (contentLength < 0) return false;

        var bodyStart = headerEndIndex + 4;
        var totalExpectedLength = bodyStart + contentLength;

        if (_count < totalExpectedLength) return false;

        _current_frame_start = bodyStart;
        _current_frame_length = contentLength;

        var remaining = _count - totalExpectedLength;

        if (remaining > 0)
        {
            Array.Copy(_buffer, totalExpectedLength, _buffer, 0, remaining);
            _count = remaining;
        }
        else
        {
            _count = 0;
        }

        return true;
    }

    /// <inheritdoc />
    public ReadOnlySpan<byte> current_frame => _buffer.AsSpan(_current_frame_start, _current_frame_length);

    /// <inheritdoc />
    public ValueTask<bool> wait_for_frame(CancellationToken ct = default)
    {
        return ValueTask.FromResult(try_get_next_frame());
    }

    /// <inheritdoc />
    public void reset()
    {
        _count = 0;
        _buffer = [];
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _count = 0;
        _buffer = [];
    }

    /// <summary>
    ///     确保缓冲区至少具有指定容量，不足时按两倍扩容�?    ///
    /// </summary>
    /// <param name="requiredCapacity">所需的最小容量�?/param>
    private void ensure_capacity(int requiredCapacity)
    {
        if (_buffer.Length >= requiredCapacity) return;

        var newCapacity = _buffer.Length == 0 ? 64 : _buffer.Length * 2;

        if (newCapacity < requiredCapacity) newCapacity = requiredCapacity;

        var newBuffer = new byte[newCapacity];
        Array.Copy(_buffer, newBuffer, _count);
        _buffer = newBuffer;
    }

    /// <summary>
    ///     �?HTTP 响应头中解析 Content-Length 值�?    ///
    /// </summary>
    /// <param name="header">
    ///     HTTP 响应头字符串�?/param>
    ///     <returns>Content-Length 值，解析失败返回 -1�?/returns>
    private static int parse_content_length(string header)
    {
        var lines = header.Split("\r\n");

        foreach (var line in lines)
        {
            if (!line.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase)) continue;

            var valueStr = line["Content-Length:".Length..].Trim();

            if (int.TryParse(valueStr, out var value)) return value;
        }

        return -1;
    }
}