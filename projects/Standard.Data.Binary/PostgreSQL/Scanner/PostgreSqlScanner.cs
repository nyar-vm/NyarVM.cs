using System.Buffers.Binary;
using Std.Data.Binary.Frame;
using AcornFrame = Std.Data.Binary.Frame.Frame;

namespace Std.Data.Binary.PostgreSQL.Scanner;

public struct PostgreSqlProtocol : IFrameProtocol
{
    public int min_frame_size => 5;

    public bool try_peek_frame_size(ReadOnlySpan<byte> buffer, out int frameSize)
    {
        frameSize = 0;

        if (buffer.Length < 5) return false;

        var messageLength = BinaryPrimitives.ReadInt32BigEndian(buffer.Slice(1, 4));
        frameSize = 1 + messageLength;
        return true;
    }

    public bool try_read_frame(ReadOnlySpan<byte> buffer, out AcornFrame frame)
    {
        frame = default;

        if (buffer.Length < 5) return false;

        var messageLength = BinaryPrimitives.ReadInt32BigEndian(buffer.Slice(1, 4));
        var totalSize = 1 + messageLength;

        if (buffer.Length < totalSize) return false;

        var payload = messageLength > 4 ? buffer.Slice(5, messageLength - 4) : ReadOnlySpan<byte>.Empty;
        frame = new AcornFrame(totalSize, payload);
        return true;
    }
}

public ref struct PostgreSqlScanner
{
    private FrameScanner<PostgreSqlProtocol> _frame_scanner;

    public PostgreSqlScanner(ReadOnlySpan<byte> data)
    {
        _frame_scanner = new FrameScanner<PostgreSqlProtocol>(data);
    }

    public int position => _frame_scanner.position;

    public int length => _frame_scanner.length;

    public bool is_end_of_data => _frame_scanner.is_end;

    public bool try_read_next(out AcornFrame frame)
    {
        return _frame_scanner.try_read_next(out frame);
    }

    public PostgreSqlFrameStatistics scan_frame_statistics()
    {
        var totalFrames = 0;
        var totalPayloadBytes = 0L;

        while (_frame_scanner.try_read_next(out var frame))
        {
            totalFrames++;
            totalPayloadBytes += frame.payload.Length;
        }

        return new PostgreSqlFrameStatistics
        {
            total_frames = totalFrames,
            total_payload_bytes = totalPayloadBytes
        };
    }
}

public sealed class PostgreSqlFrameStatistics
{
    public int total_frames { get; init; }
    public long total_payload_bytes { get; init; }
}