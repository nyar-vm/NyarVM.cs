namespace Std.Data.Binary.Ogg.Data;

/// <summary>
///     OGG 容器格式常量的
/// </summary>
public static class OggConstants
{
    /// <summary>
    ///     OGG 页面头部大小的7 字节）的
    /// </summary>
    public const int page_header_size = 27;

    /// <summary>
    ///     OGG 版本号（始终的0）的
    /// </summary>
    public const byte version = 0;

    /// <summary>
    ///     OGG 页面捕获模式的OggS"）的
    /// </summary>
    public static ReadOnlySpan<byte> capture_pattern => "OggS"u8;

    /// <summary>
    ///     Vorbis 音频标识的vorbis"）的
    /// </summary>
    public static ReadOnlySpan<byte> vorbis_id => "vorbis"u8;

    /// <summary>
    ///     Opus 音频标识的OpusHead"）的
    /// </summary>
    public static ReadOnlySpan<byte> opus_id => "OpusHead"u8;
}

/// <summary>
///     OGG 页面标志位的
/// </summary>
[Flags]
public enum OggPageFlags : byte
{
    /// <summary>
    ///     无标志的
    /// </summary>
    none = 0,

    /// <summary>
    ///     延续页的
    /// </summary>
    continued = 0x01,

    /// <summary>
    ///     逻辑流的第一页的
    /// </summary>
    begin_of_stream = 0x02,

    /// <summary>
    ///     逻辑流的最后一页的
    /// </summary>
    end_of_stream = 0x04
}

/// <summary>
///     OGG 音频编解码类型的
/// </summary>
public enum OggCodecType : byte
{
    /// <summary>
    ///     未知编解码的
    /// </summary>
    unknown = 0,

    /// <summary>
    ///     Vorbis 音频的
    /// </summary>
    vorbis = 1,

    /// <summary>
    ///     Opus 音频的
    /// </summary>
    opus = 2
}