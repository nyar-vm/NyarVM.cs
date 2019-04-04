using Std.Data.Text.Syntax;

namespace Std.Data.Text.Jasmin;

/// <summary>
///     JVM 指令
/// </summary>
/// <param name="opcode">操作码名称。</param>
/// <param name="operands">操作数列表。</param>
/// <param name="label">标签（如果指令前有标签）。</param>
/// <param name="span">源码位置。</param>
public sealed record JmInstruction(string opcode, List<string> operands, string? label = null, TextSpan span = default)
    : JmAstNode(span);