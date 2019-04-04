using Std.Data.Text.Diagnostics;

namespace Std.Data.Text.Parsing;

/// <summary>
///     解析结果
/// </summary>
/// <typeparam name="T">值类型</typeparam>
public sealed class ParseResult<T>
{
    private ParseResult(bool success, T? value, IReadOnlyList<Diagnostic> diagnostics)
    {
        this.success = success;
        this.value = value;
        this.diagnostics = diagnostics;
    }

    /// <summary>
    ///     是否成功
    /// </summary>
    public bool success { get; }

    /// <summary>
    ///     解析得到的值
    /// </summary>
    public T? value { get; }

    /// <summary>
    ///     诊断信息
    /// </summary>
    public IReadOnlyList<Diagnostic> diagnostics { get; }

    /// <summary>
    ///     创建成功的解析结果
    /// </summary>
    public static ParseResult<T> ok(T value, IReadOnlyList<Diagnostic>? diagnostics = null)
    {
        return new ParseResult<T>(true, value, diagnostics ?? []);
    }

    /// <summary>
    ///     创建失败的解析结果
    /// </summary>
    public static ParseResult<T> fail(IReadOnlyList<Diagnostic> diagnostics)
    {
        return new ParseResult<T>(false, default, diagnostics);
    }
}