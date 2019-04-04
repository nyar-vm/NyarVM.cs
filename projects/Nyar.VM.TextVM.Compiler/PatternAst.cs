using System.Collections.Immutable;

namespace Nyar.VM.TextVM.Compiler;

/// <summary>
/// 抽象语法树节点的基类。
/// </summary>
public abstract record AstNode
{
    /// <summary>
    /// 源位置（字符偏移量），用于错误报告。
    /// </summary>
    public Int32 Position { get; init; }

    /// <summary>
    /// 节点长度（字符数）。
    /// </summary>
    public Int32 Length { get; init; }
}

/// <summary>
/// 字面量字符串节点（转义字符或普通字符）。
/// </summary>
public sealed record LiteralNode(String Value) : AstNode;

/// <summary>
/// 字符类节点，如 [a-z]。
/// </summary>
public sealed record CharClassNode(ImmutableArray<CharRange> Ranges, Boolean Negated) : AstNode;

/// <summary>
/// 字符类范围内的一个区间。
/// </summary>
public readonly record struct CharRange(Char Lo, Char Hi);

/// <summary>
/// 连接节点（序列）。
/// </summary>
public sealed record ConcatNode(ImmutableArray<AstNode> Children) : AstNode;

/// <summary>
/// 选择节点（|）。
/// </summary>
public sealed record AltNode(AstNode Left, AstNode Right) : AstNode;

/// <summary>
/// Kleene 星号节点（*）。
/// </summary>
public sealed record StarNode(AstNode Inner) : AstNode;

/// <summary>
/// 加号节点（+）。
/// </summary>
public sealed record PlusNode(AstNode Inner) : AstNode;

/// <summary>
/// 可选节点（?）。
/// </summary>
public sealed record OptionalNode(AstNode Inner) : AstNode;

/// <summary>
/// 交集节点（&amp;），用于布尔正则表达式。
/// </summary>
public sealed record IntersectNode(AstNode Left, AstNode Right) : AstNode;

/// <summary>
/// 补集节点（!），用于布尔正则表达式。
/// </summary>
public sealed record ComplementNode(AstNode Inner) : AstNode;

/// <summary>
/// 差集节点（-），用于布尔正则表达式。
/// </summary>
public sealed record DifferenceNode(AstNode Left, AstNode Right) : AstNode;

/// <summary>
/// 捕获组节点。
/// </summary>
public sealed record CaptureNode(AstNode Inner, Int32 GroupId) : AstNode;

/// <summary>
/// 反向引用节点（如 \1）。
/// </summary>
public sealed record BackrefNode(Int32 GroupId) : AstNode;

/// <summary>
/// 锚点节点（^、$、\b）。
/// </summary>
public sealed record AnchorNode(AnchorKind Kind) : AstNode;

/// <summary>
/// 锚点类型。
/// </summary>
public enum AnchorKind
{
    /// <summary>
    /// 字符串起始（^）。
    /// </summary>
    Start,

    /// <summary>
    /// 字符串结束（$）。
    /// </summary>
    End,

    /// <summary>
    /// 单词边界（\b）。
    /// </summary>
    WordBoundary,
}

/// <summary>
/// 任意字符节点（.）。
/// </summary>
public sealed record AnyNode : AstNode;
