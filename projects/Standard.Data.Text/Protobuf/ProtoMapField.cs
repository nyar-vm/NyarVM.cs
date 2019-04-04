namespace Std.Data.Text.Protobuf;

public sealed class ProtoMapField : ProtoNode
{
    public ProtoMapField(string keyType, string valueType, string name, int number)
    {
        key_type = keyType;
        value_type = valueType;
        this.name = name;
        this.number = number;
    }

    public string key_type { get; }
    public string value_type { get; }
    public string name { get; }
    public int number { get; }

    public override string to_string()
    {
        return $"map<{key_type}, {value_type}> {name} = {number};";
    }
}