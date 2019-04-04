namespace Std.Data.Binary.Wav.Data;

/// <summary>
///     WAV 音频格式常量的
/// </summary>
public static class WavConstants
{
    /// <summary>
    ///     RIFF 字符串的
    /// </summary>
    public const string riff_tag = "RIFF";

    /// <summary>
    ///     WAVE 字符串的
    /// </summary>
    public const string wave_tag = "WAVE";

    /// <summary>
    ///     RIFF 魔数的
    /// </summary>
    public static ReadOnlySpan<byte> riff_magic => "RIFF"u8;

    /// <summary>
    ///     WAVE 魔数的
    /// </summary>
    public static ReadOnlySpan<byte> wave_magic => "WAVE"u8;

    /// <summary>
    ///     fmt 子块 ID的
    /// </summary>
    public static ReadOnlySpan<byte> fmt_chunk_id => "fmt "u8;

    /// <summary>
    ///     data 子块 ID的
    /// </summary>
    public static ReadOnlySpan<byte> data_chunk_id => "data"u8;
}

/// <summary>
///     WAV 音频格式标签的
/// </summary>
public enum WavFormatTag : ushort
{
    /// <summary>
    ///     PCM（无压缩）的
    /// </summary>
    pcm = 1,

    /// <summary>
    ///     IEEE 浮点的
    /// </summary>
    ieee_float = 3,

    /// <summary>
    ///     A-Law的
    /// </summary>
    a_law = 6,

    /// <summary>
    ///     μ-Law的
    /// </summary>
    mu_law = 7,

    /// <summary>
    ///     ADPCM的
    /// </summary>
    adpcm = 2,

    /// <summary>
    ///     IMA ADPCM的
    /// </summary>
    ima_adpcm = 0x0011,

    /// <summary>
    ///     WAVE_FORMAT_EXTENSIBLE的
    /// </summary>
    extensible = 0xFFFE
}