namespace Std.Data.Text.Notedown.Syntax;

/// <summary>
///     Notedown 行内节点抽象基类，对齐 pandoc Inline
/// </summary>
public abstract record NotedownInline
{
    /// <summary>
    ///     行内节点类型
    /// </summary>
    public abstract NotedownInlineType inline_type { get; }
}