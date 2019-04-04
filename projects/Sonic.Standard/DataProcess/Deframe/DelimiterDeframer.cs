using Std.Math;
using Std.Memory;
using Std.Text;

namespace Std.DataProcess.Deframe;

/// <summary>
///     分隔符解帧器，按特定字节序列（如 <c>\n</c>�?c>\r\n\r\n</c>）拆分字节流�?/// 适用�?RESP、NDJSON、HTTP 头部等文本协议�?///
/// </summary>
public sealed class DelimiterDeframer : IDeframer
{
    private readonly byte[] _delimiter;
    private byte[] _buffer;
    private int _buffer_length;
    private int _consumed;
    private int _current_frame_length;
    private int _current_frame_start;
    private int _search_offset;

    /// <summary>
    ///     使用指定分隔符初始化解帧器�?    ///
    /// </summary>
    /// <param name="delimiter">帧分隔符的字节序列�?/param>
    public DelimiterDeframer(ReadOnlySpan<byte> delimiter)
    {
        _delimiter = [.. delimiter];
        _buffer = [];
        _buffer_length = 0;
        _search_offset = 0;
        _consumed = 0;
    }

    /// <summary>
    ///     使用指定字符串作为分隔符初始化解帧器（UTF-8 编码）�?    ///
    /// </summary>
    /// <param name="delimiter">帧分隔符字符串�?/param>
    public DelimiterDeframer(string delimiter)
    {
        _delimiter = SonicEncoding.encode_utf8(delimiter);
        _buffer = [];
        _buffer_length = 0;
        _search_offset = 0;
        _consumed = 0;
    }

    /// <inheritdoc />
    public void feed(ReadOnlySpan<byte> data)
    {
        ensure_capacity(_buffer_length + data.Length);
        data.CopyTo(_buffer.AsSpan(_buffer_length));
        _buffer_length += data.Length;
    }

    /// <inheritdoc />
    public bool try_get_next_frame()
    {
        if (_delimiter.Length == 0) return false;

        var available = _buffer_length - _search_offset;

        if (available < _delimiter.Length) return false;

        var searchSpan = _buffer.AsSpan(_search_offset, _buffer_length - _search_offset);
        var index = index_of(searchSpan, _delimiter);

        if (index < 0)
        {
            _search_offset = _buffer_length - _delimiter.Length + 1;

            if (_search_offset < _consumed) _search_offset = _consumed;

            return false;
        }

        var frameStart = _search_offset;
        var frameLength = index;
        _current_frame_start = frameStart;
        _current_frame_length = frameLength;

        var totalConsumed = frameStart + frameLength + _delimiter.Length;
        _consumed = totalConsumed;
        _search_offset = totalConsumed;

        if (_consumed >= _buffer_length)
        {
            _buffer_length = 0;
            _consumed = 0;
            _search_offset = 0;
        }

        return true;
    }

    /// <inheritdoc />
    public ReadOnlySpan<byte> current_frame => _buffer.AsSpan(_current_frame_start, _current_frame_length);

    /// <inheritdoc />
    public ValueTask<bool> wait_for_frame(CancellationToken cancellationToken = default)
    {
        return new ValueTask<bool>(try_get_next_frame());
    }

    /// <inheritdoc />
    public void reset()
    {
        _buffer_length = 0;
        _search_offset = 0;
        _consumed = 0;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        reset();
    }

    /// <summary>
    ///     确保缓冲区有足够的容量�?    ///
    /// </summary>
    private void ensure_capacity(int needed)
    {
        if (_buffer.Length >= needed) return;

        var newCapacity = SonicMath.max(_buffer.Length * 2, needed);
        var newBuffer = ArrayPool<byte>.shared.rent_array(newCapacity);

        if (_buffer_length > 0)
        {
            var copyStart = SonicMath.min(_consumed, _search_offset);
            var copyLength = _buffer_length - copyStart;
            Array.Copy(_buffer, copyStart, newBuffer, 0, copyLength);
            _buffer_length = copyLength;
            _search_offset -= copyStart;
            _consumed -= copyStart;
        }

        if (_buffer.Length > 0) ArrayPool<byte>.shared.return_array(_buffer);

        _buffer = newBuffer;
    }

    /// <summary>
    ///     在字节序列中查找分隔符的首次出现位置�?    ///
    /// </summary>
    private static int index_of(ReadOnlySpan<byte> span, ReadOnlySpan<byte> delimiter)
    {
        if (delimiter.Length > span.Length) return -1;

        for (var i = 0; i <= span.Length - delimiter.Length; i++)
        {
            var slice = span.Slice(i, delimiter.Length);

            if (slice.SequenceEqual(delimiter)) return i;
        }

        return -1;
    }
}