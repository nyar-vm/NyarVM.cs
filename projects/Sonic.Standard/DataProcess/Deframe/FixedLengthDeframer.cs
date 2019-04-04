using Std.Math;
using Std.Memory;

namespace Std.DataProcess.Deframe;

/// <summary>
///     固定长度解帧器，按固定字节块大小切分字节流。
///     适用于传感器数据记录等固定长度帧场景。
/// </summary>
public sealed class FixedLengthDeframer : IDeframer
{
    private readonly int _frame_size;
    private byte[] _buffer;
    private int _buffer_length;
    private int _consumed;
    private int _current_frame_length;
    private int _current_frame_start;

    /// <summary>
    ///     使用指定帧大小初始化解帧器。
    /// </summary>
    /// <param name="frameSize">每帧的固定字节数。</param>
    public FixedLengthDeframer(int frameSize)
    {
        if (frameSize <= 0) throw new ArgumentOutOfRangeException(nameof(frameSize), "帧大小必须为正整数。");

        _frame_size = frameSize;
        _buffer = [];
        _buffer_length = 0;
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
        var available = _buffer_length - _consumed;

        if (available < _frame_size) return false;

        _current_frame_start = _consumed;
        _current_frame_length = _frame_size;
        _consumed += _frame_size;

        if (_consumed >= _buffer_length)
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
}