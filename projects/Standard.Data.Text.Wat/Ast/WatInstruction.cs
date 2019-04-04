using Std.Data.Text.Syntax;

namespace Oak.Wat.Ast;

/// <summary>
///     WAT 指令基类
/// </summary>
/// <param name="Span">源码位置。</param>
/// <param name="Opcode">操作码助记符。</param>
public abstract record WatInstruction(TextSpan Span, string Opcode) : WatAstNode(Span);

/// <summary>
///     通用指令（回退处理，无法归类到具体子类的指令）
/// </summary>
/// <param name="Span">源码位置。</param>
/// <param name="Opcode">操作码助记符。</param>
/// <param name="Operands">操作数列表。</param>
public sealed record WatGenericInstruction(TextSpan Span, string Opcode, List<string> Operands)
    : WatInstruction(Span, Opcode);