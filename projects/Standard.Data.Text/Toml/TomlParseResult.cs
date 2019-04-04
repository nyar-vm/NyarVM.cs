using Std.Data.Text.Diagnostics;
using Std.Data.Text.Toml.Ast;

namespace Std.Data.Text.Toml;

public sealed class TomlParseResult
{
    public TomlTable root { get; init; } = new();

    public IReadOnlyList<Diagnostic> diagnostics { get; init; } = [];
}