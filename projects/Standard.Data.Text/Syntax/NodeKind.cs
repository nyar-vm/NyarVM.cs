namespace Std.Data.Text.Syntax;

public readonly struct NodeKind : IEquatable<NodeKind>
{
    public int value { get; }

    public NodeKind(int value)
    {
        this.value = value;
    }

    public bool Equals(NodeKind other)
    {
        return value == other.value;
    }

    public override bool Equals(object? obj)
    {
        return obj is NodeKind other && Equals(other);
    }

    public override int GetHashCode()
    {
        return value;
    }

    public static bool operator ==(NodeKind left, NodeKind right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(NodeKind left, NodeKind right)
    {
        return !left.Equals(right);
    }

    public static implicit operator NodeKind(int value)
    {
        return new NodeKind(value);
    }

    public static implicit operator int(NodeKind kind)
    {
        return kind.value;
    }

    public override string ToString()
    {
        return value.ToString();
    }
}