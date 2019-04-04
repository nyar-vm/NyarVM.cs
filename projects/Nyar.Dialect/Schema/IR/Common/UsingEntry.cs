namespace Nyar.Dialect.Schema.IR.Common;

public sealed class UsingEntry
{
    public UsingEntry(string @namespace, IReadOnlyList<string>? selections = null, int sourceLine = 0,
        int sourceColumn = 0)
    {
        this.@namespace = @namespace;
        this.selections = selections ?? [];
        source_line = sourceLine;
        source_column = sourceColumn;
    }

    public string @namespace { get; }
    public IReadOnlyList<string> selections { get; }
    public int source_line { get; }
    public int source_column { get; }

    public bool is_wildcard => selections.Contains("*");
}