namespace Sonic.Interactive;

/// <summary>
/// 单选列表配置选项
/// </summary>
public sealed class SelectOptions
{
    /// <summary>提示符号</summary>
    public string prompt_symbol { get; set; } = "? ";

    /// <summary>每页显示数量</summary>
    public int page_size { get; set; } = 10;

    /// <summary>是否启用搜索过滤</summary>
    public bool search_enabled { get; set; }

    /// <summary>是否显示键盘快捷键提示</summary>
    public bool show_instructions { get; set; } = true;

    /// <summary>默认选中的索引</summary>
    public int default_index { get; set; }
}