namespace Std.Data.Text.Sql;

/// <summary>
///     CALL 语句
///     。
/// </summary>
public sealed class CallStatement : SqlNode
{
    public CallStatement(string procedureName, IReadOnlyList<SqlExpression> arguments)
    {
        procedure_name = procedureName;
        this.arguments = arguments;
    }


    /// <summary>
    ///     存储过程名
    ///     。
    /// </summary>
    public string procedure_name { get; }


    /// <summary>
    ///     实参列表
    ///     。
    /// </summary>
    public IReadOnlyList<SqlExpression> arguments { get; }

    public override string ToString()
    {
        var argStr = string.Join(", ", arguments.Select(a => a.ToString()));
        return $"CALL {procedure_name}({argStr})";
    }
}