namespace Sonic.Interactive;

/// <summary>
/// 多选列表配置选项
/// </summary>
public sealed class MultiSelectOptions
{
    /// <summary>提示符号</summary>
    public string prompt_symbol { get; set; } = "? ";

    /// <summary>每页显示数量</summary>
    public int page_size { get; set; } = 10;

    /// <summary>最少选择数量</summary>
    public int min_selected { get; set; } = 1;

    /// <summary>最多选择数量（0 表示不限制）</summary>
    public int max_selected { get; set; }

    /// <summary>是否启用搜索过滤</summary>
    public bool search_enabled { get; set; }

    /// <summary>默认选中的索引集合</summary>
    public HashSet<int> default_selected { get; set; } = [];

    /// <summary>是否显示键盘快捷键提示</summary>
    public bool show_instructions { get; set; } = true;

    /// <summary>是否必须选择才能确认</summary>
    public bool required { get; set; } = true;
}