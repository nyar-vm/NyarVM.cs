namespace Std.Data.Text.Notedown.Lexing;

/// <summary>
///     任务列表项数据
/// </summary>
public readonly record struct TaskListItemData(string content, bool is_checked);