namespace Plotter.Schema;

/// <summary>
///     画布尺寸配置，包含宽度和高度。
/// </summary>
public class SizeConfig
{
    /// <summary>
    ///     画布宽度（像素）。
    /// </summary>
    public int width { get; set; } = 800;

    /// <summary>
    ///     画布高度（像素）。
    /// </summary>
    public int height { get; set; } = 600;
}