namespace Std.App.Client;

/// <summary>
///     统一组件接口，与宿主无关的 UI 组件模型
/// </summary>
public interface IComponent
{
    /// <summary>
    ///     组件唯一标识
    /// </summary>
    string Id { get; }

    /// <summary>
    ///     子组件集合
    /// </summary>
    IReadOnlyList<IComponent> Children { get; }

    /// <summary>
    ///     父组件
    /// </summary>
    IComponent? Parent { get; }

    /// <summary>
    ///     获取指定键的属性值
    /// </summary>
    /// <param name="key">属性键</param>
    object? GetProperty(string key);

    /// <summary>
    ///     设置属性值，若值变更则触发绑定更新
    /// </summary>
    /// <param name="key">属性键</param>
    /// <param name="value">属性值</param>
    void SetProperty(string key, object? value);

    /// <summary>
    ///     添加子组件
    /// </summary>
    /// <param name="child">子组件</param>
    void AddChild(IComponent child);

    /// <summary>
    ///     移除子组件
    /// </summary>
    /// <param name="child">子组件</param>
    void RemoveChild(IComponent child);
}