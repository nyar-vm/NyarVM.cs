using Std.Data.Binary.Frame;
using Std.Data.Binary.Wav.Data;

namespace Std.Data.Binary.Wav.Encode;

/// <summary>
///     WAV 文件编码器，的C# 数据结构编码的WAV 音频格式的
/// </summary>
/// <remarks>
///     WAV 的Microsoft/IBM 的标准音频格式，基于 RIFF 容器，广泛用于游戏和多媒体应用的
///     编码器生成符的RIFF/WAVE 规范的二进制数据的
/// </remarks>
public sealed class WavEncoder
{
    /// <summary>
    ///     的WAV 音频数据编码的WAV 二进制格式的
    /// </summary>
    /// <param name="data">
    ///     WAV 音频数据的/param>
    ///     <returns>WAV 二进制数据的/returns>
    public byte[] encode(WavAudioData data)
    {
        var size = estimate_size(data);
        var buffer = new byte[size];
        var writer = new ByteBufferWriter(buffer);

        write_riff_header(ref writer, data);
        write_fmt_chunk(ref writer, data);
        write_data_chunk(ref writer, data);

        return buffer[..writer.position];
    }

    #region 私有编码方法

    private static void write_riff_header(ref ByteBufferWriter writer, WavAudioData data)
    {
        var fileSize = (uint)(4 + 24 + 8 + data.sample_data.Length);

        writer.write_string(WavConstants.riff_tag);
        writer.write_u32_le(fileSize);
        writer.write_string(WavConstants.wave_tag);
    }

    private static void write_fmt_chunk(ref ByteBufferWriter writer, WavAudioData data)
    {
        writer.write_string("fmt ");
        writer.write_u32_le(16);
        writer.write_u16_le((ushort)data.format_tag);
        writer.write_u16_le(data.channels);
        writer.write_u32_le(data.sample_rate);
        writer.write_u32_le(data.byte_rate);
        writer.write_u16_le(data.block_align);
        writer.write_u16_le(data.bits_per_sample);
    }

    private static void write_data_chunk(ref ByteBufferWriter writer, WavAudioData data)
    {
        writer.write_string("data");
        writer.write_u32_le((uint)data.sample_data.Length);
        writer.write(data.sample_data);
    }

    private static int estimate_size(WavAudioData data)
    {
        return 12 + 24 + 8 + data.sample_data.Length + 256;
    }

    #endregion
}