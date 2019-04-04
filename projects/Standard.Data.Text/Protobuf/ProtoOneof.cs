namespace Std.Data.Text.Protobuf;

public sealed class ProtoOneof : ProtoNode
{
    public ProtoOneof(string name, IReadOnlyList<ProtoField> fields)
    {
        this.name = name;
        this.fields = fields;
    }

    public string name { get; }
    public IReadOnlyList<ProtoField> fields { get; }

    public override string to_string()
    {
        return $"oneof {name} {{ ... }}";
    }
}