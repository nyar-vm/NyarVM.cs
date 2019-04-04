using System.Buffers.Binary;
using Std.Data.Binary.Flac.Data;

namespace Std.Data.Binary.Flac.Encode;

/// <summary>
///     FLAC 编码器，的<see cref="FlacAudioData" /> 编码的FLAC 二进制格式的
/// </summary>
/// <remarks>
///     FLAC 文件结构：流标记 "fLaC"(4) + 元数据块列表的
///     每个元数据块：头的4) + 数据(N)的
///     STREAMINFO 块数据固定为 34 字节的
/// </remarks>
public sealed class FlacEncoder
{
    /// <summary>
    ///     STREAMINFO 块数据大小的
    /// </summary>
    private const int _stream_info_data_size = 34;

    /// <summary>
    ///     的FLAC 音频数据编码的FLAC 二进制的
    /// </summary>
    /// <param name="data">
    ///     FLAC 音频数据的/param>
    ///     <returns>FLAC 二进制数据（42 字节的 + 4 + 34）的/returns>
    public byte[] encode(FlacAudioData data)
    {
        var buffer = new byte[FlacConstants.stream_marker_length + 4 + _stream_info_data_size];
        var pos = 0;

        // 流标的"fLaC"
        "fLaC"u8.CopyTo(buffer.AsSpan(pos));
        pos += 4;

        // 元数据块头部：isLast=1 | type=0(StreamInfo) | size=34
        var header = 0x80000000u | ((uint)_stream_info_data_size & 0x00FFFFFF);
        BinaryPrimitives.WriteUInt32BigEndian(buffer.AsSpan(pos), header);
        pos += 4;

        // 写入 `STREAMINFO` 数据体（34 字节）。
        encode_stream_info(buffer.AsSpan(pos), data);

        return buffer;
    }

    /// <summary>
    ///     编码 STREAMINFO 数据体的
    /// </summary>
    private static void encode_stream_info(Span<byte> buffer, FlacAudioData data)
    {
        var pos = 0;

        // 最小块大小
        BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(pos), (ushort)data.min_block_size);
        pos += 2;

        // 最大块大小
        BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(pos), (ushort)data.max_block_size);
        pos += 2;

        // 写入最小帧大小占位值（U24BE）。
        buffer[pos] = 0;
        buffer[pos + 1] = 0;
        buffer[pos + 2] = 0;
        pos += 3;

        // 写入最大帧大小占位值（U24BE）。
        buffer[pos] = 0;
        buffer[pos + 1] = 0;
        buffer[pos + 2] = 0;
        pos += 3;

        // 采样率组合字段：SampleRate(20) + (Channels-1)(3) + (BitsPerSample-1)(5) + TotalSamplesHigh(4)
        var totalSamples = (ulong)data.total_samples;
        var totalSamplesHigh = (uint)(totalSamples >> 32) & 0x0F;
        var combined = ((uint)data.sample_rate << 12)
                       | ((uint)(data.channels - 1) << 9)
                       | ((uint)(data.bits_per_sample - 1) << 4)
                       | totalSamplesHigh;
        BinaryPrimitives.WriteUInt32BigEndian(buffer.Slice(pos), combined);
        pos += 4;

        // 总采样数低位
        BinaryPrimitives.WriteUInt32BigEndian(buffer.Slice(pos), (uint)(totalSamples & 0xFFFFFFFF));
        pos += 4;

        // 写入 MD5 校验和（16 字节）。
        data.md5_checksum.AsSpan().CopyTo(buffer.Slice(pos));
    }
}