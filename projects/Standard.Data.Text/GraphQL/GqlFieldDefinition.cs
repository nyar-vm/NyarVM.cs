namespace Std.Data.Text.GraphQL;

public sealed class GqlFieldDefinition : GqlNode
{
    public GqlFieldDefinition(string name, IReadOnlyList<GqlInputValueDefinition> arguments, GqlTypeRef type,
        IReadOnlyList<GqlDirective> directives)
    {
        this.name = name;
        this.arguments = arguments;
        this.type = type;
        this.directives = directives;
    }

    public string name { get; }
    public IReadOnlyList<GqlInputValueDefinition> arguments { get; }
    public GqlTypeRef type { get; }
    public IReadOnlyList<GqlDirective> directives { get; }

    public override string to_string()
    {
        var args = arguments.Count > 0 ? $"({string.Join(", ", arguments)})" : "";
        return $"{name}{args}: {type}";
    }
}