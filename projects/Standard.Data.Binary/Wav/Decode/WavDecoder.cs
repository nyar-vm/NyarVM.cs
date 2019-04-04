using Std.Data.Binary.Frame;
using Std.Data.Binary.Wav.Data;

namespace Std.Data.Binary.Wav.Decode;

/// <summary>
///     WAV 文件解码器，的WAV 音频格式解码的C# 数据结构的
/// </summary>
/// <remarks>
///     WAV 的Microsoft/IBM 的标准音频格式，基于 RIFF 容器，广泛用于游戏和多媒体应用的
///     支持 PCM、IEEE Float、ADPCM 等多种编码格式的
/// </remarks>
public ref struct WavDecoder
{
    private ByteBuffer _buffer;

    /// <summary>
    ///     初始的<see cref="WavDecoder" /> 结构的新实例的
    /// </summary>
    /// <param name="data">WAV 二进制数据的/param>
    public WavDecoder(ReadOnlySpan<byte> data)
    {
        _buffer = new ByteBuffer(data);
    }

    /// <summary>
    ///     获取当前在流中的位置的
    /// </summary>
    public int position
    {
        get => _buffer.position;
        set => _buffer.position = value;
    }

    /// <summary>
    ///     解码 WAV 文件的
    /// </summary>
    /// <returns>WAV 音频数据的/returns>
    public WavAudioData decode()
    {
        var riffTag = _buffer.read_string(4);

        if (riffTag != WavConstants.riff_tag) throw new InvalidDataException($"WAV 文件签名无效，期的\"RIFF\"，实的\"{riffTag}\"");

        var fileSize = _buffer.read_u32_le();
        var waveTag = _buffer.read_string(4);

        if (waveTag != WavConstants.wave_tag) throw new InvalidDataException($"WAV 格式标识无效，期的\"WAVE\"，实的\"{waveTag}\"");

        WavFormatTag formatTag = 0;
        ushort channels = 0;
        uint sampleRate = 0;
        uint byteRate = 0;
        ushort blockAlign = 0;
        ushort bitsPerSample = 0;
        byte[] sampleData = [];

        while (!_buffer.is_end)
        {
            var chunkId = _buffer.read_string(4);
            var chunkSize = _buffer.read_u32_le();
            var chunkEnd = _buffer.position + (int)chunkSize;

            if (chunkId == "fmt ")
            {
                formatTag = (WavFormatTag)_buffer.read_u16_le();
                channels = _buffer.read_u16_le();
                sampleRate = _buffer.read_u32_le();
                byteRate = _buffer.read_u32_le();
                blockAlign = _buffer.read_u16_le();
                bitsPerSample = _buffer.read_u16_le();
            }
            else if (chunkId == "data")
            {
                sampleData = [.. _buffer.read_bytes((int)chunkSize)];
                break;
            }

            _buffer.position = chunkEnd;

            if (chunkSize % 2 != 0) _buffer.advance(1);
        }

        return new WavAudioData
        {
            format_tag = formatTag,
            channels = channels,
            sample_rate = sampleRate,
            byte_rate = byteRate,
            block_align = blockAlign,
            bits_per_sample = bitsPerSample,
            sample_data = sampleData
        };
    }

    /// <summary>
    ///     仅解的WAV 文件头信息的
    /// </summary>
    public (WavFormatTag FormatTag, ushort Channels, uint SampleRate, ushort BitsPerSample) decode_header()
    {
        var riffTag = _buffer.read_string(4);

        if (riffTag != WavConstants.riff_tag) throw new InvalidDataException($"WAV 文件签名无效，期的\"RIFF\"，实的\"{riffTag}\"");

        _buffer.advance(4);
        var waveTag = _buffer.read_string(4);

        if (waveTag != WavConstants.wave_tag) throw new InvalidDataException($"WAV 格式标识无效，期的\"WAVE\"，实的\"{waveTag}\"");

        while (!_buffer.is_end)
        {
            var chunkId = _buffer.read_string(4);
            var chunkSize = _buffer.read_u32_le();
            var chunkEnd = _buffer.position + (int)chunkSize;

            if (chunkId == "fmt ")
            {
                var formatTag = (WavFormatTag)_buffer.read_u16_le();
                var channels = _buffer.read_u16_le();
                var sampleRate = _buffer.read_u32_le();
                _buffer.advance(4);
                _buffer.advance(2);
                var bitsPerSample = _buffer.read_u16_le();

                return (formatTag, channels, sampleRate, bitsPerSample);
            }

            _buffer.position = chunkEnd;

            if (chunkSize % 2 != 0) _buffer.advance(1);
        }

        throw new InvalidDataException("WAV 文件缺少 fmt 块。");
    }
}