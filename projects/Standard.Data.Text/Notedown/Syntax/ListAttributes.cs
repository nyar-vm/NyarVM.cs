namespace Std.Data.Text.Notedown.Syntax;

/// <summary>
///     列表编号属性
/// </summary>
public readonly record struct ListAttributes
{
    /// <summary>
    ///     起始编号
    /// </summary>
    public int start_number { get; init; }

    /// <summary>
    ///     编号样式
    /// </summary>
    public ListNumberStyle number_style { get; init; }

    /// <summary>
    ///     编号分隔符
    /// </summary>
    public ListNumberDelim delimiter { get; init; }

    /// <summary>
    ///     默认列表属性
    /// </summary>
    public static ListAttributes @default => new()
        { start_number = 1, number_style = ListNumberStyle.@default, delimiter = ListNumberDelim.@default };
}