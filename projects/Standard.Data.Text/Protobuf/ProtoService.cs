namespace Std.Data.Text.Protobuf;

public sealed class ProtoService : ProtoNode
{
    public ProtoService(string name, IReadOnlyList<ProtoRpc> methods)
    {
        this.name = name;
        this.methods = methods;
    }

    public string name { get; }
    public IReadOnlyList<ProtoRpc> methods { get; }

    public override string to_string()
    {
        return $"service {name} {{ ... }}";
    }
}