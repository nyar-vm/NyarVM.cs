namespace Std.Data.Text.Sql;

/// <summary>
///     CREATE FUNCTION 语句
///     。
/// </summary>
public sealed class CreateFunctionStatement : SqlNode
{
    public CreateFunctionStatement(
        string name,
        IReadOnlyList<ParameterDef> parameters,
        string returnType,
        SqlExpression body)
    {
        this.name = name;
        this.parameters = parameters;
        return_type = returnType;
        this.body = body;
    }


    /// <summary>
    ///     函数名
    ///     。
    /// </summary>
    public string name { get; }


    /// <summary>
    ///     参数列表
    ///     。
    /// </summary>
    public IReadOnlyList<ParameterDef> parameters { get; }


    /// <summary>
    ///     返回值类型
    ///     。
    /// </summary>
    public string return_type { get; }


    /// <summary>
    ///     函数体（表达式）
    ///     。
    /// </summary>
    public SqlExpression body { get; }

    public override string ToString()
    {
        var paramStr = string.Join(", ", parameters.Select(p => $"{p.name} {p.type}"));
        return $"CREATE FUNCTION {name}({paramStr}) RETURNS {return_type} AS {body}";
    }
}