namespace Std.Data.Text.GraphQL;

public sealed class GqlStringValue : GqlValue
{
    public GqlStringValue(string value)
    {
        this.value = value;
    }

    public string value { get; }

    public override string to_string()
    {
        return $"\"{value}\"";
    }
}