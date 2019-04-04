namespace Core.Media;

/// <summary>
///     声道布局枚举，定义音频的声道排列方式。
/// </summary>
public enum ChannelLayout
{
    /// <summary>
    ///     单声道。
    /// </summary>
    mono,

    /// <summary>
    ///     立体声（左右双声道）。
    /// </summary>
    stereo,

    /// <summary>
    ///     5.1 环绕声（6 声道）。
    /// </summary>
    surround5_1,

    /// <summary>
    ///     7.1 环绕声（8 声道）。
    /// </summary>
    surround7_1
}