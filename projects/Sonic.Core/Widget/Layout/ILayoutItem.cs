namespace Core.Widget.Layout;

/// <summary>
///     ILayoutItem 接口
/// </summary>
public interface ILayoutItem
{
    /// <summary>
    ///     布局项宽度
    /// </summary>
    double width { get; }

    /// <summary>
    ///     布局项高度
    /// </summary>
    double height { get; }
}