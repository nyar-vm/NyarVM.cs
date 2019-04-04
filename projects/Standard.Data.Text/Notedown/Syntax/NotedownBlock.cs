namespace Std.Data.Text.Notedown.Syntax;

/// <summary>
///     Notedown 块级节点抽象基类，对齐 pandoc Block
/// </summary>
public abstract record NotedownBlock
{
    /// <summary>
    ///     块类型
    /// </summary>
    public abstract NotedownBlockType block_type { get; }
}