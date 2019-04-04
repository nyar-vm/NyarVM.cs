namespace Std.Data.Text.GraphQL;

public sealed class GqlNonNullType : GqlTypeRef
{
    public GqlNonNullType(GqlTypeRef inner)
    {
        this.inner = inner;
    }

    public GqlTypeRef inner { get; }

    public override string to_string()
    {
        return $"{inner}!";
    }
}