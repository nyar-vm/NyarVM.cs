namespace Core.Terminal;

/// <summary>
///     布局接口，描述组件的期望尺寸
/// </summary>
public interface ILayout
{
    /// <summary>
    ///     获取期望宽度
    /// </summary>
    int desired_width { get; }

    /// <summary>
    ///     获取期望高度
    /// </summary>
    int desired_height { get; }
}