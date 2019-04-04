using Std.DataProcess.Deframe;
using Std.DataProcess.Enframe;
using Std.Memory;
using Std.Text;

namespace Std.Data.Protocol.Redis;

/// <summary>
///     <see cref="IDeframer" /> �?Redis RESP 协议解帧实现�?/// 从字节流中解�?RESP 协议消息，支�?Simple String、Error、Integer、Bulk String
///     �?Array 类型�?/// 内部维护缓冲区以处理粘包/半包，使�?<see cref="ArrayPool{T}" /> 管理内存�?///
/// </summary>
public sealed class RedisDeframer : IDeframer
{
    private byte[] _buffer;
    private int _count;
    private int _current_frame_length;
    private int _current_frame_start;
    private bool _has_frame;

    /// <summary>
    ///     初始化一个新�?Redis RESP 解帧器实例�?    ///
    /// </summary>
    public RedisDeframer()
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
        if (_count == 0)
        {
            _has_frame = false;
            return false;
        }

        var bufferSpan = _buffer.AsSpan(0, _count);
        var crlfIndex = bufferSpan.IndexOf(new byte[] { 0x0D, 0x0A });

        if (crlfIndex < 0)
        {
            _has_frame = false;
            return false;
        }

        var firstByte = bufferSpan[0];
        var consumed = 0;

        switch (firstByte)
        {
            case (byte)'+':
            case (byte)'-':
            case (byte)':':
            {
                consumed = crlfIndex + 2;
                _current_frame_start = 0;
                _current_frame_length = consumed;
                break;
            }
            case (byte)'$':
            {
                var lengthStr = SonicEncoding.decode_ascii(bufferSpan[1..crlfIndex]);

                if (!int.TryParse(lengthStr, out var bulkLength))
                    throw new FramingException($"非法�?RESP Bulk String 长度: {lengthStr}");

                if (bulkLength == -1)
                {
                    consumed = crlfIndex + 2;
                    _current_frame_start = 0;
                    _current_frame_length = consumed;
                    break;
                }

                var dataStart = crlfIndex + 2;

                if (_count < dataStart + bulkLength + 2)
                {
                    _has_frame = false;
                    return false;
                }

                consumed = dataStart + bulkLength + 2;
                _current_frame_start = 0;
                _current_frame_length = consumed;
                break;
            }
            case (byte)'*':
            {
                var countStr = SonicEncoding.decode_ascii(bufferSpan[1..crlfIndex]);

                if (!int.TryParse(countStr, out var elementCount))
                    throw new FramingException($"非法�?RESP Array 数量: {countStr}");

                if (elementCount == -1)
                {
                    consumed = crlfIndex + 2;
                    _current_frame_start = 0;
                    _current_frame_length = consumed;
                    break;
                }

                var offset = crlfIndex + 2;

                for (var i = 0; i < elementCount; i++)
                {
                    if (offset >= _count)
                    {
                        _has_frame = false;
                        return false;
                    }

                    var elemCrlf = bufferSpan[offset..].IndexOf(new byte[] { 0x0D, 0x0A });

                    if (elemCrlf < 0)
                    {
                        _has_frame = false;
                        return false;
                    }

                    if (bufferSpan[offset] == (byte)'$')
                    {
                        var elemLengthStr = SonicEncoding.decode_ascii(bufferSpan.Slice(offset + 1, elemCrlf - 1));

                        if (!int.TryParse(elemLengthStr, out var elemLength))
                            throw new FramingException($"非法�?RESP Bulk String 长度: {elemLengthStr}");

                        if (elemLength == -1)
                        {
                            offset += elemCrlf + 2;
                        }
                        else
                        {
                            var elemDataStart = offset + elemCrlf + 2;

                            if (_count < elemDataStart + elemLength + 2)
                            {
                                _has_frame = false;
                                return false;
                            }

                            offset = elemDataStart + elemLength + 2;
                        }
                    }
                    else
                    {
                        offset += elemCrlf + 2;
                    }
                }

                consumed = offset;
                _current_frame_start = 0;
                _current_frame_length = consumed;
                break;
            }
            default:
            {
                throw new FramingException($"未知�?RESP 消息类型: {(char)firstByte}");
            }
        }

        var remaining = _count - consumed;

        if (remaining > 0)
        {
            Array.Copy(_buffer, consumed, _buffer, 0, remaining);
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