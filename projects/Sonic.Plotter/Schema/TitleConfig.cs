namespace Plotter.Schema;

/// <summary>
///     标题配置，包含主标题和副标题。
/// </summary>
public class TitleConfig
{
    /// <summary>
    ///     主标题文本。
    /// </summary>
    public string main_text { get; set; } = "";

    /// <summary>
    ///     副标题文本。
    /// </summary>
    public string sub_text { get; set; } = "";
}