namespace Std.Data.Text.Syntax;

public readonly struct TextSpan : IEquatable<TextSpan>
{
    public int start { get; }

    public int length { get; }

    public int end => start + length;

    public TextSpan(int start, int length)
    {
        this.start = start;
        this.length = length;
    }

    public bool Equals(TextSpan other)
    {
        return start == other.start && length == other.length;
    }

    public override bool Equals(object? obj)
    {
        return obj is TextSpan other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(start, length);
    }

    public static bool operator ==(TextSpan left, TextSpan right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(TextSpan left, TextSpan right)
    {
        return !left.Equals(right);
    }

    public bool contains(int position)
    {
        return position >= start && position < end;
    }

    public bool overlaps_with(TextSpan other)
    {
        return other.start < end && start < other.end;
    }

    public override string ToString()
    {
        return $"[{start}..{end})";
    }
}