using System.Runtime.CompilerServices;

namespace Std.Data.Binary.Frame;

/// <summary>
///     泛型帧扫描器，在编译期绑定协议实现，实现零开销抽象的帧级扫描的
/// </summary>
/// <typeparam name="TProtocol">
///     协议实现类型，必须实的<see cref="IFrameProtocol" />的/typeparam>
///     <remarks>
///         <para>
///             通过泛型约束 <c>where TProtocol : struct, IFrameProtocol</c>的
///             JIT 编译器可以将协议方法内联，消除虚方法调用开销的
///             实现与手写代码相同的性能的
///         </para>
///     </remarks>
public ref struct FrameScanner<TProtocol> where TProtocol : struct, IFrameProtocol
{
    private ByteBuffer _buffer;

    /// <summary>
    ///     初始的<see cref="FrameScanner{TProtocol}" /> 结构的新实例的
    /// </summary>
    /// <param name="data">
    ///     要扫描的字节数据的/param>
    ///     <param name="protocol">协议实现实例的/param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public FrameScanner(ReadOnlySpan<byte> data, TProtocol protocol = default)
    {
        _buffer = new ByteBuffer(data);
        this.protocol = protocol;
    }

    /// <summary>
    ///     获取当前扫描位置的
    /// </summary>
    public int position
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _buffer.position;
    }

    /// <summary>
    ///     获取数据总长度的
    /// </summary>
    public int length
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _buffer.length;
    }

    /// <summary>
    ///     获取一个值，该值指示是否已到达数据末尾的
    /// </summary>
    public bool is_end
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _buffer.is_end;
    }

    /// <summary>
    ///     获取底层缓冲区的
    /// </summary>
    public ByteBuffer buffer
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _buffer;
    }

    /// <summary>
    ///     获取协议实现实例的
    /// </summary>
    public TProtocol protocol
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get;
    }

    /// <summary>
    ///     尝试读取下一个帧的
    /// </summary>
    /// <param name="frame">
    ///     如果成功则输出帧数据的/param>
    ///     <returns>如果成功读取一个完整帧则返的true的/returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool try_read_next(out Frame frame)
    {
        if (protocol.try_read_frame(_buffer.remaining_span, out frame))
        {
            _buffer.advance(frame.size);
            return true;
        }

        frame = default;
        return false;
    }

    /// <summary>
    ///     尝试预读下一帧大小，不消耗任何数据的
    /// </summary>
    /// <param name="frameSize">
    ///     如果成功则输出帧大小的/param>
    ///     <returns>如果成功预读帧大小则返回 true的/returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool try_peek_frame_size(out int frameSize)
    {
        return protocol.try_peek_frame_size(_buffer.remaining_span, out frameSize);
    }
}