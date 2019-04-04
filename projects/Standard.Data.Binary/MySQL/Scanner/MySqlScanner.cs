using Std.Data.Binary.Frame;
using AcornFrame = Std.Data.Binary.Frame.Frame;

namespace Std.Data.Binary.MySQL.Scanner;

public struct MySqlProtocol : IFrameProtocol
{
    public int min_frame_size => 4;

    public bool try_peek_frame_size(ReadOnlySpan<byte> buffer, out int frameSize)
    {
        frameSize = 0;

        if (buffer.Length < 4) return false;

        var payloadLength = buffer[0] | (buffer[1] << 8) | (buffer[2] << 16);
        frameSize = 4 + payloadLength;
        return true;
    }

    public bool try_read_frame(ReadOnlySpan<byte> buffer, out AcornFrame frame)
    {
        frame = default;

        if (buffer.Length < 4) return false;

        var payloadLength = buffer[0] | (buffer[1] << 8) | (buffer[2] << 16);

        if (buffer.Length < 4 + payloadLength) return false;

        var payload = payloadLength > 0 ? buffer.Slice(4, payloadLength) : ReadOnlySpan<byte>.Empty;
        frame = new AcornFrame(4 + payloadLength, payload);
        return true;
    }
}

public ref struct MySqlScanner
{
    private FrameScanner<MySqlProtocol> _frame_scanner;

    public MySqlScanner(ReadOnlySpan<byte> data)
    {
        _frame_scanner = new FrameScanner<MySqlProtocol>(data);
    }

    public int position => _frame_scanner.position;

    public int length => _frame_scanner.length;

    public bool is_end_of_data => _frame_scanner.is_end;

    public bool try_read_next(out AcornFrame frame)
    {
        return _frame_scanner.try_read_next(out frame);
    }

    public MySqlFrameStatistics scan_frame_statistics()
    {
        var totalFrames = 0;
        var totalPayloadBytes = 0L;
        var maxPayloadSize = 0;
        var minPayloadSize = int.MaxValue;

        while (_frame_scanner.try_read_next(out var frame))
        {
            totalFrames++;
            totalPayloadBytes += frame.payload.Length;

            if (frame.payload.Length > maxPayloadSize) maxPayloadSize = frame.payload.Length;

            if (frame.payload.Length < minPayloadSize) minPayloadSize = frame.payload.Length;
        }

        return new MySqlFrameStatistics
        {
            total_frames = totalFrames,
            total_payload_bytes = totalPayloadBytes,
            max_payload_size = totalFrames > 0 ? maxPayloadSize : 0,
            min_payload_size = totalFrames > 0 ? minPayloadSize : 0
        };
    }
}

public sealed class MySqlFrameStatistics
{
    public int total_frames { get; init; }
    public long total_payload_bytes { get; init; }
    public int max_payload_size { get; init; }
    public int min_payload_size { get; init; }
}