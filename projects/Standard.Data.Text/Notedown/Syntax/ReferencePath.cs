namespace Std.Data.Text.Notedown.Syntax;

/// <summary>
///     AST 中的位置路径，用于标识引用来源
/// </summary>
public readonly record struct ReferencePath
{
    /// <summary>
    ///     块索引（-1 表示来自 Meta 或其他非块位置）
    /// </summary>
    public int block_index { get; init; }

    /// <summary>
    ///     行内索引（-1 表示块级引用如 CodeBlock）
    /// </summary>
    public int inline_index { get; init; }

    /// <summary>
    ///     创建块级引用路径
    /// </summary>
    public static ReferencePath from_block(int blockIndex)
    {
        return new ReferencePath { block_index = blockIndex, inline_index = -1 };
    }

    /// <summary>
    ///     创建行内引用路径
    /// </summary>
    public static ReferencePath from_inline(int blockIndex, int inlineIndex)
    {
        return new ReferencePath { block_index = blockIndex, inline_index = inlineIndex };
    }

    /// <summary>
    ///     创建 Meta 级别引用路径
    /// </summary>
    public static ReferencePath from_meta()
    {
        return new ReferencePath { block_index = -1, inline_index = -1 };
    }
}