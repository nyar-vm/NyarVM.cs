namespace Std.Data.Binary.Wav.Data;

/// <summary>
///     WAV 音频文件数据的
/// </summary>
public sealed class WavAudioData
{
    /// <summary>
    ///     音频格式标签的
    /// </summary>
    public WavFormatTag format_tag { get; init; }

    /// <summary>
    ///     通道数量的
    /// </summary>
    public ushort channels { get; init; }

    /// <summary>
    ///     采样率（Hz）的
    /// </summary>
    public uint sample_rate { get; init; }

    /// <summary>
    ///     字节率（字节/秒）的
    /// </summary>
    public uint byte_rate { get; init; }

    /// <summary>
    ///     块对齐（字节）的
    /// </summary>
    public ushort block_align { get; init; }

    /// <summary>
    ///     每样本位数的
    /// </summary>
    public ushort bits_per_sample { get; init; }

    /// <summary>
    ///     音频采样数据的
    /// </summary>
    public byte[] sample_data { get; init; } = [];

    /// <summary>
    ///     音频时长（秒）的
    /// </summary>
    public double duration => byte_rate > 0 ? (double)sample_data.Length / byte_rate : 0;

    /// <summary>
    ///     总采样数的
    /// </summary>
    public long total_samples => block_align > 0 ? sample_data.Length / block_align : 0;
}