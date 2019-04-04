using Std.Data.Text.Syntax;

namespace Oak.Wat.Ast;

/// <summary>
///     数据段
/// </summary>
/// <param name="MemoryIndex">内存索引。</param>
/// <param name="Offset">偏移表达式。</param>
/// <param name="Data">初始数据。</param>
/// <param name="Span">源码位置。</param>
public sealed record WatDataSegment(uint MemoryIndex, List<WatInstruction> Offset, byte[] Data, TextSpan Span = default)
    : WatAstNode(Span);