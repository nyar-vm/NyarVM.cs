namespace Std.Terminal.Controls;

/// <summary>
///     标签页项定义
/// </summary>
public sealed class TabItem
{
    /// <summary>
    ///     标签 ID
    /// </summary>
    public string id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    ///     标签标题
    /// </summary>
    public string title { get; set; } = string.Empty;

    /// <summary>
    ///     标签内容控件
    /// </summary>
    public View? content { get; set; }

    /// <summary>
    ///     是否可关闭
    /// </summary>
    public bool closable { get; set; } = true;

    /// <summary>
    ///     是否激活
    /// </summary>
    public bool is_active { get; set; }

    /// <summary>
    ///     关闭事件
    /// </summary>
    public event Action<TabItem>? OnCloseRequested;

    internal void request_close()
    {
        OnCloseRequested?.Invoke(this);
    }
}