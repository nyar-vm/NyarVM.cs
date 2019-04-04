namespace Std.Data.Binary.Opus.Data;

/// <summary>
///     Opus 音频数据的
/// </summary>
public sealed class OpusAudioData
{
    /// <summary>
    ///     通道数量的
    /// </summary>
    public byte channels { get; init; }

    /// <summary>
    ///     采样率的
    /// </summary>
    public uint sample_rate { get; init; }

    /// <summary>
    ///     预跳过采样数的
    /// </summary>
    public ushort pre_skip { get; init; }

    /// <summary>
    ///     输出增益的
    /// </summary>
    public short output_gain { get; init; }

    /// <summary>
    ///     通道映射族的
    /// </summary>
    public byte channel_mapping_family { get; init; }
}