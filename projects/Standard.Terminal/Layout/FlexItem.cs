using Std.Terminal.Controls;

namespace Std.Terminal.Layout;

/// <summary>
///     Flex 子项弹性设置
/// </summary>
public sealed class FlexItem
{
    /// <summary>
    ///     创建弹性子项
    /// </summary>
    /// <param name="view">子控件</param>
    /// <param name="grow">弹性系数</param>
    /// <param name="basis">固定主轴大小</param>
    public FlexItem(View view, double grow = 0, int? basis = null)
    {
        this.view = view;
        this.grow = grow;
        this.basis = basis;
    }

    /// <summary>
    ///     子控件
    /// </summary>
    public View view { get; set; }

    /// <summary>
    ///     弹性系数（0 表示固定大小）
    /// </summary>
    public double grow { get; set; }

    /// <summary>
    ///     固定主轴大小
    /// </summary>
    public int? basis { get; set; }
}