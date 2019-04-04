namespace Std.Data.Binary.Opus.Data;

/// <summary>
///     Opus 音频格式常量的
/// </summary>
public static class OpusConstants
{
    /// <summary>
    ///     Opus 头部大小的
    /// </summary>
    public const int header_size = 19;

    /// <summary>
    ///     Opus 头部标识的OpusHead"）的
    /// </summary>
    public static byte[] opus_head => "OpusHead"u8.ToArray();

    /// <summary>
    ///     Opus 标签标识的OpusTags"）的
    /// </summary>
    public static byte[] opus_tags => "OpusTags"u8.ToArray();
}