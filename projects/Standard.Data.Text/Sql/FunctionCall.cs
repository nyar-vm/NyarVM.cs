namespace Std.Data.Text.Sql;

public sealed class FunctionCall : SqlExpression
{
    public FunctionCall(string name, IReadOnlyList<SqlExpression> arguments, bool distinct = false)
    {
        this.name = name;
        this.arguments = arguments;
        this.distinct = distinct;
    }

    public string name { get; }
    public IReadOnlyList<SqlExpression> arguments { get; }
    public bool distinct { get; }

    public override string ToString()
    {
        var args = distinct ? $"DISTINCT {string.Join(", ", arguments)}" : string.Join(", ", arguments);
        return $"{name}({args})";
    }
}