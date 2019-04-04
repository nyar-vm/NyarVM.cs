namespace Std.Data.Text.Protobuf;

public sealed class ProtoField : ProtoNode
{
    public ProtoField(string label, string type, string name, int number, IReadOnlyList<ProtoOption> options)
    {
        this.label = label;
        this.type = type;
        this.name = name;
        this.number = number;
        this.options = options;
    }

    public string label { get; }
    public string type { get; }
    public string name { get; }
    public int number { get; }
    public IReadOnlyList<ProtoOption> options { get; }

    public override string to_string()
    {
        var opts = options.Count > 0 ? $" [{string.Join(", ", options)}]" : "";
        return $"{label} {type} {name} = {number}{opts};";
    }
}