using Std.DataProcess.Decode;
using Std.DataProcess.Enframe;
using Std.Math;
using Std.Memory;

namespace Std.DataProcess.Deframe;

/// <summary>
///     长度前缀解帧器，从字节流中提取带长度前缀的帧载荷�?/// 支持 1/2/4 字节的固定长度前缀�?LEB128 变长前缀�?/// LEB128 解码委托�?<see cref="Leb128Decoder" />�?///
/// </summary>
public sealed class LengthPrefixedDeframer : IDeframer
{
    private readonly int _prefix_size;
    private readonly bool _use_var_int;
    private readonly Leb128Decoder _var_int_decoder = new();
    private byte[] _buffer;
    private int _buffer_length;
    private int _consumed;
    private int _current_frame_length;
    private int _current_frame_start;
    private int _frame_length;
    private bool _frame_length_decoded;

    /// <summary>
    ///     使用固定长度前缀初始化解帧器�?    ///
    /// </summary>
    /// <param name="prefixSize">长度前缀的字节数�?�? �?4）�?/param>
    public LengthPrefixedDeframer(int prefixSize = 4)
    {
        if (prefixSize is not (1 or 2 or 4))
            throw new ArgumentOutOfRangeException(nameof(prefixSize), "长度前缀字节数必须为 1�? �?4");

        _prefix_size = prefixSize;
        _use_var_int = false;
        _buffer = [];
        _buffer_length = 0;
        _consumed = 0;
        _frame_length = 0;
        _frame_length_decoded = false;
    }

    /// <summary>
    ///     使用 LEB128 变长前缀初始化解帧器�?    ///
    /// </summary>
    /// <param name="useVarInt">必须�?<c>true</c>，表示使�?LEB128 变长前缀�?/param>
    public LengthPrefixedDeframer(bool useVarInt)
    {
        _prefix_size = 0;
        _use_var_int = useVarInt;
        _buffer = [];
        _buffer_length = 0;
        _consumed = 0;
        _frame_length = 0;
        _frame_length_decoded = false;
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
        var available = _buffer_length - _consumed;

        if (!_frame_length_decoded)
            if (!try_decode_frame_length(available))
                return false;

        if (available < _frame_length) return false;

        _current_frame_start = _consumed;
        _current_frame_length = _frame_length;
        _consumed += _frame_length;
        _frame_length_decoded = false;
        _frame_length = 0;

        if (_consumed > 0 && _consumed >= _buffer_length)
        {
            _buffer_length = 0;
            _consumed = 0;
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
        _consumed = 0;
        _frame_length = 0;
        _frame_length_decoded = false;
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
            Array.Copy(_buffer, _consumed, newBuffer, 0, _buffer_length - _consumed);
            _buffer_length -= _consumed;
            _consumed = 0;
        }

        if (_buffer.Length > 0) ArrayPool<byte>.shared.return_array(_buffer);

        _buffer = newBuffer;
    }

    /// <summary>
    ///     尝试从缓冲区解码帧长度�?    ///
    /// </summary>
    private bool try_decode_frame_length(int available)
    {
        if (_use_var_int) return try_decode_var_int_length(available);

        if (available < _prefix_size) return false;

        _frame_length = _prefix_size switch
        {
            1 => _buffer[_consumed],
            2 => (_buffer[_consumed] << 8) | _buffer[_consumed + 1],
            4 => (_buffer[_consumed] << 24) | (_buffer[_consumed + 1] << 16) | (_buffer[_consumed + 2] << 8) |
                 _buffer[_consumed + 3],
            _ => 0
        };

        _consumed += _prefix_size;
        _frame_length_decoded = true;

        if (_frame_length < 0) throw new FramingException($"帧长度为负数: {_frame_length}");

        return true;
    }

    /// <summary>
    ///     尝试使用 <see cref="Leb128Decoder" /> 解码 LEB128 变长帧长度�?    ///
    /// </summary>
    private bool try_decode_var_int_length(int available)
    {
        var buffer = _buffer.AsSpan(_consumed, available);
        var result = _var_int_decoder.decode_u64(buffer);

        if (!result.is_success) return false;

        _consumed += result.bytes_consumed;
        _frame_length = (int)result.value;
        _frame_length_decoded = true;

        if (_frame_length < 0) throw new FramingException($"帧长度为负数: {_frame_length}");

        return true;
    }
}