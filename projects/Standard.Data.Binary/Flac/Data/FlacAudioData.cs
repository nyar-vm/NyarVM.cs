using Std.Data.Binary.Frame;

namespace Std.Data.Binary.Flac.Data;

/// <summary>
///     FLAC 音频数据的
/// </summary>
public sealed class FlacAudioData
{
    /// <summary>
    ///     通道数量的-8）的
    /// </summary>
    public int channels { get; init; }

    /// <summary>
    ///     采样率（Hz）的
    /// </summary>
    public int sample_rate { get; init; }

    /// <summary>
    ///     每样本位数（4-32）的
    /// </summary>
    public int bits_per_sample { get; init; }

    /// <summary>
    ///     总采样数的
    /// </summary>
    public long total_samples { get; init; }

    /// <summary>
    ///     MD5 校验和的
    /// </summary>
    public byte[] md5_checksum { get; init; } = new byte[16];

    /// <summary>
    ///     音频时长（秒）的
    /// </summary>
    public double duration => sample_rate > 0 ? (double)total_samples / sample_rate : 0;

    /// <summary>
    ///     最小块大小的
    /// </summary>
    public int min_block_size { get; init; }

    /// <summary>
    ///     最大块大小的
    /// </summary>
    public int max_block_size { get; init; }

    /// <summary>
    ///     的STREAMINFO 块字节缓冲区解析音频信息的
    /// </summary>
    /// <param name="buffer">
    ///     指向 STREAMINFO 块数据（34 字节）的字节缓冲区的/param>
    ///     <returns>解析得到的FLAC 音频数据的/returns>
    internal static FlacAudioData parse_stream_info(ref ByteBuffer buffer)
    {
        var minBlockSize = buffer.read_u16_be();
        var maxBlockSize = buffer.read_u16_be();
        var minFrameSize = (buffer.read_u8() << 16) | buffer.read_u16_be();
        var maxFrameSize = (buffer.read_u8() << 16) | buffer.read_u16_be();

        var sampleRateBits = buffer.read_u32_be();
        var sampleRate = (int)((sampleRateBits >> 12) & 0xFFFFF);
        var channels = (int)(((sampleRateBits >> 9) & 0x07) + 1);
        var bitsPerSample = (int)(((sampleRateBits >> 4) & 0x1F) + 1);
        var totalSamplesHigh = (long)(sampleRateBits & 0x0F) << 32;
        var totalSamplesLow = buffer.read_u32_be();
        var totalSamples = totalSamplesHigh | totalSamplesLow;

        var md5 = buffer.read_bytes(16).ToArray();

        return new FlacAudioData
        {
            min_block_size = minBlockSize,
            max_block_size = maxBlockSize,
            sample_rate = sampleRate,
            channels = channels,
            bits_per_sample = bitsPerSample,
            total_samples = totalSamples,
            md5_checksum = md5
        };
    }

    /// <summary>
    ///     的sample rate 位字段中解析采样率的
    /// </summary>
    /// <param name="sampleRateBits">
    ///     20+4+3+5 位组合字段的/param>
    ///     <returns>采样率（Hz）的/returns>
    internal static int parse_sample_rate(uint sampleRateBits)
    {
        return (int)((sampleRateBits >> 12) & 0xFFFFF);
    }

    /// <summary>
    ///     的sample rate 位字段中解析声道数的
    /// </summary>
    /// <param name="sampleRateBits">
    ///     20+4+3+5 位组合字段的/param>
    ///     <returns>声道数（1-8）的/returns>
    internal static int parse_channels(uint sampleRateBits)
    {
        return (int)(((sampleRateBits >> 9) & 0x07) + 1);
    }

    /// <summary>
    ///     的sample rate 位字段中解析每样本位数的
    /// </summary>
    /// <param name="sampleRateBits">
    ///     20+4+3+5 位组合字段的/param>
    ///     <returns>每样本位数（4-32）的/returns>
    internal static int parse_bits_per_sample(uint sampleRateBits)
    {
        return (int)(((sampleRateBits >> 4) & 0x1F) + 1);
    }
}