namespace Std.Data.Text.Syntax;

public readonly record struct SourceSpan
{
    public string file_path { get; init; }
    public int start_line { get; init; }
    public int start_column { get; init; }
    public int end_line { get; init; }
    public int end_column { get; init; }


    public SourceSpan(int startLine, int startColumn, int endLine, int endColumn)
        : this(string.Empty, startLine, startColumn, endLine, endColumn)
    {
    }

    public SourceSpan(string file_path, int start_line, int start_column, int end_line, int end_column)
    {
        this.file_path = file_path;
        this.start_line = start_line;
        this.start_column = start_column;
        this.end_line = end_line;
        this.end_column = end_column;
    }


    public static SourceSpan single_line(int line, int column, int length, string? filePath = null)
    {
        var safeLength = System.Math.Max(length, 0);
        return new SourceSpan(filePath ?? string.Empty, line, column, line, column + safeLength);
    }
}
