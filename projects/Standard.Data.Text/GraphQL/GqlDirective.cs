namespace Std.Data.Text.GraphQL;

public sealed class GqlDirective : GqlNode
{
    public GqlDirective(string name, IReadOnlyList<(string Name, GqlValue Value)> arguments)
    {
        this.name = name;
        this.arguments = arguments;
    }

    public string name { get; }
    public IReadOnlyList<(string Name, GqlValue Value)> arguments { get; }

    public override string to_string()
    {
        var args = arguments.Count > 0 ? $"({string.Join(", ", arguments.Select(a => $"{a.Name}: {a.Value}"))})" : "";
        return $"@{name}{args}";
    }
}