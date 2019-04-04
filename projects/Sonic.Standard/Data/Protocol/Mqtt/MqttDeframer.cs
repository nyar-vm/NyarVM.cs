using Std.DataProcess.Deframe;
using Std.DataProcess.Enframe;
using Std.Memory;

namespace Std.Data.Protocol.Mqtt;

/// <summary>
///     <see cref="IDeframer" /> �?MQTT 3.1.1/5.0 数据包解帧实现�?/// 解析 MQTT 数据包格式：1 字节固定�?+ 变长剩余长度（最�?4 字节 LEB128�?+ 载荷�?///
///     内部维护缓冲区以处理粘包/半包，使�?<see cref="ArrayPool{T}" /> 管理内存�?///
/// </summary>
public sealed class MqttDeframer : IDeframer
{
    private byte[] _buffer;
    private int _count;
    private int _current_frame_length;
    private int _current_frame_start;
    private bool _has_frame;

    /// <summary>
    ///     初始化一个新�?MQTT 解帧器实例�?    ///
    /// </summary>
    public MqttDeframer()
    {
        _buffer = [];
        _count = 0;
    }

    /// <summary>
    ///     当前数据包的控制包类型（1-15）�?    /// 1=CONNECT, 2=CONNACK, 3=PUBLISH, 4=PUBACK, 5=PUBREC, 6=PUBREL, 7=PUBCOMP,
    ///     8=SUBSCRIBE, 9=SUBACK, 10=UNSUBSCRIBE, 11=UNSUBACK, 12=PINGREQ, 13=PINGRESP,
    ///     14=DISCONNECT, 15=AUTH�?    ///
    /// </summary>
    public int current_packet_type { get; private set; }

    /// <summary>
    ///     当前数据包固定头�?4 位标志�?    ///
    /// </summary>
    public int current_flags { get; private set; }

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
        if (_count < 2)
        {
            _has_frame = false;
            return false;
        }

        var firstByte = _buffer[0];
        current_packet_type = (firstByte >> 4) & 0x0F;
        current_flags = firstByte & 0x0F;

        var multiplier = 1;
        var remainingLength = 0;
        var offset = 1;

        while (offset < _count)
        {
            var encodedByte = _buffer[offset];
            offset++;

            remainingLength += (encodedByte & 0x7F) * multiplier;

            if ((encodedByte & 0x80) == 0) break;

            multiplier *= 128;

            if (multiplier > 128 * 128 * 128) throw new FramingException("MQTT 变长整数编码超过 4 字节");
        }

        if (offset > _count)
        {
            _has_frame = false;
            return false;
        }

        var totalLength = offset + remainingLength;

        if (_count < totalLength)
        {
            _has_frame = false;
            return false;
        }

        _current_frame_start = offset;
        _current_frame_length = remainingLength;

        var remaining = _count - totalLength;

        if (remaining > 0)
        {
            var temp = new byte[_current_frame_length];
            Array.Copy(_buffer, _current_frame_start, temp, 0, _current_frame_length);
            Array.Copy(_buffer, totalLength, _buffer, 0, remaining);
            Array.Copy(temp, 0, _buffer, 0, temp.Length);
            _current_frame_start = 0;
            _count = remaining + _current_frame_length;
        }
        else
        {
            _current_frame_start = offset;
            _count = totalLength;
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