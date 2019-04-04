namespace Std.Data.Text.GraphQL;

public sealed class GqlNullValue : GqlValue
{
    public static GqlNullValue instance { get; } = new();

    public override string to_string()
    {
        return "null";
    }
}