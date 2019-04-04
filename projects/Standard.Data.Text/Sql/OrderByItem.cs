namespace Std.Data.Text.Sql;

public sealed class OrderByItem : SqlNode
{
    public OrderByItem(SqlExpression expression, bool descending = false, bool nullsFirst = false,
        bool nullsLast = false)
    {
        this.expression = expression;
        this.descending = descending;
        nulls_first = nullsFirst;
        nulls_last = nullsLast;
    }

    public SqlExpression expression { get; }
    public bool descending { get; }
    public bool nulls_first { get; }
    public bool nulls_last { get; }

    public override string ToString()
    {
        var result = descending ? $"{expression} DESC" : $"{expression} ASC";
        if (nulls_first) result += " NULLS FIRST";

        if (nulls_last) result += " NULLS LAST";

        return result;
    }
}