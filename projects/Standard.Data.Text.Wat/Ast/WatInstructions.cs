using Std.Data.Text.Syntax;

namespace Oak.Wat.Ast;

/// <summary>
///     常量指令（i32.const, i64.const, f32.const, f64.const）
/// </summary>
/// <param name="Span">源码位置。</param>
/// <param name="Opcode">操作码助记符。</param>
/// <param name="ConstType">常量类型。</param>
/// <param name="Value">常量值。</param>
public sealed record WatConstInstruction(TextSpan Span, string Opcode, string ConstType, string Value)
    : WatInstruction(Span, Opcode);

/// <summary>
///     变量指令（local.get, local.set, local.tee, global.get, global.set）
/// </summary>
/// <param name="Span">源码位置。</param>
/// <param name="Opcode">操作码助记符。</param>
/// <param name="Variable">变量标识。</param>
public sealed record WatVariableInstruction(TextSpan Span, string Opcode, string Variable)
    : WatInstruction(Span, Opcode);

/// <summary>
///     调用指令（call, call_indirect）
/// </summary>
/// <param name="Span">源码位置。</param>
/// <param name="Opcode">操作码助记符。</param>
/// <param name="Function">函数名。</param>
/// <param name="TypeRef">类型引用。</param>
/// <param name="Params">参数列表。</param>
/// <param name="Results">返回值类型列表。</param>
public sealed record WatCallInstruction(
    TextSpan Span,
    string Opcode,
    string? Function,
    string? TypeRef,
    List<WatParameter>? Params,
    List<string>? Results) : WatInstruction(Span, Opcode);

/// <summary>
///     控制流指令（block, loop, if, br, br_if, br_table）
/// </summary>
/// <param name="Span">源码位置。</param>
/// <param name="Opcode">操作码助记符。</param>
/// <param name="Label">标签。</param>
/// <param name="Results">返回值类型列表。</param>
/// <param name="Body">指令体。</param>
/// <param name="ElseBody">else 分支指令体。</param>
/// <param name="Targets">跳转目标列表。</param>
public sealed record WatControlInstruction(
    TextSpan Span,
    string Opcode,
    string? Label,
    List<string>? Results,
    List<WatInstruction> Body,
    List<WatInstruction>? ElseBody,
    List<string>? Targets) : WatInstruction(Span, Opcode);

/// <summary>
///     内存指令（各种 load/store）
/// </summary>
/// <param name="Span">源码位置。</param>
/// <param name="Opcode">操作码助记符。</param>
/// <param name="Align">对齐。</param>
/// <param name="Offset">偏移。</param>
public sealed record WatMemoryInstruction(TextSpan Span, string Opcode, string? Align, string? Offset)
    : WatInstruction(Span, Opcode);

/// <summary>
///     简单指令（drop, select, return, nop, unreachable）
/// </summary>
/// <param name="Span">源码位置。</param>
/// <param name="Opcode">操作码助记符。</param>
public sealed record WatSimpleInstruction(TextSpan Span, string Opcode) : WatInstruction(Span, Opcode);

/// <summary>
///     二元运算指令（i32.add 等）
/// </summary>
/// <param name="Span">源码位置。</param>
/// <param name="Opcode">操作码助记符。</param>
public sealed record WatBinaryInstruction(TextSpan Span, string Opcode) : WatInstruction(Span, Opcode);

/// <summary>
///     比较指令（i32.eq 等）
/// </summary>
/// <param name="Span">源码位置。</param>
/// <param name="Opcode">操作码助记符。</param>
public sealed record WatCompareInstruction(TextSpan Span, string Opcode) : WatInstruction(Span, Opcode);

/// <summary>
///     一元运算指令（i32.clz 等）
/// </summary>
/// <param name="Span">源码位置。</param>
/// <param name="Opcode">操作码助记符。</param>
public sealed record WatUnaryInstruction(TextSpan Span, string Opcode) : WatInstruction(Span, Opcode);