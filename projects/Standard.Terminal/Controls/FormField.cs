namespace Std.Terminal.Controls;

/// <summary>
///     表单字段定义
/// </summary>
public sealed class FormField
{
    /// <summary>
    ///     字段标签
    /// </summary>
    public string label { get; set; } = string.Empty;

    /// <summary>
    ///     字段名称（标识符）
    /// </summary>
    public string name { get; set; } = string.Empty;

    /// <summary>
    ///     输入控件
    /// </summary>
    public View? input { get; set; }

    /// <summary>
    ///     验证提示
    /// </summary>
    public string? error_message { get; set; }

    /// <summary>
    ///     输入验证委托
    /// </summary>
    public Func<string?, string?>? validator { get; set; }
}