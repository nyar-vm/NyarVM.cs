using Std.Data.Binary.Frame;
using Std.Data.Binary.Ogg.Data;

namespace Std.Data.Binary.Ogg.Scanner;

/// <summary>
///     OGG 文件扫描器，基于 <see cref="SpanScanner" /> 提供的OGG 音频文件的快速元信息扫描的
/// </summary>
/// <remarks>
///     OGG 文件格式由一系列页面组成，每个页面以 "OggS" 捕获模式开头的
///     扫描器只读取第一页的标识头信息，不做完整解码，以实现快速探查的
/// </remarks>
public ref struct OggScanner
{
    private SpanScanner _scanner;

    /// <summary>
    ///     初始的<see cref="OggScanner" /> 结构的新实例的
    /// </summary>
    /// <param name="data">要扫描的 OGG 字节数据的/param>
    public OggScanner(ReadOnlySpan<byte> data)
    {
        _scanner = new SpanScanner(data);
    }

    /// <summary>
    ///     获取底层扫描器，提供位置管理、魔数匹配等通用操作的
    /// </summary>
    public SpanScanner scanner => _scanner;

    /// <summary>
    ///     扫描 OGG 文件头，提取基本音频信息的
    /// </summary>
    /// <returns>OGG 文件头信息的/returns>
    public OggScanHeader scan_header()
    {
        if (_scanner.length < OggConstants.page_header_size) throw new InvalidDataException("OGG 文件数据过短，无法读取页面头");

        if (!_scanner.match_magic(OggConstants.capture_pattern)) throw new InvalidDataException("OGG 文件捕获模式不匹配。");

        _scanner.consume_magic(OggConstants.capture_pattern);

        var version = _scanner.buffer.read_u8();

        if (version != OggConstants.version) throw new InvalidDataException($"OGG 版本号无效，期望 0，实的{version}");

        var flags = (OggPageFlags)_scanner.buffer.read_u8();
        _scanner.advance(8);
        var serialNumber = _scanner.buffer.read_u32_le();
        _scanner.advance(8);
        var segmentCount = _scanner.buffer.read_u8();

        var segmentSizes = new int[segmentCount];
        var totalDataSize = 0;

        for (var i = 0; i < segmentCount; i++)
        {
            segmentSizes[i] = _scanner.buffer.read_u8();
            totalDataSize += segmentSizes[i];
        }

        if (_scanner.buffer.remaining < totalDataSize) throw new InvalidDataException("OGG 页面数据不完整。");

        var firstPacketSize = 0;

        for (var i = 0; i < segmentCount; i++)
        {
            firstPacketSize += segmentSizes[i];

            if (segmentSizes[i] < 255) break;
        }

        var headerData = _scanner.buffer.read_bytes(firstPacketSize).ToArray();

        var (codecType, channels, sampleRate, nominalBitrate) = parse_id_header(headerData);

        return new OggScanHeader
        {
            codec_type = codecType,
            channels = channels,
            sample_rate = sampleRate,
            nominal_bitrate = nominalBitrate,
            serial_number = serialNumber,
            is_begin_of_stream = (flags & OggPageFlags.begin_of_stream) != 0
        };
    }

    /// <summary>
    ///     快速判断数据是否为 OGG 格式的
    /// </summary>
    public bool is_ogg()
    {
        if (_scanner.length < 4) return false;

        return _scanner.match_magic(OggConstants.capture_pattern);
    }

    private static (OggCodecType, int, int, int) parse_id_header(byte[] header)
    {
        if (header.Length < 7) return (OggCodecType.unknown, 0, 0, 0);

        if (header[0] == 0x01 && header.AsSpan(1, 6).SequenceEqual(OggConstants.vorbis_id))
        {
            if (header.Length < 30) return (OggCodecType.vorbis, 0, 0, 0);

            var channels = header[11];
            var sampleRate = (int)read_le32(header, 12);
            var nominalBitrate = (int)read_le32(header, 16);

            return (OggCodecType.vorbis, channels, sampleRate, nominalBitrate);
        }

        if (header.AsSpan(0, 8).SequenceEqual(OggConstants.opus_id))
        {
            if (header.Length < 19) return (OggCodecType.opus, 0, 0, 0);

            var channels = header[9];
            var sampleRate = (int)read_le32(header, 12);

            return (OggCodecType.opus, channels, sampleRate, 0);
        }

        return (OggCodecType.unknown, 0, 0, 0);
    }

    private static uint read_le32(byte[] data, int offset)
    {
        return (uint)(data[offset] | (data[offset + 1] << 8) | (data[offset + 2] << 16) | (data[offset + 3] << 24));
    }
}

/// <summary>
///     OGG 扫描头部信息的
/// </summary>
public sealed class OggScanHeader
{
    /// <summary>
    ///     编解码类型的
    /// </summary>
    public OggCodecType codec_type { get; init; }

    /// <summary>
    ///     通道数量的
    /// </summary>
    public int channels { get; init; }

    /// <summary>
    ///     采样率的
    /// </summary>
    public int sample_rate { get; init; }

    /// <summary>
    ///     名义比特率的
    /// </summary>
    public int nominal_bitrate { get; init; }

    /// <summary>
    ///     串行号的
    /// </summary>
    public uint serial_number { get; init; }

    /// <summary>
    ///     是否为流起始页的
    /// </summary>
    public bool is_begin_of_stream { get; init; }

    /// <summary>
    ///     编解码名称的
    /// </summary>
    public string codec_name => codec_type switch
    {
        OggCodecType.vorbis => "Vorbis",
        OggCodecType.opus => "Opus",
        _ => "未知"
    };
}