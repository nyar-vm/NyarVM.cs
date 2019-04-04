using Core.Media;

namespace Sonic.Audio.DSP;

/// <summary>
///     音频数字信号处理操作静态类，提供重采样、重混音、归一化、淡入淡出和增益等基础 DSP 能力。
/// </summary>
public static class AudioOps
{
    /// <summary>
    ///     对音频帧执行重采样，使用线性插值将采样率转换为目标采样率。
    /// </summary>
    /// <param name="audio">输入音频帧。</param>
    /// <param name="target_sample_rate">目标采样率。</param>
    /// <returns>重采样后的音频帧。</returns>
    public static AudioFrame<float> resample(AudioFrame<float> audio, int target_sample_rate)
    {
        if (audio.sample_rate == target_sample_rate) return audio;

        var ratio = (double)target_sample_rate / audio.sample_rate;
        var inputSamples = audio.readonly_span();
        var totalInputFrames = inputSamples.Length / audio.channels;
        var totalOutputFrames = (int)(totalInputFrames * ratio);
        var outputSamples = new float[totalOutputFrames * audio.channels];

        for (var i = 0; i < totalOutputFrames; i++)
        {
            var srcPos = i / ratio;

            for (var ch = 0; ch < audio.channels; ch++)
            {
                var srcIdx = (int)srcPos * audio.channels + ch;
                var nextIdx = Math.Min(srcIdx + audio.channels, inputSamples.Length - 1);
                var frac = (float)(srcPos - (int)srcPos);

                outputSamples[i * audio.channels + ch] =
                    inputSamples[srcIdx] * (1.0f - frac) + inputSamples[nextIdx] * frac;
            }
        }

        return new AudioFrame<float>(target_sample_rate, audio.channels, audio.format, audio.layout, audio.timestamp,
            outputSamples);
    }

    /// <summary>
    ///     对音频帧执行声道重混音，将声道布局转换为目标布局。
    /// </summary>
    /// <param name="audio">输入音频帧。</param>
    /// <param name="target_layout">目标声道布局。</param>
    /// <returns>重混音后的音频帧。</returns>
    public static AudioFrame<float> remix(AudioFrame<float> audio, ChannelLayout target_layout)
    {
        var targetChannels = target_layout switch
        {
            ChannelLayout.mono => 1,
            ChannelLayout.stereo => 2,
            ChannelLayout.surround5_1 => 6,
            ChannelLayout.surround7_1 => 8,
            _ => audio.channels
        };

        if (targetChannels == audio.channels) return audio;

        var input = audio.readonly_span();
        var totalFrames = input.Length / audio.channels;
        var output = new float[totalFrames * targetChannels];

        for (var i = 0; i < totalFrames; i++)
            if (audio.channels == 2 && targetChannels == 1)
            {
                var left = input[i * 2];
                var right = input[i * 2 + 1];
                output[i] = (left + right) * 0.5f;
            }
            else if (audio.channels == 1 && targetChannels == 2)
            {
                var mono = input[i];
                output[i * 2] = mono;
                output[i * 2 + 1] = mono;
            }
            else
            {
                for (var ch = 0; ch < targetChannels; ch++)
                    if (ch < audio.channels)
                        output[i * targetChannels + ch] = input[i * audio.channels + ch];
                    else
                        output[i * targetChannels + ch] = 0.0f;
            }

        return new AudioFrame<float>(audio.sample_rate, targetChannels, audio.format, target_layout, audio.timestamp,
            output);
    }

    /// <summary>
    ///     对音频帧执行峰值归一化，将采样峰值调整到目标峰值。
    /// </summary>
    /// <param name="audio">输入音频帧。</param>
    /// <param name="target_peak">目标峰值（0.0 ~ 1.0）。</param>
    /// <returns>归一化后的音频帧。</returns>
    public static AudioFrame<float> normalize(AudioFrame<float> audio, float target_peak)
    {
        var input = audio.readonly_span();
        var maxAbs = 0.0f;

        for (var i = 0; i < input.Length; i++)
        {
            var abs = Math.Abs(input[i]);
            if (abs > maxAbs) maxAbs = abs;
        }

        if (maxAbs < 1e-10f) return audio;

        var gain = target_peak / maxAbs;
        return amplify(audio, gain);
    }

    /// <summary>
    ///     对音频帧应用淡入效果，在指定毫秒数内从零渐变到原始音量。
    /// </summary>
    /// <param name="audio">输入音频帧。</param>
    /// <param name="milliseconds">淡入时长（毫秒）。</param>
    /// <returns>淡入后的音频帧。</returns>
    public static AudioFrame<float> fade_in(AudioFrame<float> audio, int milliseconds)
    {
        var fadeSamples = (int)(audio.sample_rate * milliseconds / 1000.0) * audio.channels;
        var input = audio.readonly_span();
        var output = new float[input.Length];

        Array.Copy(input.ToArray(), output, input.Length);

        var actualFade = Math.Min(fadeSamples, output.Length);

        for (var i = 0; i < actualFade; i++) output[i] = input[i] * ((float)i / actualFade);

        return new AudioFrame<float>(audio.sample_rate, audio.channels, audio.format, audio.layout, audio.timestamp,
            output);
    }

    /// <summary>
    ///     对音频帧应用淡出效果，在指定毫秒数内从原始音量渐变到零。
    /// </summary>
    /// <param name="audio">输入音频帧。</param>
    /// <param name="milliseconds">淡出时长（毫秒）。</param>
    /// <returns>淡出后的音频帧。</returns>
    public static AudioFrame<float> fade_out(AudioFrame<float> audio, int milliseconds)
    {
        var fadeSamples = (int)(audio.sample_rate * milliseconds / 1000.0) * audio.channels;
        var input = audio.readonly_span();
        var output = new float[input.Length];

        Array.Copy(input.ToArray(), output, input.Length);

        var actualFade = Math.Min(fadeSamples, output.Length);
        var fadeStart = output.Length - actualFade;

        for (var i = 0; i < actualFade; i++)
            output[fadeStart + i] = input[fadeStart + i] * (1.0f - (float)i / actualFade);

        return new AudioFrame<float>(audio.sample_rate, audio.channels, audio.format, audio.layout, audio.timestamp,
            output);
    }

    /// <summary>
    ///     对音频帧应用增益，将所有采样值乘以指定增益系数。
    /// </summary>
    /// <param name="audio">输入音频帧。</param>
    /// <param name="gain">增益系数。</param>
    /// <returns>增益后的音频帧。</returns>
    public static AudioFrame<float> amplify(AudioFrame<float> audio, float gain)
    {
        var input = audio.readonly_span();
        var output = new float[input.Length];

        for (var i = 0; i < input.Length; i++) output[i] = input[i] * gain;

        return new AudioFrame<float>(audio.sample_rate, audio.channels, audio.format, audio.layout, audio.timestamp,
            output);
    }
}