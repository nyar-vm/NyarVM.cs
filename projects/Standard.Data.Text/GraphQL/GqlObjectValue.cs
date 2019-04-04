namespace Std.Data.Text.GraphQL;

public sealed class GqlObjectValue : GqlValue
{
    public GqlObjectValue(IReadOnlyList<(string Name, GqlValue Value)> fields)
    {
        this.fields = fields;
    }

    public IReadOnlyList<(string Name, GqlValue Value)> fields { get; }

    public override string to_string()
    {
        return $"{{{string.Join(", ", fields.Select(f => $"{f.Name}: {f.Value}"))}}}";
    }
}