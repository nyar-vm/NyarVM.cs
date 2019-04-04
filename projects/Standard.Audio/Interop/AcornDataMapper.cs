using Core.Media;
using Std.Data.Binary.Flac.Data;
using Std.Data.Binary.Wav.Data;

namespace Sonic.Audio.Interop;

/// <summary>
///     Acorn 数据映射器静态类，提供 Sonic.Standard.Audio 数据模型与 Acorn 二进制格式数据模型之间的转换能力。
/// </summary>
public static class AcornDataMapper
{
    /// <summary>
    ///     将 Acorn WAV 音频数据转换为 Sonic 音频帧。
    /// </summary>
    /// <param name="wav">Acorn WAV 音频数据。</param>
    /// <returns>Sonic 音频帧。</returns>
    public static AudioFrame<float> to_audio_frame(WavAudioData wav)
    {
        var channels = (int)wav.channels;
        var sampleRate = (int)wav.sample_rate;
        var bitsPerSample = (int)wav.bits_per_sample;
        var totalSamples = (int)wav.total_samples;
        var samples = new float[totalSamples * channels];
        var layout = channels switch
        {
            1 => ChannelLayout.mono,
            2 => ChannelLayout.stereo,
            6 => ChannelLayout.surround5_1,
            8 => ChannelLayout.surround7_1,
            _ => ChannelLayout.mono
        };

        if (wav.format_tag == WavFormatTag.ieee_float && bitsPerSample == 32)
        {
            var byteOffset = 0;
            for (var i = 0; i < samples.Length; i++)
            {
                samples[i] = BitConverter.ToSingle(wav.sample_data, byteOffset);
                byteOffset += 4;
            }
        }
        else if (wav.format_tag == WavFormatTag.pcm && bitsPerSample == 16)
        {
            var byteOffset = 0;
            for (var i = 0; i < samples.Length; i++)
            {
                var raw = BitConverter.ToInt16(wav.sample_data, byteOffset);
                samples[i] = raw / 32768.0f;
                byteOffset += 2;
            }
        }
        else if (wav.format_tag == WavFormatTag.pcm && bitsPerSample == 24)
        {
            var byteOffset = 0;
            for (var i = 0; i < samples.Length; i++)
            {
                var b0 = wav.sample_data[byteOffset];
                var b1 = wav.sample_data[byteOffset + 1];
                var b2 = wav.sample_data[byteOffset + 2];
                var raw = b0 | (b1 << 8) | (b2 << 16);
                if ((raw & 0x800000) != 0) raw |= unchecked((int)0xFF000000);

                samples[i] = raw / 8388608.0f;
                byteOffset += 3;
            }
        }
        else if (wav.format_tag == WavFormatTag.pcm && bitsPerSample == 32)
        {
            var byteOffset = 0;
            for (var i = 0; i < samples.Length; i++)
            {
                var raw = BitConverter.ToInt32(wav.sample_data, byteOffset);
                samples[i] = raw / 2147483648.0f;
                byteOffset += 4;
            }
        }
        else
        {
            var bytesPerSample = bitsPerSample / 8;
            var byteOffset = 0;
            for (var i = 0; i < samples.Length; i++)
            {
                if (byteOffset + bytesPerSample <= wav.sample_data.Length) samples[i] = 0.0f;

                byteOffset += bytesPerSample;
            }
        }

        return new AudioFrame<float>(sampleRate, channels, SampleFormat.float32, layout, 0, samples);
    }

    /// <summary>
    ///     将 Acorn FLAC 音频数据转换为 Sonic 音频帧。
    /// </summary>
    /// <param name="flac">Acorn FLAC 音频数据。</param>
    /// <returns>Sonic 音频帧。</returns>
    public static AudioFrame<float> to_audio_frame(FlacAudioData flac)
    {
        var channels = flac.channels;
        var sampleRate = flac.sample_rate;
        var totalSamples = (int)flac.total_samples;
        var samples = new float[totalSamples * channels];
        var layout = channels switch
        {
            1 => ChannelLayout.mono,
            2 => ChannelLayout.stereo,
            6 => ChannelLayout.surround5_1,
            8 => ChannelLayout.surround7_1,
            _ => ChannelLayout.mono
        };

        return new AudioFrame<float>(sampleRate, channels, SampleFormat.float32, layout, 0, samples);
    }

    /// <summary>
    ///     将 Sonic 音频帧转换为 Acorn WAV 音频数据。
    /// </summary>
    /// <param name="frame">Sonic 音频帧。</param>
    /// <returns>Acorn WAV 音频数据。</returns>
    public static WavAudioData from_audio_frame(AudioFrame<float> frame)
    {
        var channels = (ushort)frame.channels;
        var sampleRate = (uint)frame.sample_rate;
        var bitsPerSample = (ushort)32;
        var byteRate = (uint)(sampleRate * channels * (bitsPerSample / 8));
        var blockAlign = (ushort)(channels * (bitsPerSample / 8));

        var input = frame.readonly_span();
        var sampleData = new byte[input.Length * 4];

        for (var i = 0; i < input.Length; i++)
        {
            var bytes = BitConverter.GetBytes(input[i]);
            Array.Copy(bytes, 0, sampleData, i * 4, 4);
        }

        return new WavAudioData
        {
            format_tag = WavFormatTag.ieee_float,
            channels = channels,
            sample_rate = sampleRate,
            byte_rate = byteRate,
            block_align = blockAlign,
            bits_per_sample = bitsPerSample,
            sample_data = sampleData
        };
    }
}