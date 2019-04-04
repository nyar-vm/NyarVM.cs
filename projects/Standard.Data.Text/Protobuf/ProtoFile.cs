namespace Std.Data.Text.Protobuf;

public sealed class ProtoFile : ProtoNode
{
    public ProtoFile(string? syntax, string? package, IReadOnlyList<string> imports, IReadOnlyList<ProtoOption> options,
        IReadOnlyList<ProtoMessage> messages, IReadOnlyList<ProtoEnum> enums, IReadOnlyList<ProtoService> services)
    {
        this.syntax = syntax;
        this.package = package;
        this.imports = imports;
        this.options = options;
        this.messages = messages;
        this.enums = enums;
        this.services = services;
    }

    public string? syntax { get; }
    public string? package { get; }
    public IReadOnlyList<string> imports { get; }
    public IReadOnlyList<ProtoOption> options { get; }
    public IReadOnlyList<ProtoMessage> messages { get; }
    public IReadOnlyList<ProtoEnum> enums { get; }
    public IReadOnlyList<ProtoService> services { get; }

    public override string to_string()
    {
        var parts = new List<string>();
        if (syntax is not null) parts.Add($"syntax = \"{syntax}\";");

        if (package is not null) parts.Add($"package {package};");

        foreach (var imp in imports) parts.Add($"import \"{imp}\";");

        parts.AddRange(messages.Select(m => m.to_string()));
        parts.AddRange(enums.Select(e => e.to_string()));
        parts.AddRange(services.Select(s => s.to_string()));
        return string.Join("\n", parts);
    }
}