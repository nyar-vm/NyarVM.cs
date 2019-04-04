using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Sonic.Data.Generator.Meta;

/// <summary>
///     分析器诊断的创建与报告助手，封装 <see cref="DiagnosticDescriptor" /> 的构建与 <see cref="Diagnostic" /> 的生成。
/// </summary>
public static class DiagnosticHelper
{
    /// <summary>
    ///     创建一个诊断描述符。
    /// </summary>
    /// <param name="id">诊断标识符，如 <c>"SONIC0001"</c>。</param>
    /// <param name="title">诊断标题（简要描述）。</param>
    /// <param name="messageFormat">消息格式字符串。</param>
    /// <param name="category">诊断分类。</param>
    /// <param name="severity">诊断严重级别。</param>
    /// <param name="isEnabledByDefault">是否默认启用。</param>
    /// <param name="description">详细说明文本。</param>
    /// <param name="helpLinkUri">帮助链接。</param>
    /// <returns>构建的诊断描述符。</returns>
    public static DiagnosticDescriptor create_descriptor(
        string id,
        string title,
        string messageFormat,
        string category,
        DiagnosticSeverity severity,
        bool isEnabledByDefault = true,
        string? description = null,
        string? helpLinkUri = null)
    {
        return new DiagnosticDescriptor(
            id,
            title,
            messageFormat,
            category,
            severity,
            isEnabledByDefault,
            description,
            helpLinkUri);
    }

    /// <summary>
    ///     使用指定描述符创建一个诊断实例。
    /// </summary>
    /// <param name="descriptor">诊断描述符。</param>
    /// <param name="location">诊断发生的源代码位置。</param>
    /// <param name="messageArgs">格式化消息的参数。</param>
    /// <returns>创建的诊断实例。</returns>
    public static Diagnostic create_diagnostic(
        DiagnosticDescriptor descriptor,
        Location? location,
        params object?[] messageArgs)
    {
        return Diagnostic.Create(descriptor, location, messageArgs);
    }

    /// <summary>
    ///     报告一个诊断到分析器上下文。
    /// </summary>
    /// <param name="context">符号分析或操作块分析上下文。</param>
    /// <param name="descriptor">诊断描述符。</param>
    /// <param name="symbol">目标符号，其声明位置将被作为诊断位置。</param>
    /// <param name="messageArgs">格式化消息的参数。</param>
    public static void report_diagnostic(
        SymbolAnalysisContext context,
        DiagnosticDescriptor descriptor,
        ISymbol symbol,
        params object?[] messageArgs)
    {
        var diagnostic = Diagnostic.Create(descriptor, symbol.Locations[0], messageArgs);
        context.ReportDiagnostic(diagnostic);
    }

    /// <summary>
    ///     报告一个诊断到指定的上下文（通用方法）。
    /// </summary>
    /// <param name="context">语法节点分析上下文。</param>
    /// <param name="descriptor">诊断描述符。</param>
    /// <param name="node">目标语法节点，其位置将被作为诊断位置。</param>
    /// <param name="messageArgs">格式化消息的参数。</param>
    public static void report_diagnostic(
        SyntaxNodeAnalysisContext context,
        DiagnosticDescriptor descriptor,
        SyntaxNode node,
        params object?[] messageArgs)
    {
        var diagnostic = Diagnostic.Create(descriptor, node.GetLocation(), messageArgs);
        context.ReportDiagnostic(diagnostic);
    }
}