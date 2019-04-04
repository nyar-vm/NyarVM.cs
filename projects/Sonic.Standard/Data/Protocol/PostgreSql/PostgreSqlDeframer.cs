using Std.DataProcess.Deframe;
using Std.DataProcess.Enframe;
using Std.Memory;

namespace Std.Data.Protocol.PostgreSql;

/// <summary>
///     <see cref="IDeframer" /> �?PostgreSQL 前端/后端协议消息解帧实现�?/// 解析 PostgreSQL 消息格式�? 字节消息类型 + 4 字节大端序长度（含自身） + 载荷�?/// 支持
///     Startup 消息（首字节�?<c>\0</c>）和常规消息�?/// 内部维护缓冲区以处理粘包/半包，使�?<see cref="ArrayPool{T}" /> 管理内存�?///
/// </summary>
public sealed class PostgreSqlDeframer : IDeframer
{
    private byte[] _buffer;
    private int _count;
    private int _current_frame_length;
    private int _current_frame_start;
    private bool _has_frame;

    /// <summary>
    ///     初始化一个新�?PostgreSQL 消息解帧器实例�?    ///
    /// </summary>
    public PostgreSqlDeframer()
    {
        _buffer = [];
        _count = 0;
    }

    /// <summary>
    ///     当前消息的类型字节。Startup 消息�?<c>0x00</c>，普通消息如 <c>'Q'</c>（查询）�?c>'P'</c>（解析）等�?    ///
    /// </summary>
    public byte current_message_type { get; private set; }

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
        if (_count < 5)
        {
            _has_frame = false;
            return false;
        }

        var messageType = _buffer[0];
        var length = (_buffer[1] << 24) | (_buffer[2] << 16) | (_buffer[3] << 8) | _buffer[4];

        if (length < 4) throw new FramingException($"非法 PG 消息长度: {length}（最小为 4）。");

        var totalLength = 1 + length;

        if (_count < totalLength)
        {
            _has_frame = false;
            return false;
        }

        current_message_type = messageType;
        _current_frame_start = 5;
        _current_frame_length = length - 4;

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

        var newCapacity = _buffer.Length == 0 ? 256 : _buffer.Length * 2;

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