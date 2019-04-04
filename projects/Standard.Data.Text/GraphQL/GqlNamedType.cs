namespace Std.Data.Text.GraphQL;

public sealed class GqlNamedType : GqlTypeRef
{
    public GqlNamedType(string name)
    {
        this.name = name;
    }

    public string name { get; }

    public override string to_string()
    {
        return name;
    }
}