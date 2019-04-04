namespace Std.Data.Text.Notedown.Syntax;

/// <summary>
///     数学公式
/// </summary>
public sealed record Math : NotedownInline
{
    /// <summary>
    ///     创建数学公式
    /// </summary>
    public Math(MathType mathType, string text)
    {
        math_type = mathType;
        this.text = text;
    }

    /// <inheritdoc />
    public override NotedownInlineType inline_type => NotedownInlineType.math;

    /// <summary>
    ///     公式类型
    /// </summary>
    public MathType math_type { get; init; }

    /// <summary>
    ///     公式文本
    /// </summary>
    public string text { get; init; }
}