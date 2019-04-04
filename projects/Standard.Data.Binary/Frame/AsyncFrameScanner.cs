using System.Buffers;
using System.IO.Pipelines;

namespace Std.Data.Binary.Frame;

/// <summary>
///     异步帧扫描器，基的<see cref="PipeReader" /> 提供异步的帧级扫描能力的
/// </summary>
/// <typeparam name="TProtocol">协议实现类型，必须实的<see cref="IFrameProtocol" />的/typeparam>
public ref struct AsyncFrameScanner<TProtocol> where TProtocol : struct, IFrameProtocol
{
    private readonly PipeReader _reader;
    private readonly TProtocol _protocol;
    private ReadOnlySequence<byte> _buffer;

    public AsyncFrameScanner(PipeReader reader, TProtocol protocol = default)
    {
        _reader = reader;
        _protocol = protocol;
        _buffer = ReadOnlySequence<byte>.Empty;
    }

    public ValueTask<ReadResult> read_next(CancellationToken ct = default)
    {
        return _reader.ReadAsync(ct);
    }

    public bool try_get_next_frame(out Frame frame)
    {
        frame = default;

        if (_buffer.IsEmpty) return false;

        var span = _buffer.IsSingleSegment
            ? _buffer.FirstSpan
            : _buffer.ToArray();

        if (!_protocol.try_read_frame(span, out frame)) return false;

        _buffer = _buffer.Slice(frame.size);
        return true;
    }

    public void advance_to(SequencePosition consumed, SequencePosition examined)
    {
        _reader.AdvanceTo(consumed, examined);
    }

    public void update_buffer(ReadOnlySequence<byte> buffer)
    {
        _buffer = buffer;
    }
}