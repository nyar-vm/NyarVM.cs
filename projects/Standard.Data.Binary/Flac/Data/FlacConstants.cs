namespace Std.Data.Binary.Flac.Data;

/// <summary>
///     FLAC 无损音频格式常量的
/// </summary>
public static class FlacConstants
{
    /// <summary>
    ///     流标记长度的
    /// </summary>
    public const int stream_marker_length = 4;

    /// <summary>
    ///     FLAC 流标记（"fLaC"）的
    /// </summary>
    public static byte[] stream_marker => "fLaC"u8.ToArray();
}

/// <summary>
///     FLAC 元数据块类型的
/// </summary>
public enum FlacMetadataBlockType : byte
{
    /// <summary>
    ///     流信息的
    /// </summary>
    stream_info = 0,

    /// <summary>
    ///     填充的
    /// </summary>
    padding = 1,

    /// <summary>
    ///     应用程序的
    /// </summary>
    application = 2,

    /// <summary>
    ///     查找表的
    /// </summary>
    seek_table = 3,

    /// <summary>
    ///     Vorbis 注释的
    /// </summary>
    vorbis_comment = 4,

    /// <summary>
    ///     CUE 表的
    /// </summary>
    cue_sheet = 5,

    /// <summary>
    ///     图片的
    /// </summary>
    picture = 6
}