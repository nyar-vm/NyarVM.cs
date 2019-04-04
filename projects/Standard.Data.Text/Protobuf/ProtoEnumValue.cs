namespace Std.Data.Text.Protobuf;

public sealed class ProtoEnumValue : ProtoNode
{
    public ProtoEnumValue(string name, int number)
    {
        this.name = name;
        this.number = number;
    }

    public string name { get; }
    public int number { get; }

    public override string to_string()
    {
        return $"{name} = {number};";
    }
}