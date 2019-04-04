namespace Std.Data.Text.Notedown;

/// <summary>
///     Notedown 语言配置
/// </summary>
public sealed class NotedownLanguageConfig
{
    /// <summary>
    ///     是否启用 GFM 表格扩展
    /// </summary>
    public bool enable_tables { get; init; } = true;

    /// <summary>
    ///     是否启用任务列表扩展
    /// </summary>
    public bool enable_task_lists { get; init; } = true;

    /// <summary>
    ///     是否启用删除线扩展
    /// </summary>
    public bool enable_strikethrough { get; init; } = true;

    /// <summary>
    ///     是否启用数学公式扩展
    /// </summary>
    public bool enable_math { get; init; } = true;

    /// <summary>
    ///     是否启用脚注扩展
    /// </summary>
    public bool enable_footnotes { get; init; } = true;

    /// <summary>
    ///     是否启用定义列表扩展
    /// </summary>
    public bool enable_definition_lists { get; init; } = true;

    /// <summary>
    ///     是否启用行块扩展
    /// </summary>
    public bool enable_line_blocks { get; init; } = true;

    /// <summary>
    ///     是否启用上标/下标扩展
    /// </summary>
    public bool enable_super_subscript { get; init; } = true;

    /// <summary>
    ///     是否启用高亮扩展
    /// </summary>
    public bool enable_highlight { get; init; } = true;

    /// <summary>
    ///     默认配置实例
    /// </summary>
    public static NotedownLanguageConfig @default { get; } = new();
}