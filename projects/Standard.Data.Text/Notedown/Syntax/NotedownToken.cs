namespace Std.Data.Text.Notedown.Syntax;

/// <summary>
///     Notedown 词法单元
/// </summary>
public readonly record struct NotedownToken
{
    /// <summary>
    ///     创建词法单元
    /// </summary>
    public NotedownToken(NotedownTokenKind kind, string text, int lineNumber, int indent = 0, object? data = null)
    {
        this.kind = kind;
        this.text = text;
        line_number = lineNumber;
        this.indent = indent;
        this.data = data;
    }

    /// <summary>
    ///     词法单元类型
    /// </summary>
    public NotedownTokenKind kind { get; init; }

    /// <summary>
    ///     原始行文本
    /// </summary>
    public string text { get; init; }

    /// <summary>
    ///     行号（从 1 开始）
    /// </summary>
    public int line_number { get; init; }

    /// <summary>
    ///     缩进级别
    /// </summary>
    public int indent { get; init; }

    /// <summary>
    ///     附加数据（如标题级别、围栏信息等）
    /// </summary>
    public object? data { get; init; }

    /// <summary>
    ///     文件结束标记
    /// </summary>
    public static NotedownToken end_of_file { get; } = new(NotedownTokenKind.end_of_file, string.Empty, -1);
}