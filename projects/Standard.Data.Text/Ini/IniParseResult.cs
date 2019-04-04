using Std.Data.Text.Diagnostics;

namespace Std.Data.Text.Ini;

public sealed class IniParseResult
{
    public Dictionary<string, string> global_entries { get; init; } = [];
    public IReadOnlyList<IniSection> sections { get; init; } = [];
    public IReadOnlyList<Diagnostic> diagnostics { get; init; } = [];
}