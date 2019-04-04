namespace Std.Data.Text.Diagnostics;

/// <summary>
///     诊断输出格式。
/// </summary>
public enum DiagnosticFormat : byte
{
    Pretty,
    Short,
    Detail,
    Custom
}

/// <summary>
///     诊断颜色模式。
/// </summary>
public enum DiagnosticColorMode : byte
{
    Auto,
    Always,
    Never
}

/// <summary>
///     诊断符号字符集。
/// </summary>
public enum DiagnosticSymbolSet : byte
{
    Unicode,
    Ascii
}

/// <summary>
///     诊断输出语言。
/// </summary>
public enum DiagnosticLanguage : byte
{
    En,
    ZhHans
}

/// <summary>
///     诊断渲染选项。
/// </summary>
public readonly record struct DiagnosticRenderOptions(
    DiagnosticFormat format,
    DiagnosticSeverity minimum_severity,
    DiagnosticColorMode color = DiagnosticColorMode.Auto,
    DiagnosticSymbolSet symbol = DiagnosticSymbolSet.Unicode,
    DiagnosticLanguage language = DiagnosticLanguage.En,
    int digits = 4,
    bool show_code = true,
    bool show_hints = true,
    string? custom_format = null,
    IDiagnosticRenderAdapter? custom_adapter = null);
