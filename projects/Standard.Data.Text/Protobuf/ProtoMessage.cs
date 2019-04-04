namespace Std.Data.Text.Protobuf;

public sealed class ProtoMessage : ProtoNode
{
    public ProtoMessage(string name, IReadOnlyList<ProtoField> fields, IReadOnlyList<ProtoMessage> nestedMessages,
        IReadOnlyList<ProtoEnum> nestedEnums, IReadOnlyList<ProtoOneof> oneofs, IReadOnlyList<ProtoMapField> mapFields,
        IReadOnlyList<ProtoReserved> reserved, IReadOnlyList<ProtoOption> options)
    {
        this.name = name;
        this.fields = fields;
        nested_messages = nestedMessages;
        nested_enums = nestedEnums;
        this.oneofs = oneofs;
        map_fields = mapFields;
        this.reserved = reserved;
        this.options = options;
    }

    public string name { get; }
    public IReadOnlyList<ProtoField> fields { get; }
    public IReadOnlyList<ProtoMessage> nested_messages { get; }
    public IReadOnlyList<ProtoEnum> nested_enums { get; }
    public IReadOnlyList<ProtoOneof> oneofs { get; }
    public IReadOnlyList<ProtoMapField> map_fields { get; }
    public IReadOnlyList<ProtoReserved> reserved { get; }
    public IReadOnlyList<ProtoOption> options { get; }

    public override string to_string()
    {
        return $"message {name} {{ ... }}";
    }
}