namespace Std.Data.Text.Protobuf;

public sealed class ProtoRpc : ProtoNode
{
    public ProtoRpc(string name, string inputType, bool inputStream, string outputType, bool outputStream)
    {
        this.name = name;
        input_type = inputType;
        input_stream = inputStream;
        output_type = outputType;
        output_stream = outputStream;
    }

    public string name { get; }
    public string input_type { get; }
    public bool input_stream { get; }
    public string output_type { get; }
    public bool output_stream { get; }

    public override string to_string()
    {
        var inStr = input_stream ? "stream " : "";
        var outStr = output_stream ? "stream " : "";
        return $"rpc {name}({inStr}{input_type}) returns ({outStr}{output_type});";
    }
}