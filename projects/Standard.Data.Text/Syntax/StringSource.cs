namespace Std.Data.Text.Syntax;

public sealed class StringSource : ISource
{
    public static readonly StringSource empty = new(string.Empty);

    private readonly string _text;

    public StringSource(string text)
    {
        _text = text;
    }

    public char this[int index] => _text[index];

    public int length => _text.Length;

    public string substring(Range range)
    {
        return _text[range];
    }

    public override string ToString()
    {
        return _text;
    }
}