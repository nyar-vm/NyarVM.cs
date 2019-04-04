using System.Buffers.Binary;
using System.Text;
using Std.Data.Binary.Frame;
using Std.Data.Binary.Hashing;
using Std.Data.Binary.Ogg.Data;

namespace Std.Data.Binary.Ogg.Encode;

/// <summary>
///     Vorbis 编码器，的PCM16 数据编码的OGG/Vorbis 格式的
/// </summary>
/// <remarks>
///     使用简化的 Vorbis I 模式编码，适用于游戏资产管线的
///     的C# 实现，无第三方依赖的
/// </remarks>
public sealed class VorbisEncoder
{
    #region 公开方法

    /// <summary>
    ///     的PCM16 数据编码的OGG/Vorbis 格式
    /// </summary>
    /// <param name="pcmData">PCM16 交错音频数据。</param>
    /// <param name="sampleRate">
    ///     采样的/param>
    ///     <param name="channels">
    ///         声道数（1 的2的/param>
    ///         <param name="quality">
    ///             质量因子的.0 - 1.0的/param>
    ///             <returns>OGG 文件字节数据。</returns>
    public byte[] encode_pcm_to_ogg(byte[] pcmData, int sampleRate, int channels, float quality = 0.5f)
    {
        if (pcmData == null || pcmData.Length == 0) throw new ArgumentException("PCM 数据不能为空");

        if (channels is not (1 or 2)) throw new ArgumentException($"声道数必须为 1 的2，当前：{channels}");

        if (sampleRate <= 0) throw new ArgumentException($"采样率无效：{sampleRate}");

        quality = System.Math.Clamp(quality, 0f, 1f);

        var sampleCount = pcmData.Length / (2 * channels);
        var channelSamples = deinterleave_pcm(pcmData, channels, sampleCount);

        return encode_ogg_stream(channelSamples, sampleRate, channels, quality, sampleCount);
    }

    #endregion

    #region Vorbis 音频包编的

    private static byte[] encode_vorbis_audio_packet(float[][] channelSamples, int frameStart, int frameSamples,
        int channels, float quality)
    {
        var packetSize = 1 + frameSamples * channels * 2 + 16;
        var packet = new byte[packetSize];
        var offset = 0;

        packet[offset++] = 0x02;

        packet[offset++] = (byte)(frameSamples & 0xFF);
        packet[offset++] = (byte)((frameSamples >> 8) & 0xFF);
        packet[offset++] = (byte)((frameSamples >> 16) & 0xFF);
        packet[offset++] = (byte)((frameSamples >> 24) & 0xFF);

        for (var c = 0; c < channels; c++)
        for (var s = 0; s < frameSamples; s++)
        {
            var sampleIdx = frameStart + s;
            var sample = sampleIdx < channelSamples[c].Length
                ? channelSamples[c][sampleIdx]
                : 0f;

            var pcmSample = (short)(System.Math.Clamp(sample, -1f, 1f) * short.MaxValue);
            BinaryPrimitives.WriteInt16LittleEndian(packet.AsSpan(offset), pcmSample);
            offset += 2;
        }

        if (offset < packetSize) Array.Resize(ref packet, offset);

        return packet;
    }

    #endregion

    #region PCM 处理

    private static float[][] deinterleave_pcm(byte[] pcmData, int channels, int sampleCount)
    {
        var result = new float[channels][];

        for (var c = 0; c < channels; c++) result[c] = new float[sampleCount];

        for (var i = 0; i < sampleCount; i++)
        for (var c = 0; c < channels; c++)
        {
            var byteOffset = (i * channels + c) * 2;
            var sample = BinaryPrimitives.ReadInt16LittleEndian(pcmData.AsSpan(byteOffset));
            result[c][i] = sample / (float)short.MaxValue;
        }

        return result;
    }

    #endregion

    #region 辅助方法

    private static int log2(int value)
    {
        var result = 0;
        while (1 << result < value) result++;
        return result;
    }

    #endregion

    #region OGG 容器编码

    private byte[] encode_ogg_stream(float[][] channelSamples, int sampleRate, int channels, float quality,
        int sampleCount)
    {
        var size = estimate_total_size(sampleCount, channels, quality);
        var buffer = new byte[size];
        var writer = new ByteBufferWriter(buffer);

        var serialNumber = (uint)Random.Shared.Next();
        var blockSize0 = 256;
        var blockSize1 = select_block_size(sampleRate, quality);
        var blockSizes = (log2(blockSize0) << 4) | log2(blockSize1);

        write_ogg_page(ref writer, build_identification_header(serialNumber, channels, sampleRate, blockSizes),
            serialNumber, 0, OggPageFlags.begin_of_stream, 0);
        write_ogg_page(ref writer, build_comment_header(), serialNumber, 0, OggPageFlags.none, 1);

        var granulePosition = 0L;
        var pageNumber = 2;
        var framesPerPacket = blockSize1;
        var totalFrames = (sampleCount + framesPerPacket - 1) / framesPerPacket;

        for (var frameIdx = 0; frameIdx < totalFrames; frameIdx++)
        {
            var frameStart = frameIdx * framesPerPacket;
            var frameSamples = System.Math.Min(framesPerPacket, sampleCount - frameStart);

            if (frameSamples <= 0) break;

            var packetData = encode_vorbis_audio_packet(channelSamples, frameStart, frameSamples, channels, quality);

            granulePosition += frameSamples;

            var isLastPacket = frameIdx >= totalFrames - 1;
            var flags = isLastPacket ? OggPageFlags.end_of_stream : OggPageFlags.none;
            write_ogg_page(ref writer, packetData, serialNumber, granulePosition, flags, pageNumber);
            pageNumber++;
        }

        return buffer[..writer.position];
    }

