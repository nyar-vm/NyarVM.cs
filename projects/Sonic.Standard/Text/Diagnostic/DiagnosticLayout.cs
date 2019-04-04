namespace Std.Data.Text.Diagnostics;

/// <summary>
///     诊断布局模型。
/// </summary>
internal readonly record struct DiagnosticLayout(
    IReadOnlyList<DiagnosticLayoutBlock> blocks);

/// <summary>
///     诊断布局块。
/// </summary>
internal readonly record struct DiagnosticLayoutBlock(
    DiagnosticLayoutBlockKind kind,
    IReadOnlyList<DiagnosticLayoutLine> lines);

/// <summary>
///     诊断布局行。
/// </summary>
internal readonly record struct DiagnosticLayoutLine(
    string text,
    ConsoleColor? color = null);

/// <summary>
///     诊断布局块类型。
/// </summary>
internal enum DiagnosticLayoutBlockKind : byte
{
    Heading,
    Location,
    Snippet,
    Metadata,
    Spacer
}
