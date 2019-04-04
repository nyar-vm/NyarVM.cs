using Std.Data.Text.Syntax;

namespace Oak.Wat.Ast;

/// <summary>
///     函数导入描述
/// </summary>
/// <param name="Span">源码位置。</param>
/// <param name="Id">标识符。</param>
/// <param name="TypeRef">类型引用。</param>
/// <param name="Params">参数列表。</param>
/// <param name="Results">返回值类型列表。</param>
public sealed record WatFuncImportDescriptor(
    TextSpan Span,
    string? Id,
    string? TypeRef,
    List<WatParameter> Params,
    List<string> Results) : WatAstNode(Span);

/// <summary>
///     内存导入描述
/// </summary>
/// <param name="Span">源码位置。</param>
/// <param name="Id">标识符。</param>
/// <param name="MinPages">最小页数。</param>
/// <param name="MaxPages">最大页数。</param>
public sealed record WatMemoryImportDescriptor(TextSpan Span, string? Id, int MinPages, int? MaxPages)
    : WatAstNode(Span);

/// <summary>
///     表导入描述
/// </summary>
/// <param name="Span">源码位置。</param>
/// <param name="Id">标识符。</param>
/// <param name="ElementType">元素类型。</param>
/// <param name="MinSize">最小大小。</param>
/// <param name="MaxSize">最大大小。</param>
public sealed record WatTableImportDescriptor(TextSpan Span, string? Id, string ElementType, int MinSize, int? MaxSize)
    : WatAstNode(Span);

/// <summary>
///     全局变量导入描述
/// </summary>
/// <param name="Span">源码位置。</param>
/// <param name="Id">标识符。</param>
/// <param name="IsMutable">是否可变。</param>
/// <param name="ValueType">值类型。</param>
public sealed record WatGlobalImportDescriptor(TextSpan Span, string? Id, bool IsMutable, string ValueType)
    : WatAstNode(Span);