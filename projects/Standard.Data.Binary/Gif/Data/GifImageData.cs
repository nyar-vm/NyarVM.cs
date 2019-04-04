namespace Std.Data.Binary.Gif.Data;

/// <summary>
///     GIF 图像文件数据——完整的 GIF 文件结构表示
/// </summary>
/// <remarks>
///     包含逻辑屏幕描述符、全局调色板、所有帧数据、扩展块等的
///     用于解码后的结构化表示，可传递给编码器重新编码的
/// </remarks>
public sealed class GifImageData
{
    /// <summary>
    ///     逻辑屏幕宽度
    /// </summary>
    public int width { get; init; }


    /// <summary>
    ///     逻辑屏幕高度
    /// </summary>
    public int height { get; init; }


    /// <summary>
    ///     全局调色板（的3 字节一组：R, G, B的
    /// </summary>
    public byte[] global_color_table { get; init; } = [];


    /// <summary>
    ///     背景色索的
    /// </summary>
    public byte background_color_index { get; init; }


    /// <summary>
    ///     像素宽高的
    /// </summary>
    public byte pixel_aspect_ratio { get; init; }


    /// <summary>
    ///     循环次数的 = 无限循环的1 = 未指定）
    /// </summary>
    public int loop_count { get; init; }


    /// <summary>
    ///     帧列的
    /// </summary>
    public IReadOnlyList<GifImageFrame> frames { get; init; } = [];
}

/// <summary>
///     GIF 图像帧——包含调色板索引数据和帧控制信息
/// </summary>
public sealed class GifImageFrame
{
    /// <summary>
    ///     帧左偏移
    /// </summary>
    public int left { get; init; }


    /// <summary>
    ///     帧上偏移
    /// </summary>
    public int top { get; init; }


    /// <summary>
    ///     帧宽的
    /// </summary>
    public int width { get; init; }


    /// <summary>
    ///     帧高的
    /// </summary>
    public int height { get; init; }


    /// <summary>
    ///     局部调色板（每 3 字节一组：R, G, B的
    /// </summary>
    public byte[] local_color_table { get; init; } = [];


    /// <summary>
    ///     帧延迟时间（1/100 秒）
    /// </summary>
    public int delay_centiseconds { get; init; }


    /// <summary>
    ///     帧处置方的
    /// </summary>
    public GifDisposalMethod disposal_method { get; init; }


    /// <summary>
    ///     是否有透明的
    /// </summary>
    public bool has_transparent_color { get; init; }


    /// <summary>
    ///     透明色索的
    /// </summary>
    public int transparent_color_index { get; init; }


    /// <summary>
    ///     是否隔行扫描
    /// </summary>
    public bool interlaced { get; init; }


    /// <summary>
    ///     像素索引数据（LZW 解压后的调色板索引）
    /// </summary>
    public byte[] indices { get; init; } = [];
}