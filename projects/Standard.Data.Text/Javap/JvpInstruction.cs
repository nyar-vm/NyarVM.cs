using Std.Data.Text.Syntax;

namespace Std.Data.Text.Javap;

/// <summary>
///     JVM 指令（javap 格式）
/// </summary>
/// <param name="offset">字节码偏移量。</param>
/// <param name="opcode">操作码名称（小写下划线格式）。</param>
/// <param name="operand">操作数文本。</param>
/// <param name="comment">常量池注释。</param>
/// <param name="span">源码位置。</param>
public sealed record JvpInstruction(
    int offset,
    string opcode,
    string? operand = null,
    string? comment = null,
    TextSpan span = default) : JvpAstNode(span);