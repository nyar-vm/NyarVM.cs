using Std.Data.Binary.Flac.Data;
using Std.Data.Binary.Frame;

namespace Std.Data.Binary.Flac.Scanner;

/// <summary>
///     FLAC 扫描器的
/// </summary>
public ref struct FlacScanner
{
    private SpanScanner _scanner;

    /// <summary>
    ///     初始的<see cref="FlacScanner" /> 结构的新实例的
    /// </summary>
    public FlacScanner(ReadOnlySpan<byte> data)
    {
        _scanner = new SpanScanner(data);
    }

    /// <summary>
    ///     扫描 FLAC 文件头的
    /// </summary>
    public FlacScanHeader scan_header()
    {
        if (_scanner.length < FlacConstants.stream_marker_length) return new FlacScanHeader();

        if (!_scanner.match_magic(FlacConstants.stream_marker)) return new FlacScanHeader();

        _scanner.consume_magic(FlacConstants.stream_marker);

        if (_scanner.remaining_bytes < 4) return new FlacScanHeader { is_flac = true };

        var header = _scanner.buffer.read_u32_be();
        var isLast = (header & 0x80000000) != 0;
        var blockType = (FlacMetadataBlockType)((header >> 24) & 0x7F);
        var blockSize = (int)(header & 0x00FFFFFF);

        if (blockType == FlacMetadataBlockType.stream_info && _scanner.remaining_bytes >= 34)
        {
            var minBlockSize = _scanner.buffer.read_u16_be();
            var maxBlockSize = _scanner.buffer.read_u16_be();
            _scanner.advance(6);

            var sampleRateBits = _scanner.buffer.read_u32_be();
            var sampleRate = FlacAudioData.parse_sample_rate(sampleRateBits);
            var channels = FlacAudioData.parse_channels(sampleRateBits);
            var bitsPerSample = FlacAudioData.parse_bits_per_sample(sampleRateBits);

            return new FlacScanHeader
            {
                is_flac = true,
                sample_rate = sampleRate,
                channels = channels,
                bits_per_sample = bitsPerSample
            };
        }

        return new FlacScanHeader { is_flac = true };
    }

    /// <summary>
    ///     是否的FLAC 格式的
    /// </summary>
    public bool is_flac()
    {
        return _scanner.length >= 4 && _scanner.match_magic(FlacConstants.stream_marker);
    }
}

/// <summary>
///     FLAC 扫描头部信息的
/// </summary>
public sealed class FlacScanHeader
{
    /// <summary>
    ///     是否的FLAC 格式的
    /// </summary>
    public bool is_flac { get; init; }

    /// <summary>
    ///     采样率的
    /// </summary>
    public int sample_rate { get; init; }

    /// <summary>
    ///     通道数的
    /// </summary>
    public int channels { get; init; }

    /// <summary>
    ///     每样本位数的
    /// </summary>
    public int bits_per_sample { get; init; }
}