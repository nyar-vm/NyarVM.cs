namespace Std.Data.Text.GraphQL;

public sealed class GqlBooleanValue : GqlValue
{
    public GqlBooleanValue(bool value)
    {
        this.value = value;
    }

    public bool value { get; }

    public override string to_string()
    {
        return value ? "true" : "false";
    }
}