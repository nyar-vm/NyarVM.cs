namespace Sonic.Interactive;

/// <summary>
/// 确认提示配置选项
/// </summary>
public sealed class ConfirmOptions
{
    /// <summary>提示符号</summary>
    public string prompt_symbol { get; set; } = "? ";

    /// <summary>默认值</summary>
    public bool default_value { get; set; } = true;

    /// <summary>是否为危险操作（显示警告样式）</summary>
    public bool dangerous { get; set; }

    /// <summary>自定义确认文本（默认 Y）</summary>
    public string yes_label { get; set; } = "Y";

    /// <summary>自定义拒绝文本（默认 N）</summary>
    public string no_label { get; set; } = "N";
}