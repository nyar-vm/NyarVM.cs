using Std.Data.Text.Notedown.Syntax;

namespace Std.Data.Text.Notedown.Parsing;

internal sealed class DelimiterParseResult
{
    public DelimiterParseResult(NotedownInline inline, int newPosition)
    {
        this.inline = inline;
        new_position = newPosition;
    }

    public NotedownInline inline { get; }
    public int new_position { get; }
}