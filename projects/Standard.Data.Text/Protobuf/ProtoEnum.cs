namespace Std.Data.Text.Protobuf;

public sealed class ProtoEnum : ProtoNode
{
    public ProtoEnum(string name, IReadOnlyList<ProtoEnumValue> values)
    {
        this.name = name;
        this.values = values;
    }

    public string name { get; }
    public IReadOnlyList<ProtoEnumValue> values { get; }

    public override string to_string()
    {
        return $"enum {name} {{ ... }}";
    }
}