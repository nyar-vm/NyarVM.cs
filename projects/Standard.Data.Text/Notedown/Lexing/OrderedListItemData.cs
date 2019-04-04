namespace Std.Data.Text.Notedown.Lexing;

/// <summary>
///     有序列表项数据
/// </summary>
public readonly record struct OrderedListItemData(string content, int start_number);