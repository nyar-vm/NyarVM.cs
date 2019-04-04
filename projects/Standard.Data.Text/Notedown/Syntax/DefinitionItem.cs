namespace Std.Data.Text.Notedown.Syntax;

/// <summary>
///     定义列表项
/// </summary>
public readonly record struct DefinitionItem
{
    /// <summary>
    ///     术语行内内容
    /// </summary>
    public IReadOnlyList<NotedownInline> term { get; init; }

    /// <summary>
    ///     定义块列表
    /// </summary>
    public IReadOnlyList<IReadOnlyList<NotedownBlock>> definitions { get; init; }
}