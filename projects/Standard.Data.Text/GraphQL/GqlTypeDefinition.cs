namespace Std.Data.Text.GraphQL;

public sealed class GqlTypeDefinition : GqlNode
{
    public GqlTypeDefinition(string name, string kind, IReadOnlyList<GqlFieldDefinition> fields,
        IReadOnlyList<string> implements, IReadOnlyList<GqlDirective> directives)
    {
        this.name = name;
        this.kind = kind;
        this.fields = fields;
        this.implements = implements;
        this.directives = directives;
    }

    public string name { get; }
    public string kind { get; }
    public IReadOnlyList<GqlFieldDefinition> fields { get; }
    public IReadOnlyList<string> implements { get; }
    public IReadOnlyList<GqlDirective> directives { get; }

    public override string to_string()
    {
        var impl = implements.Count > 0 ? $" implements {string.Join(" & ", implements)}" : "";
        var fields = string.Join("\n  ", this.fields);
        return $"{kind} {name}{impl} {{\n  {fields}\n}}";
    }
}