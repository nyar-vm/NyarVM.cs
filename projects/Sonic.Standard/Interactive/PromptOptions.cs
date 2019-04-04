namespace Sonic.Interactive;

/// <summary>
/// 交互式提示选项
/// </summary>
public sealed class PromptOptions
{
    /// <summary>
    /// 提示符号
    /// </summary>
    public string prompt_symbol { get; set; } = "? ";

    /// <summary>
    /// 是否启用搜索
    /// </summary>
    public bool search_enabled { get; set; }

    /// <summary>
    /// 每页显示数量
    /// </summary>
    public int page_size { get; set; } = 10;

    /// <summary>
    /// 是否启用历史记录
    /// </summary>
    public bool history_enabled { get; set; }
}