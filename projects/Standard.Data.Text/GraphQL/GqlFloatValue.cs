namespace Std.Data.Text.GraphQL;

public sealed class GqlFloatValue : GqlValue
{
    public GqlFloatValue(string value)
    {
        this.value = value;
    }

    public string value { get; }

    public override string to_string()
    {
        return value;
    }
}