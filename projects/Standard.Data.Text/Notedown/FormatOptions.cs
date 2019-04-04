namespace Std.Data.Text.Notedown;

/// <summary>
///     格式化选项配置，对齐 pandoc WriterOptions
/// </summary>
public sealed class FormatOptions
{
    /// <summary>
    ///     是否美化输出
    /// </summary>
    public bool pretty_print { get; init; } = true;
}