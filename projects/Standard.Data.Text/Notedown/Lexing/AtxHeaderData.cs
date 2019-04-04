namespace Std.Data.Text.Notedown.Lexing;

/// <summary>
///     ATX 标题数据
/// </summary>
public readonly record struct AtxHeaderData(int level, string content);