namespace Std.Data.Text.Valkyrie.Query;

public sealed class AggregateOperationAst
{
    public AggregateOperationAst(AggregateFunctionAst function, string fieldName, string? alias = null)
    {
        this.function = function;
        field_name = fieldName;
        this.alias = alias;
    }

    public AggregateFunctionAst function { get; }
    public string field_name { get; }
    public string? alias { get; }
}