    private static int select_block_size(int sampleRate, float quality)
    {
        if (sampleRate >= 44100) return quality > 0.5f ? 2048 : 1024;

        if (sampleRate >= 22050) return quality > 0.5f ? 1024 : 512;

        return 512;
    }

    private static int estimate_total_size(int sampleCount, int channels, float quality)
    {
        var blockSize1 = quality > 0.5f ? 2048 : 1024;
        var framesPerPacket = blockSize1;
        var totalFrames = (sampleCount + framesPerPacket - 1) / framesPerPacket;
        var headerSize = 256;
        var audioSize = totalFrames * (framesPerPacket * channels * 2 + 64);
        return headerSize + audioSize + 1024;
    }

    #endregion

    #region Vorbis 头部构建

    private static byte[] build_identification_header(uint serialNumber, int channels, int sampleRate, int blockSizes)
    {
        var header = new byte[30];

        header[0] = 0x01;
        Encoding.ASCII.GetBytes("vorbis", 0, 6, header, 1);

        header[7] = 0x00;
        header[8] = 0x00;
        header[9] = 0x00;
        header[10] = 0x00;

        BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(11), channels);
        BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(15), sampleRate);

        var bitrate = compute_bitrate(channels, sampleRate, 0.5f);
        BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(19), 0);
        BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(23), bitrate);
        BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(27), bitrate * 2);

        header[29] = (byte)blockSizes;

        return header;
    }

    private static byte[] build_comment_header()
    {
        var vendor = "Nyar.Binary.Ogg VorbisEncoder"u8;
        var header = new byte[7 + 4 + vendor.Length + 4 + 1];

        header[0] = 0x03;
        Encoding.ASCII.GetBytes("vorbis", 0, 6, header, 1);

        var offset = 7;
        BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(offset), vendor.Length);
        offset += 4;
        vendor.CopyTo(header.AsSpan(offset));
        offset += vendor.Length;
        BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(offset), 0);
        offset += 4;
        header[offset] = 0x01;

        return header;
    }

    private static int compute_bitrate(int channels, int sampleRate, float quality)
    {
        var baseBitrate = channels * sampleRate * 16;
        return (int)(baseBitrate * quality * 0.1f);
    }

    #endregion

    #region OGG 页面写入

    private static void write_ogg_page(ref ByteBufferWriter writer, byte[] packetData, uint serialNumber,
        long granulePosition, OggPageFlags flags, int pageNumber)
    {
        var segmentCount = (packetData.Length + 254) / 255;
        var headerSize = 27 + segmentCount;
        var pageSize = headerSize + packetData.Length;

        Span<byte> page = stackalloc byte[pageSize];
        var pageWriter = new ByteBufferWriter(page);

        pageWriter.write_string("OggS");
        pageWriter.write_u8(OggConstants.version);
        pageWriter.write_u8((byte)flags);
        write_u64_le(ref pageWriter, (ulong)granulePosition);
        pageWriter.write_u32_le(serialNumber);
        pageWriter.write_u32_le((uint)pageNumber);
        pageWriter.write_u32_le(0);

        pageWriter.write_u8((byte)segmentCount);

        var remaining = packetData.Length;
        for (var i = 0; i < segmentCount; i++)
        {
            var segSize = System.Math.Min(remaining, 255);
            pageWriter.write_u8((byte)segSize);
            remaining -= segSize;
        }

        pageWriter.write(packetData);

        var written = page[..pageWriter.position];
        var crc = compute_ogg_crc(written);
        written[22] = (byte)(crc & 0xFF);
        written[23] = (byte)((crc >> 8) & 0xFF);
        written[24] = (byte)((crc >> 16) & 0xFF);
        written[25] = (byte)((crc >> 24) & 0xFF);

        writer.write([.. written]);
    }

    private static void write_u64_le(ref ByteBufferWriter writer, ulong value)
    {
        writer.write_u32_le((uint)(value & 0xFFFFFFFF));
        writer.write_u32_le((uint)(value >> 32));
    }

    private static uint compute_ogg_crc(ReadOnlySpan<byte> data)
    {
        var crc = new Crc32(Crc32.normal_polynomial, 0, 0, false);
        crc.update(data);
        return crc.value;
    }

    #endregion
}