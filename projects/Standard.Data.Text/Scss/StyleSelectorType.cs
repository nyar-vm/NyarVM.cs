namespace Std.Data.Text.Scss;

/// <summary>
///     样式选择器类型
/// </summary>
public enum StyleSelectorType
{
    /// <summary>
    ///     元素类型选择器
    /// </summary>
    type,


    /// <summary>
    ///     ID 选择器
    /// </summary>
    id,


    /// <summary>
    ///     类选择器
    /// </summary>
    @class,


    /// <summary>
    ///     伪类选择器
    /// </summary>
    pseudo_class,


    /// <summary>
    ///     父引用选择器（&）
    /// </summary>
    parent_ref
}