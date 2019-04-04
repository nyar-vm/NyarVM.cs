using Std.DataProcess.Deframe;
using Std.Memory;

namespace Std.Data.Protocol.MySql;

/// <summary>
///     <see cref="IDeframer" /> �?MySQL 客户�?服务器协议数据包解帧实现�?/// 解析 MySQL 数据包格式：3 字节小端序载荷长�?+ 1 字节序列�?+ 载荷�?///
///     内部维护缓冲区以处理粘包/半包，使�?<see cref="ArrayPool{T}" /> 管理内存�?///
/// </summary>
public sealed class MySqlPacketDeframer : IDeframer
{
    private byte[] _buffer;
    private int _count;
    private int _current_frame_length;
    private int _current_frame_start;
    private bool _has_frame;

    /// <summary>
    ///     初始化一个新�?MySQL 数据包解帧器实例�?    ///
    /// </summary>
    public MySqlPacketDeframer()
    {
        _buffer = [];
        _count = 0;
    }

    /// <summary>
    ///     当前数据包的序列号�?    ///
    /// </summary>
    public byte current_sequence { get; private set; }

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
        if (_count < 4)
        {
            _has_frame = false;
            return false;
        }

        var payloadLength = _buffer[0] | (_buffer[1] << 8) | (_buffer[2] << 16);
        var totalLength = 4 + payloadLength;

        if (_count < totalLength)
        {
            _has_frame = false;
            return false;
        }

        current_sequence = _buffer[3];
        _current_frame_start = 4;
        _current_frame_length = payloadLength;

        var remaining = _count - totalLength;

        if (remaining > 0)
        {
            Array.Copy(_buffer, totalLength, _buffer, 0, remaining);
            _count = remaining;
        }
        else
        {
            _count = 0;
        }

        _has_frame = true;
        return true;
    }

    /// <inheritdoc />
    public ReadOnlySpan<byte> current_frame
    {
        get
        {
            if (!_has_frame) return ReadOnlySpan<byte>.Empty;

            return _buffer.AsSpan(_current_frame_start, _current_frame_length);
        }
    }

    /// <inheritdoc />
    public ValueTask<bool> wait_for_frame(CancellationToken ct = default)
    {
        return ValueTask.FromResult(try_get_next_frame());
    }

    /// <inheritdoc />
    public void reset()
    {
        return_buffer();
        _buffer = [];
        _count = 0;
        _has_frame = false;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        reset();
    }

    private void ensure_capacity(int requiredCapacity)
    {
        if (_buffer.Length >= requiredCapacity) return;

        var newCapacity = _buffer.Length == 0 ? 64 : _buffer.Length * 2;

        if (newCapacity < requiredCapacity) newCapacity = requiredCapacity;

        var newBuffer = ArrayPool<byte>.shared.rent_array(newCapacity);
        Array.Copy(_buffer, newBuffer, _count);
        return_buffer();
        _buffer = newBuffer;
    }

    private void return_buffer()
    {
        if (_buffer.Length > 0) ArrayPool<byte>.shared.return_array(_buffer);
    }
}