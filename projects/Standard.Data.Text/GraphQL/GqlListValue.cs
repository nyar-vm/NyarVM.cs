namespace Std.Data.Text.GraphQL;

public sealed class GqlListValue : GqlValue
{
    public GqlListValue(IReadOnlyList<GqlValue> values)
    {
        this.values = values;
    }

    public IReadOnlyList<GqlValue> values { get; }

    public override string to_string()
    {
        return $"[{string.Join(", ", values)}]";
    }
}