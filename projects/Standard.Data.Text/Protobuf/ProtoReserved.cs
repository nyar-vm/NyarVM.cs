namespace Std.Data.Text.Protobuf;

public sealed class ProtoReserved : ProtoNode
{
    public ProtoReserved(IReadOnlyList<string> names, IReadOnlyList<(int Start, int End)> ranges)
    {
        this.names = names;
        this.ranges = ranges;
    }

    public IReadOnlyList<string> names { get; }
    public IReadOnlyList<(int Start, int End)> ranges { get; }

    public override string to_string()
    {
        var parts = new List<string>();
        parts.AddRange(names);
        parts.AddRange(ranges.Select(r => r.Start == r.End ? r.Start.ToString() : $"{r.Start} to {r.End}"));
        return $"reserved {string.Join(", ", parts)};";
    }
}