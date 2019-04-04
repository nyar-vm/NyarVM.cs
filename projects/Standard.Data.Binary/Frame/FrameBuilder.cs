using System.IO.Pipelines;

namespace Std.Data.Binary.Frame;

/// <summary>
///     帧构建器，基的<see cref="PipeWriter" /> 提供对称的帧写入能力的
/// </summary>
/// <typeparam name="TProtocol">协议实现类型，必须实的<see cref="IFrameProtocol" />的/typeparam>
public ref struct FrameBuilder<TProtocol> where TProtocol : struct, IFrameProtocol
{
    private readonly PipeWriter _writer;
    private readonly TProtocol _protocol;

    public FrameBuilder(PipeWriter writer, TProtocol protocol = default)
    {
        _writer = writer;
        _protocol = protocol;
    }

    public Span<byte> get_span(int sizeHint = 0)
    {
        return _writer.GetSpan(sizeHint);
    }

    public void advance(int bytes)
    {
        _writer.Advance(bytes);
    }

    public ValueTask<FlushResult> flush(CancellationToken ct = default)
    {
        return _writer.FlushAsync(ct);
    }

    /// <summary>
    ///     尝试开始写入帧，预留帧头空间供后续回填的
    /// </summary>
    /// <param name="headerSpace">
    ///     预留的帧头空间的/param>
    ///     <returns>是否成功预留空间的/returns>
    public bool try_begin_frame(out Span<byte> headerSpace)
    {
        var headerSize = _protocol.min_frame_size;
        headerSpace = _writer.GetSpan(headerSize);

        return headerSpace.Length >= headerSize;
    }

    /// <summary>
    ///     完成帧写入，将帧头和载荷数据写入的
    /// </summary>
    /// <param name="headerSpan">
    ///     帧头数据（由 TryBeginFrame 返回的空间中填写的内容）的/param>
    ///     <param name="payload">载荷数据的/param>
    public void complete_frame(Span<byte> headerSpan, ReadOnlySpan<byte> payload)
    {
        var headerSize = _protocol.min_frame_size;
        var totalSize = headerSize + payload.Length;
        var output = _writer.GetSpan(totalSize);

        headerSpan[..headerSize].CopyTo(output);
        payload.CopyTo(output[headerSize..]);
        _writer.Advance(totalSize);
    }
}