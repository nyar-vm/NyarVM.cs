using Std.Data.Text.Notedown.Syntax;

namespace Std.Data.Text.Notedown.Parsing;

internal sealed class BracketParseResult
{
    public BracketParseResult(IReadOnlyList<NotedownInline> inlines, int newPosition)
    {
        this.inlines = inlines;
        new_position = newPosition;
    }

    public IReadOnlyList<NotedownInline> inlines { get; }
    public int new_position { get; }
}