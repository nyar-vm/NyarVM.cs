using Std.Data.Text.Diagnostics;
using TextSpan = Std.Text.TextSpan;

namespace Nyar.Types;

/// <summary>
///     Nyar 编译器基础设施异常
/// </summary>
public class NyarError : Exception
{
    /// <summary>
    ///     创建 Nyar 错误
    /// </summary>
    /// <param name="message">错误消息。</param>
    public NyarError(string message) : base(message)
    {
        code = string.Empty;
        diagnostics = [];
    }

    /// <summary>
    ///     创建 Nyar 错误
    /// </summary>
    /// <param name="message">错误消息。</param>
    /// <param name="innerException">内部异常。</param>
    public NyarError(string message, Exception innerException)
        : base(message, innerException)
    {
        code = string.Empty;
        diagnostics = [];
    }

    /// <summary>
    ///     创建 Nyar 错误
    /// </summary>
    /// <param name="message">错误消息。</param>
    /// <param name="diagnostics">诊断信息列表。</param>
    /// <param name="code">错误代码。</param>
    public NyarError(string message, IReadOnlyList<Diagnostic> diagnostics, string code = "")
        : base(message)
    {
        this.code = code;
        this.diagnostics = diagnostics;
    }

    /// <summary>
    ///     错误代码
    /// </summary>
    public string code { get; }

    /// <summary>
    ///     关联的诊断信息列表
    /// </summary>
    public IReadOnlyList<Diagnostic> diagnostics { get; }

    /// <summary>
    ///     创建语法错误
    /// </summary>
    /// <param name="message">错误消息。</param>
    /// <param name="span">错误位置。</param>
    /// <returns>语法错误实例。</returns>
    public static NyarError syntax_error(string message, TextSpan span = default)
    {
        var diagnostic = new Diagnostic(span, message, DiagnosticSeverity.error);
        return new NyarError(message, [diagnostic], "SYNTAX");
    }

    /// <summary>
    ///     创建无效数据错误
    /// </summary>
    /// <param name="message">错误消息。</param>
    /// <param name="span">错误位置。</param>
    /// <returns>无效数据错误实例。</returns>
    public static NyarError invalid_data(string message, TextSpan span = default)
    {
        var diagnostic = new Diagnostic(span, message, DiagnosticSeverity.error);
        return new NyarError(message, [diagnostic], "INVALID_DATA");
    }

    /// <summary>
    ///     创建自定义错误
    /// </summary>
    /// <param name="message">错误消息。</param>
    /// <param name="code">错误代码。</param>
    /// <param name="span">错误位置。</param>
    /// <returns>自定义错误实例。</returns>
    public static NyarError custom_error(string message, string code, TextSpan span = default)
    {
        var diagnostic = new Diagnostic(span, message, DiagnosticSeverity.error);
        return new NyarError(message, [diagnostic], code);
    }
}