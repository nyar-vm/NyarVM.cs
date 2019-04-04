namespace Std.Data.Text.GraphQL;

public sealed class GqlIntValue : GqlValue
{
    public GqlIntValue(string value)
    {
        this.value = value;
    }

    public string value { get; }

    public override string to_string()
    {
        return value;
    }
}