namespace Std.App.Client;

/// <summary>
///     属性绑定方向
/// </summary>
public enum BindingDirection
{
    /// <summary>
    ///     单向绑定：数据变更时更新视图
    /// </summary>
    OneWay,

    /// <summary>
    ///     双向绑定：数据和视图互相更新
    /// </summary>
    TwoWay
}

/// <summary>
///     属性绑定描述，连接数据源和视图属性
/// </summary>
public sealed class PropertyBinding
{
    /// <summary>
    ///     初始化属性绑定
    /// </summary>
    /// <param name="sourcePropertyName">数据源属性名</param>
    /// <param name="targetPropertyName">目标组件属性键</param>
    /// <param name="direction">绑定方向</param>
    public PropertyBinding(
        string sourcePropertyName,
        string targetPropertyName,
        BindingDirection direction = BindingDirection.OneWay)
    {
        SourcePropertyName = sourcePropertyName;
        TargetPropertyName = targetPropertyName;
        Direction = direction;
    }

    /// <summary>
    ///     数据源属性名
    /// </summary>
    public string SourcePropertyName { get; }

    /// <summary>
    ///     目标组件属性键
    /// </summary>
    public string TargetPropertyName { get; }

    /// <summary>
    ///     绑定方向
    /// </summary>
    public BindingDirection Direction { get; }
}