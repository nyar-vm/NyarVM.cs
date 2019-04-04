using Std.Data.Text.Syntax;

namespace Std.Data.Text.Msil;

/// <summary>
///     MSIL 指令
/// </summary>
/// <param name="offset">字节码偏移量。</param>
/// <param name="opcode">操作码名称（点分格式：ldarg.0, call, add 等）</param>
/// <param name="operand">操作数文本。</param>
/// <param name="span">源码位置。</param>
public sealed record MsilInstruction(int offset, string opcode, string? operand = null, TextSpan span = default)
    : MsilAstNode(span);