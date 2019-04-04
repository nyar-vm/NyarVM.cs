using System;

namespace Core.Media.Audio;

/// <summary>
///     标记音频类型，指定采样率、声道数和采样格式。
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
public sealed class AudioAttribute : Attribute
{
    /// <summary>
    ///     获取或设置采样率（Hz），默认为 44100。
    /// </summary>
    public int sample_rate { get; init; } = 44100;

    /// <summary>
    ///     获取或设置声道数，默认为 2。
    /// </summary>
    public int channels { get; init; } = 2;

    /// <summary>
    ///     获取或设置采样格式，默认为 <see cref="SampleFormat.float32" />。
    /// </summary>
    public SampleFormat sample_format { get; init; } = SampleFormat.float32;
}