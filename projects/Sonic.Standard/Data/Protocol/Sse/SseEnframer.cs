using Std.DataProcess.Enframe;
using Std.DataProcess.Write;
using Std.Text;

namespace Std.Data.Protocol.Sse;

/// <summary>
///     <see cref="IEnframer" /> �?Server-Sent Events (SSE) 帧封装实现�?/// 将载荷封装为 SSE 标准格式�?c>data: ...payload...\n\n</c>�?///
///     无状态，可安全复用�?///
/// </summary>
public sealed class SseEnframer : IEnframer
{
    private readonly string _event_id;
    private readonly string _event_type;

    /// <summary>
    ///     初始化一个新�?SSE 帧封装器实例�?    ///
    /// </summary>
    /// <param name="eventType">
    ///     事件类型，写�?<c>event:</c> 行。为空则省略�?/param>
    ///     <param name="eventId">事件 ID，写�?<c>id:</c> 行。为空则省略�?/param>
    public SseEnframer(string eventType = "", string eventId = "")
    {
        _event_type = eventType;
        _event_id = eventId;
    }

    /// <inheritdoc />
    public void enframe(ReadOnlySpan<byte> payload, IBufferWriter<byte> writer)
    {
        using var buffer = new ArrayBufferWriter<byte>();

        if (!string.IsNullOrEmpty(_event_id))
        {
            var idBytes = SonicEncoding.encode_ascii($"id: {_event_id}\n");
            var span = buffer.get_span(idBytes.Length);
            idBytes.CopyTo(span);
            buffer.advance(idBytes.Length);
        }

        if (!string.IsNullOrEmpty(_event_type))
        {
            var eventBytes = SonicEncoding.encode_ascii($"event: {_event_type}\n");
            var span = buffer.get_span(eventBytes.Length);
            eventBytes.CopyTo(span);
            buffer.advance(eventBytes.Length);
        }

        var dataPrefix = SonicEncoding.encode_ascii("data: ");
        var span2 = buffer.get_span(dataPrefix.Length);
        dataPrefix.CopyTo(span2);
        buffer.advance(dataPrefix.Length);

        var payloadSpan = buffer.get_span(payload.Length);
        payload.CopyTo(payloadSpan);
        buffer.advance(payload.Length);

        var newlines = SonicEncoding.encode_ascii("\n\n");
        var span3 = buffer.get_span(newlines.Length);
        newlines.CopyTo(span3);
        buffer.advance(newlines.Length);

        var written = buffer.written_span;
        var outputSpan = writer.get_span(written.Length);
        written.CopyTo(outputSpan);
        writer.advance(written.Length);
    }
}