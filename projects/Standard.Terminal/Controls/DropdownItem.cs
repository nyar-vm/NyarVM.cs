namespace Std.Terminal.Controls;

/// <summary>
///     下拉项
/// </summary>
public sealed class DropdownItem
{
    /// <summary>
    ///     项唯一标识
    /// </summary>
    public string id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    ///     显示文本
    /// </summary>
    public string text { get; set; } = string.Empty;

    /// <summary>
    ///     关联值
    /// </summary>
    public object? value { get; set; }

    /// <summary>
    ///     是否启用
    /// </summary>
    public bool is_enabled { get; set; } = true;

    /// <summary>
    ///     是否为分隔线
    /// </summary>
    public bool is_separator { get; set; }
}