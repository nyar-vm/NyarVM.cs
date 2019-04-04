using Std.Data.Binary.Frame;
using Std.Data.Binary.Opus.Data;

namespace Std.Data.Binary.Opus.Decode;

/// <summary>
///     Opus 解码器的
/// </summary>
public ref struct OpusDecoder
{
    private ByteBuffer _buffer;

    /// <summary>
    ///     初始的<see cref="OpusDecoder" /> 结构的新实例的
    /// </summary>
    public OpusDecoder(ReadOnlySpan<byte> data)
    {
        _buffer = new ByteBuffer(data);
    }

    /// <summary>
    ///     当前位置的
    /// </summary>
    public int position
    {
        get => _buffer.position;
        set => _buffer.position = value;
    }

    /// <summary>
    ///     解码 Opus 头部的
    /// </summary>
    public OpusAudioData decode()
    {
        var id = _buffer.read_bytes(8).ToArray();

        if (!id.AsSpan().SequenceEqual(OpusConstants.opus_head)) throw new InvalidDataException("Opus 头部标识不匹配。");

        var version = _buffer.read_u8();
        var channels = _buffer.read_u8();
        var preSkip = _buffer.read_u16_le();
        var sampleRate = _buffer.read_u32_le();
        var outputGain = _buffer.read_i16_le();
        var channelMappingFamily = _buffer.read_u8();

        return new OpusAudioData
        {
            channels = channels,
            sample_rate = sampleRate,
            pre_skip = preSkip,
            output_gain = outputGain,
            channel_mapping_family = channelMappingFamily
        };
    }
}