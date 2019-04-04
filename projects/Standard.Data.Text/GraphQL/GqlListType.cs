namespace Std.Data.Text.GraphQL;

public sealed class GqlListType : GqlTypeRef
{
    public GqlListType(GqlTypeRef elementType)
    {
        element_type = elementType;
    }

    public GqlTypeRef element_type { get; }

    public override string to_string()
    {
        return $"[{element_type}]";
    }
}