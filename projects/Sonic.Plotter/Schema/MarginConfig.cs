namespace Plotter.Schema;

/// <summary>
///     边距配置，包含上、下、左、右边距。
/// </summary>
public class MarginConfig
{
    /// <summary>
    ///     上边距（像素）。
    /// </summary>
    public int top { get; set; } = 20;

    /// <summary>
    ///     下边距（像素）。
    /// </summary>
    public int bottom { get; set; } = 30;

    /// <summary>
    ///     左边距（像素）。
    /// </summary>
    public int left { get; set; } = 40;

    /// <summary>
    ///     右边距（像素）。
    /// </summary>
    public int right { get; set; } = 20;
}