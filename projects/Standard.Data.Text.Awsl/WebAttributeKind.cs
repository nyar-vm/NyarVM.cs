namespace Std.Data.Text.Awsl;

/// <summary>
///     AWSL 属性种类
/// </summary>
public enum WebAttributeKind
{
    /// <summary>
    ///     普通属性（class="foo", id="bar"）
    /// </summary>
    normal,

    /// <summary>
    ///     事件绑定（@click="handler"）
    /// </summary>
    event_binding,

    /// <summary>
    ///     响应式数据绑定（@bind="value"）
    /// </summary>
    data_binding,

    /// <summary>
    ///     HTML 属性绑定（class=, style=, id= 等）
    /// </summary>
    property_binding
}