namespace Std.Data.Text.GraphQL;

public sealed class GqlSchema : GqlNode
{
    public GqlSchema(IReadOnlyList<GqlTypeDefinition> typeDefinitions,
        IReadOnlyList<GqlDirectiveDefinition> directiveDefinitions)
    {
        type_definitions = typeDefinitions;
        directive_definitions = directiveDefinitions;
    }

    public IReadOnlyList<GqlTypeDefinition> type_definitions { get; }
    public IReadOnlyList<GqlDirectiveDefinition> directive_definitions { get; }

    public override string to_string()
    {
        return string.Join("\n", type_definitions.Cast<object>().Concat(directive_definitions));
    }
}