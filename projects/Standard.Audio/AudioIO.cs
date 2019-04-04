using Sonic.Audio.Formats;
using Sonic.Audio.Interop;
using Std.Data.Binary.Flac.Data;
using Std.Data.Binary.Flac.Decode;
using Std.Data.Binary.Flac.Encode;
using Std.Data.Binary.Wav.Decode;
using Std.Data.Binary.Wav.Encode;

namespace Sonic.Audio;

/// <summary>
///     音频输入输出静态类，提供统一的音频加载和保存门面，委托 Acorn 执行实际编解码。
/// </summary>
public static class AudioIO
{
    /// <summary>
    ///     懒加载的格式注册表实例。
    /// </summary>
    private static readonly Lazy<AudioFormatRegistry> _registry = new(() => new AudioFormatRegistry());

    /// <summary>
    ///     获取格式注册表实例。
    /// </summary>
    public static AudioFormatRegistry registry => _registry.Value;

    /// <summary>
    ///     从二进制数据加载音频帧，自动检测格式并委托 Acorn 解码器。
    /// </summary>
    /// <param name="data">音频二进制数据。</param>
    /// <returns>加载后的音频帧。</returns>
    public static AudioFrame<float> load(ReadOnlySpan<byte> data)
    {
        if (data.Length >= 4)
        {
            var riff = data[..4];

            if (riff[0] == 'R' && riff[1] == 'I' && riff[2] == 'F' && riff[3] == 'F') return load_wav(data);

            if (riff[0] == 'f' && riff[1] == 'L' && riff[2] == 'a' && riff[3] == 'C') return load_flac(data);
        }

        throw new InvalidDataException("无法识别的音频格式。");
    }

    /// <summary>
    ///     从文件路径加载音频帧，自动检测格式并委托 Acorn 解码器。
    /// </summary>
    /// <param name="path">音频文件路径。</param>
    /// <returns>加载后的音频帧。</returns>
    public static AudioFrame<float> load(string path)
    {
        var bytes = File.ReadAllBytes(path);
        return load(bytes);
    }

    /// <summary>
    ///     将音频帧保存为 WAV 格式的二进制数据。
    /// </summary>
    /// <param name="audio">音频帧。</param>
    /// <returns>WAV 格式的二进制数据。</returns>
    public static byte[] save_wav(AudioFrame<float> audio)
    {
        var wavData = AcornDataMapper.from_audio_frame(audio);
        var encoder = new WavEncoder();
        return encoder.encode(wavData);
    }

    /// <summary>
    ///     将音频帧保存为 FLAC 格式的二进制数据。
    /// </summary>
    /// <param name="audio">音频帧。</param>
    /// <returns>FLAC 格式的二进制数据。</returns>
    public static byte[] save_flac(AudioFrame<float> audio)
    {
        var layout = audio.layout;
        var flacData = new FlacAudioData
        {
            channels = audio.channels,
            sample_rate = audio.sample_rate,
            bits_per_sample = 32,
            total_samples = audio.samples.Length / audio.channels,
            min_block_size = 4096,
            max_block_size = 4096,
            md5_checksum = new byte[16]
        };

        var encoder = new FlacEncoder();
        return encoder.encode(flacData);
    }

    /// <summary>
    ///     从二进制数据加载 WAV 音频帧。
    /// </summary>
    /// <param name="data">WAV 二进制数据。</param>
    /// <returns>加载后的音频帧。</returns>
    private static AudioFrame<float> load_wav(ReadOnlySpan<byte> data)
    {
        var decoder = new WavDecoder(data);
        var wavData = decoder.decode();
        return AcornDataMapper.to_audio_frame(wavData);
    }

    /// <summary>
    ///     从二进制数据加载 FLAC 音频帧。
    /// </summary>
    /// <param name="data">FLAC 二进制数据。</param>
    /// <returns>加载后的音频帧。</returns>
    private static AudioFrame<float> load_flac(ReadOnlySpan<byte> data)
    {
        var decoder = new FlacDecoder(data);
        var flacData = decoder.decode();
        return AcornDataMapper.to_audio_frame(flacData);
    }
}