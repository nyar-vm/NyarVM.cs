namespace Std.Data.Text.GraphQL;

public sealed class GqlDirectiveDefinition : GqlNode
{
    public GqlDirectiveDefinition(string name, IReadOnlyList<GqlInputValueDefinition> arguments,
        IReadOnlyList<GqlDirectiveLocation> locations, bool repeatable)
    {
        this.name = name;
        this.arguments = arguments;
        this.locations = locations;
        this.repeatable = repeatable;
    }

    public string name { get; }
    public IReadOnlyList<GqlInputValueDefinition> arguments { get; }
    public IReadOnlyList<GqlDirectiveLocation> locations { get; }
    public bool repeatable { get; }

    public override string to_string()
    {
        var args = arguments.Count > 0 ? $"({string.Join(", ", arguments)})" : "";
        var rep = repeatable ? " repeatable" : "";
        var locs = string.Join(" | ", locations);
        return $"directive @{name}{args}{rep} on {locs}";
    }
}