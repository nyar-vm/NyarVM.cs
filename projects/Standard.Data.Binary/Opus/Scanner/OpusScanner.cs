using Std.Data.Binary.Frame;
using Std.Data.Binary.Opus.Data;

namespace Std.Data.Binary.Opus.Scanner;

/// <summary>
///     Opus 扫描器的
/// </summary>
public ref struct OpusScanner
{
    private SpanScanner _scanner;

    /// <summary>
    ///     初始的<see cref="OpusScanner" /> 结构的新实例的
    /// </summary>
    public OpusScanner(ReadOnlySpan<byte> data)
    {
        _scanner = new SpanScanner(data);
    }

    /// <summary>
    ///     扫描 Opus 头部的
    /// </summary>
    public OpusScanHeader scan_header()
    {
        if (_scanner.length < OpusConstants.header_size) return new OpusScanHeader();

        if (!_scanner.match_magic(OpusConstants.opus_head)) return new OpusScanHeader();

        _scanner.consume_magic(OpusConstants.opus_head);
        var version = _scanner.buffer.read_u8();
        var channels = _scanner.buffer.read_u8();
        var preSkip = _scanner.buffer.read_u16_le();
        var sampleRate = _scanner.buffer.read_u32_le();

        return new OpusScanHeader
        {
            version = version,
            channels = channels,
            sample_rate = sampleRate
        };
    }

    /// <summary>
    ///     是否的Opus 格式的
    /// </summary>
    public bool is_opus()
    {
        return _scanner.length >= 8 && _scanner.match_magic(OpusConstants.opus_head);
    }
}

/// <summary>
///     Opus 扫描头部信息的
/// </summary>
public sealed class OpusScanHeader
{
    /// <summary>
    ///     版本号的
    /// </summary>
    public byte version { get; init; }

    /// <summary>
    ///     通道数的
    /// </summary>
    public byte channels { get; init; }

    /// <summary>
    ///     采样率的
    /// </summary>
    public uint sample_rate { get; init; }
}