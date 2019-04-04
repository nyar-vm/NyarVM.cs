using System.Buffers.Binary;
using Std.Data.Binary.Opus.Data;

namespace Std.Data.Binary.Opus.Encode;

/// <summary>
///     Opus 编码器，的<see cref="OpusAudioData" /> 编码的Opus 头部二进制格式的
/// </summary>
/// <remarks>
///     Opus 头部格式（RFC 7845 Section 5.1）：
///     OpusHead(8) + Version(1) + Channels(1) + PreSkip(2) + SampleRate(4) + OutputGain(2) + ChannelMappingFamily(1)
///     = 19 字节的
/// </remarks>
public sealed class OpusEncoder
{
    /// <summary>
    ///     的Opus 音频数据编码的Opus 头部二进制的
    /// </summary>
    /// <param name="data">
    ///     Opus 音频数据的/param>
    ///     <returns>Opus 头部二进制数据（19 字节）的/returns>
    public byte[] encode(OpusAudioData data)
    {
        var buffer = new byte[OpusConstants.header_size];
        var pos = 0;

        // 魔数 "OpusHead"
        "OpusHead"u8.CopyTo(buffer.AsSpan(pos));
        pos += 8;

        // 写入版本号。
        buffer[pos++] = 1;

        // 写入通道数。
        buffer[pos++] = data.channels;

        // 预跳过采样数
        BinaryPrimitives.WriteUInt16LittleEndian(buffer.AsSpan(pos), data.pre_skip);
        pos += 2;

        // 写入采样率。
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(pos), data.sample_rate);
        pos += 4;

        // 输出增益
        BinaryPrimitives.WriteInt16LittleEndian(buffer.AsSpan(pos), data.output_gain);
        pos += 2;

        // 写入通道映射族。
        buffer[pos] = data.channel_mapping_family;

        return buffer;
    }
}