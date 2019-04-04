namespace Std.Data.Text.GraphQL;

public sealed class GqlInputValueDefinition : GqlNode
{
    public GqlInputValueDefinition(string name, GqlTypeRef type, GqlValue? defaultValue,
        IReadOnlyList<GqlDirective> directives)
    {
        this.name = name;
        this.type = type;
        default_value = defaultValue;
        this.directives = directives;
    }

    public string name { get; }
    public GqlTypeRef type { get; }
    public GqlValue? default_value { get; }
    public IReadOnlyList<GqlDirective> directives { get; }

    public override string to_string()
    {
        var def = default_value is not null ? $" = {default_value}" : "";
        return $"{name}: {type}{def}";
    }
}