namespace Std.Data.Text.Sql;

/// <summary>
///     复合查询：SELECT ... UNION/INTERSECT/EXCEPT SELECT ...
/// </summary>
public sealed class CompoundSelectStatement : SqlNode
{
    public CompoundSelectStatement(CompoundOperator op, SelectStatement left, SelectStatement right)
    {
        @operator = op;
        this.left = left;
        this.right = right;
    }

    public CompoundOperator @operator { get; }
    public SelectStatement left { get; }
    public SelectStatement right { get; }

    public override string ToString()
    {
        var op = @operator switch
        {
            CompoundOperator.union => "UNION",
            CompoundOperator.union_all => "UNION ALL",
            CompoundOperator.intersect => "INTERSECT",
            CompoundOperator.except => "EXCEPT",
            _ => "?"
        };
        return $"{left} {op} {right}";
    }
}