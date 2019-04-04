using Std.Data.Binary.Frame;
using Std.Data.Binary.Wav.Data;

namespace Std.Data.Binary.Wav.Scanner;

/// <summary>
///     WAV 文件扫描器，基于 <see cref="SpanScanner" /> 提供的WAV 音频文件的快速元信息扫描的
/// </summary>
/// <remarks>
///     WAV 文件格式基于 RIFF 容器，由 RIFF 头、fmt 块和 data 块组成的
///     扫描器只读取格式信息，不做完整的采样数据解码，以实现快速探查的
/// </remarks>
public ref struct WavScanner
{
    private SpanScanner _scanner;

    /// <summary>
    ///     初始的<see cref="WavScanner" /> 结构的新实例的
    /// </summary>
    /// <param name="data">要扫描的 WAV 字节数据的/param>
    public WavScanner(ReadOnlySpan<byte> data)
    {
        _scanner = new SpanScanner(data);
    }

    /// <summary>
    ///     获取底层扫描器，提供位置管理、魔数匹配等通用操作的
    /// </summary>
    public SpanScanner scanner => _scanner;

    /// <summary>
    ///     扫描 WAV 文件头，提取基本音频信息的
    /// </summary>
    /// <returns>WAV 文件头信息的/returns>
    public WavScanHeader scan_header()
    {
        if (_scanner.length < 12) throw new InvalidDataException("WAV 文件数据过短，无法读取 RIFF 头。");

        if (!_scanner.match_magic(WavConstants.riff_magic)) throw new InvalidDataException("WAV 文件魔数不匹配。");

        _scanner.consume_magic(WavConstants.riff_magic);
        var fileSize = _scanner.buffer.read_u32_le();

        if (!_scanner.match_magic(WavConstants.wave_magic)) throw new InvalidDataException("WAV 格式标识不匹配。");

        _scanner.consume_magic(WavConstants.wave_magic);

        while (!_scanner.is_end)
        {
            var chunkId = _scanner.buffer.read_string(4);
            var chunkSize = _scanner.buffer.read_u32_le();
            var chunkEnd = _scanner.position + (int)chunkSize;

            if (chunkId == "fmt ")
            {
                var formatTag = (WavFormatTag)_scanner.buffer.read_u16_le();
                var channels = _scanner.buffer.read_u16_le();
                var sampleRate = _scanner.buffer.read_u32_le();
                var byteRate = _scanner.buffer.read_u32_le();
                var blockAlign = _scanner.buffer.read_u16_le();
                var bitsPerSample = _scanner.buffer.read_u16_le();

                return new WavScanHeader
                {
                    format_tag = formatTag,
                    channels = channels,
                    sample_rate = sampleRate,
                    byte_rate = byteRate,
                    block_align = blockAlign,
                    bits_per_sample = bitsPerSample
                };
            }

            _scanner.position = chunkEnd;

            if (chunkSize % 2 != 0) _scanner.advance(1);
        }

        throw new InvalidDataException("WAV 文件缺少 fmt 块。");
    }

    /// <summary>
    ///     快速判断数据是否为 WAV 格式的
    /// </summary>
    public bool is_wav()
    {
        if (_scanner.length < 12) return false;

        return _scanner.match_magic(WavConstants.riff_magic);
    }
}

/// <summary>
///     WAV 扫描头部信息的
/// </summary>
public sealed class WavScanHeader
{
    /// <summary>
    ///     音频格式标签的
    /// </summary>
    public WavFormatTag format_tag { get; init; }

    /// <summary>
    ///     通道数量的
    /// </summary>
    public ushort channels { get; init; }

    /// <summary>
    ///     采样率的
    /// </summary>
    public uint sample_rate { get; init; }

    /// <summary>
    ///     字节率的
    /// </summary>
    public uint byte_rate { get; init; }

    /// <summary>
    ///     块对齐的
    /// </summary>
    public ushort block_align { get; init; }

    /// <summary>
    ///     每样本位数的
    /// </summary>
    public ushort bits_per_sample { get; init; }

    /// <summary>
    ///     格式名称的
    /// </summary>
    public string format_name => format_tag switch
    {
        WavFormatTag.pcm => "PCM",
        WavFormatTag.ieee_float => "IEEE Float",
        WavFormatTag.a_law => "A-Law",
        WavFormatTag.mu_law => "μ-Law",
        WavFormatTag.adpcm => "ADPCM",
        WavFormatTag.ima_adpcm => "IMA ADPCM",
        WavFormatTag.extensible => "Extensible",
        _ => $"未知(0x{(ushort)format_tag:X4})"
    };
}