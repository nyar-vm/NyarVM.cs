using System.Collections.Immutable;

namespace Std.Data.Text.Syntax;

public readonly record struct SingleEdit(int start, int length, string new_text) : IEdit
{
    public ImmutableArray<TextEdit> changes =>
        [new(start, length, new_text)];
}