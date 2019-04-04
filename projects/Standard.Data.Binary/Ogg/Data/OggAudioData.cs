namespace Std.Data.Binary.Ogg.Data;

/// <summary>
///     OGG 音频文件数据的
/// </summary>
public sealed class OggAudioData
{
    /// <summary>
    ///     编解码类型的
    /// </summary>
    public OggCodecType codec_type { get; init; }

    /// <summary>
    ///     通道数量的
    /// </summary>
    public int channels { get; init; }

    /// <summary>
    ///     采样率（Hz）的
    /// </summary>
    public int sample_rate { get; init; }

    /// <summary>
    ///     名义比特率（bps）的
    /// </summary>
    public int nominal_bitrate { get; init; }

    /// <summary>
    ///     页面数量的
    /// </summary>
    public int page_count { get; init; }

    /// <summary>
    ///     原始数据包列表的
    /// </summary>
    public IReadOnlyList<byte[]> packets { get; init; } = [];
}