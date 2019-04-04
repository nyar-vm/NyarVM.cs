using Std.Data.Text.Syntax;

namespace Oak.Wat.Ast;

/// <summary>
///     WAT 模块
/// </summary>
/// <param name="Span">源码位置。</param>
/// <param name="Name">模块名。</param>
/// <param name="Imports">导入列表。</param>
/// <param name="Exports">导出列表。</param>
/// <param name="Functions">函数列表。</param>
/// <param name="Memories">内存列表。</param>
/// <param name="Tables">表列表。</param>
/// <param name="Globals">全局变量列表。</param>
/// <param name="DataSegments">数据段列表。</param>
/// <param name="Types">类型定义列表。</param>
/// <param name="ElemSegments">元素段列表。</param>
/// <param name="Start">start 函数声明。</param>
public sealed record WatModule(
    TextSpan Span,
    string? Name,
    List<WatImport> Imports,
    List<WatExport> Exports,
    List<WatFunction> Functions,
    List<WatMemory> Memories,
    List<WatTable> Tables,
    List<WatGlobal> Globals,
    List<WatDataSegment> DataSegments,
    List<WatTypeDefinition> Types,
    List<WatElemSegment> ElemSegments,
    WatStart? Start
) : WatAstNode(Span);