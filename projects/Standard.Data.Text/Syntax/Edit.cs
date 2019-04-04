namespace Std.Data.Text.Syntax;

public readonly struct Edit
{
    public TextSpan old_span { get; }

    public string new_text { get; }

    public Edit(TextSpan oldSpan, string newText)
    {
        old_span = oldSpan;
        new_text = newText;
    }

    public int delta => new_text.Length - old_span.length;

    public override string ToString()
    {
        return $"Replace {old_span} with \"{new_text}\"";
    }
}