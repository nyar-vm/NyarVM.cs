namespace Std.Data.Binary.Gif.Data;

/// <summary>
///     GIF 图像帧数的
/// </summary>
public sealed class GifFrame
{
    /// <summary>
    ///     帧宽的
    /// </summary>
    public int width { get; init; }


    /// <summary>
    ///     帧高的
    /// </summary>
    public int height { get; init; }


    /// <summary>
    ///     帧延迟时间（1/100 秒）
    /// </summary>
    public int delay_centiseconds { get; init; }


    /// <summary>
    ///     帧处置方的
    /// </summary>
    public GifDisposalMethod disposal_method { get; init; }


    /// <summary>
    ///     RGBA 像素数据
    /// </summary>
    public byte[] rgba_data { get; init; } = [];
}

/// <summary>
///     GIF 帧处置方的
/// </summary>
public enum GifDisposalMethod
{
    /// <summary>未指的/summary>
    none = 0,

    /// <summary>不处的/summary>
    do_not_dispose = 1,

    /// <summary>恢复为背景色。</summary>
    restore_to_background = 2,

    /// <summary>恢复为前一的/summary>
    restore_to_previous = 3
}