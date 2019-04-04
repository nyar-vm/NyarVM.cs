using Std.DataProcess.Deframe;
using Std.DataProcess.Enframe;
using Std.Memory;

namespace Std.Data.Protocol.Grpc;

/// <summary>
///     <see cref="IDeframer" /> �?gRPC 帧解帧实现�?/// 解析 gRPC 标准帧格式：1 字节压缩标志 + 4 字节大端序消息长�?+ 载荷�?/// 内部维护缓冲区以处理粘包/半包，使�?
///     <see cref="ArrayPool{T}" /> 管理内存�?///
/// </summary>
public sealed class GrpcDeframer : IDeframer
{
    private byte[] _buffer;
    private int _count;
    private int _current_frame_length;
    private int _current_frame_start;
    private bool _has_frame;

    /// <summary>
    ///     初始化一个新�?gRPC 解帧器实例�?    ///
    /// </summary>
    public GrpcDeframer()
    {
        _buffer = [];
        _count = 0;
    }

    /// <summary>
    ///     当前帧是否标记为已压缩�?    /// 实际解压由中间件层处理，解帧器只读取标志位�?    ///
    /// </summary>
    public bool current_compressed { get; private set; }

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

        var compressed = _buffer[0] != 0;
        var messageLength = (_buffer[1] << 24) | (_buffer[2] << 16) | (_buffer[3] << 8) | _buffer[4];

        if (messageLength < 0) throw new FramingException($"非法�?gRPC 消息长度: {messageLength}");

        var totalLength = 5 + messageLength;

        if (_count < totalLength)
        {
            _has_frame = false;
            return false;
        }

        current_compressed = compressed;
        _current_frame_start = 5;
        _current_frame_length = messageLength;

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