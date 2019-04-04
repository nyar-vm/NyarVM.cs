using Std.Data.Text.Syntax;

namespace Oak.Wat.Ast;

/// <summary>
///     函数定义
/// </summary>
/// <param name="Span">源码位置。</param>
/// <param name="Name">函数名。</param>
/// <param name="Parameters">参数列表。</param>
/// <param name="Results">返回值类型列表。</param>
/// <param name="Locals">局部变量列表。</param>
/// <param name="Instructions">指令列表。</param>
/// <param name="ExportName">导出名。</param>
/// <param name="ImportModule">导入模块名。</param>
/// <param name="ImportName">导入字段名。</param>
public sealed record WatFunction(
    TextSpan Span,
    string? Name,
    List<WatParameter> Parameters,
    List<string> Results,
    List<WatLocal> Locals,
    List<WatInstruction> Instructions,
    string? ExportName,
    string? ImportModule,
    string? ImportName
) : WatAstNode(Span);