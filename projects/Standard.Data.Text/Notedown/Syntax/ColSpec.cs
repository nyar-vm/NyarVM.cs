namespace Std.Data.Text.Notedown.Syntax;

/// <summary>
///     列规格
/// </summary>
public readonly record struct ColSpec
{
    /// <summary>
    ///     对齐方式
    /// </summary>
    public Alignment alignment { get; init; }

    /// <summary>
    ///     相对列宽（0 表示默认）
    /// </summary>
    public double width { get; init; }
}