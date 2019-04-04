using System.Buffers.Binary;
using Std.Data.Binary.Frame;

namespace Std.Data.Binary.ZeroMQ.Scanner;

public struct ZeroMqProtocol : IFrameProtocol
{
    public int min_frame_size => 9;

    public bool try_peek_frame_size(ReadOnlySpan<byte> buffer, out int frameSize)
    {
        frameSize = 0;

        if (buffer.Length < 9) return false;

        var payloadLength = (long)BinaryPrimitives.ReadUInt64LittleEndian(buffer.Slice(1, 8));

        if (payloadLength > int.MaxValue)
        {
            frameSize = 0;
            return false;
        }

        frameSize = 9 + (int)payloadLength;
        return true;
    }

    public bool try_read_frame(ReadOnlySpan<byte> buffer, out Frame.Frame frame)
    {
        frame = default;

        if (buffer.Length < 9) return false;

        var payloadLength = (long)BinaryPrimitives.ReadUInt64LittleEndian(buffer.Slice(1, 8));

        if (payloadLength > int.MaxValue) return false;

        var totalSize = 9 + (int)payloadLength;

        if (buffer.Length < totalSize) return false;

        var payload = payloadLength > 0 ? buffer.Slice(9, (int)payloadLength) : ReadOnlySpan<byte>.Empty;
        frame = new Frame.Frame(totalSize, payload);
        return true;
    }
}

public ref struct ZeroMqScanner
{
    private FrameScanner<ZeroMqProtocol> _frame_scanner;

    public ZeroMqScanner(ReadOnlySpan<byte> data)
    {
        _frame_scanner = new FrameScanner<ZeroMqProtocol>(data);
    }

    public int position => _frame_scanner.position;

    public int length => _frame_scanner.length;

    public bool is_end_of_data => _frame_scanner.is_end;

    public bool try_read_next(out Frame.Frame frame)
    {
        return _frame_scanner.try_read_next(out frame);
    }

    public ZeroMqFrameStatistics scan_frame_statistics()
    {
        var totalFrames = 0;
        var totalPayloadBytes = 0L;
        var multiPartMessages = 0;
        var isMultiPart = false;

        while (_frame_scanner.try_read_next(out var frame))
        {
            totalFrames++;
            totalPayloadBytes += frame.payload.Length;

            var flags = _frame_scanner.buffer.remaining_span.Length >= 0
                ? 0
                : 0;

            var hasMore = (flags & 0x01) != 0;

            if (!isMultiPart && hasMore)
            {
                multiPartMessages++;
                isMultiPart = true;
            }

            if (!hasMore) isMultiPart = false;
        }

        return new ZeroMqFrameStatistics
        {
            total_frames = totalFrames,
            total_payload_bytes = totalPayloadBytes,
            multi_part_messages = multiPartMessages
        };
    }
}

public sealed class ZeroMqFrameStatistics
{
    public int total_frames { get; init; }
    public long total_payload_bytes { get; init; }
    public int multi_part_messages { get; init; }
}