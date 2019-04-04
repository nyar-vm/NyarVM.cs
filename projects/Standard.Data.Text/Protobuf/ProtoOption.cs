namespace Std.Data.Text.Protobuf;

public sealed class ProtoOption : ProtoNode
{
    public ProtoOption(string name, string value)
    {
        this.name = name;
        this.value = value;
    }

    public string name { get; }
    public string value { get; }

    public override string to_string()
    {
        return $"{name} = {value}";
    }
}