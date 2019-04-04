using Std.DataProcess.Deframe;
using Std.Memory;

namespace Std.Data.Protocol.WebSocket;

/// <summary>
///     <see cref="IDeframer" /> �?WebSocket 帧解帧实现（RFC 6455）�?/// 解析 WebSocket 帧格式，支持文本帧、二进制帧、关闭帧、Ping/Pong 帧�?///
///     服务端模式自动处理客户端掩码（unmask）�?/// 内部维护缓冲区以处理粘包/半包，使�?<see cref="ArrayPool{T}" /> 管理内存�?///
/// </summary>
public sealed class WebSocketDeframer : IDeframer
{
    private readonly bool _is_server;
    private byte[] _buffer;
    private int _count;
    private int _current_frame_length;
    private int _current_frame_start;
    private bool _has_frame;

    /// <summary>
    ///     初始化一个新�?WebSocket 解帧器实例�?    ///
    /// </summary>
    /// <param name="isServer">是否为服务端模式。服务端模式下自动对客户端帧执行 unmask�?/param>
    public WebSocketDeframer(bool isServer = true)
    {
        _is_server = isServer;
        _buffer = [];
        _count = 0;
    }

    /// <summary>
    ///     当前帧的操作码�?    /// 0x00=continuation, 0x01=text, 0x02=binary, 0x08=close, 0x09=ping, 0x0A=pong�?    ///
    /// </summary>
    public int current_opcode { get; private set; }

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
        var secondByte = _buffer[1];
        var opcode = firstByte & 0x0F;
        var isMasked = (secondByte & 0x80) != 0;
        var payloadLength = (long)(secondByte & 0x7F);

        var headerSize = 2;

        if (payloadLength == 126)
        {
            if (_count < 4)
            {
                _has_frame = false;
                return false;
            }

            payloadLength = (_buffer[2] << 8) | _buffer[3];
            headerSize = 4;
        }
        else if (payloadLength == 127)
        {
            if (_count < 10)
            {
                _has_frame = false;
                return false;
            }

            payloadLength =
                ((long)_buffer[2] << 56) | ((long)_buffer[3] << 48) |
                ((long)_buffer[4] << 40) | ((long)_buffer[5] << 32) |
                ((long)_buffer[6] << 24) | ((long)_buffer[7] << 16) |
                ((long)_buffer[8] << 8) | _buffer[9];
            headerSize = 10;
        }

        var maskSize = isMasked ? 4 : 0;
        var totalLength = headerSize + maskSize + (int)payloadLength;

        if (_count < totalLength)
        {
            _has_frame = false;
            return false;
        }

        var maskOffset = headerSize;

        if (isMasked && _is_server)
        {
            var maskKey = new byte[4];
            Array.Copy(_buffer, maskOffset, maskKey, 0, 4);

            var dataOffset = maskOffset + 4;

            for (var i = 0; i < payloadLength; i++) _buffer[dataOffset + i] ^= maskKey[i % 4];
        }

        current_opcode = opcode;
        _current_frame_start = headerSize + maskSize;
        _current_frame_length = (int)payloadLength;

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
            _current_frame_start = headerSize + maskSize;
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