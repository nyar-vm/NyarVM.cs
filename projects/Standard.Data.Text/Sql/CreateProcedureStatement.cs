namespace Std.Data.Text.Sql;

/// <summary>
///     CREATE PROCEDURE 语句
///     。
/// </summary>
public sealed class CreateProcedureStatement : SqlNode
{
    public CreateProcedureStatement(
        string name,
        IReadOnlyList<ParameterDef> parameters,
        IReadOnlyList<SqlNode> body)
    {
        this.name = name;
        this.parameters = parameters;
        this.body = body;
    }


    /// <summary>
    ///     存储过程名
    ///     。
    /// </summary>
    public string name { get; }


    /// <summary>
    ///     参数列表
    ///     。
    /// </summary>
    public IReadOnlyList<ParameterDef> parameters { get; }


    /// <summary>
    ///     过程体（多条语句）
    ///     。
    /// </summary>
    public IReadOnlyList<SqlNode> body { get; }

    public override string ToString()
    {
        var paramStr = string.Join(", ", parameters.Select(p => $"{p.name} {p.type}"));
        var bodyStr = string.Join("; ", body.Select(b => b.ToString()));
        return $"CREATE PROCEDURE {name}({paramStr}) BEGIN {bodyStr}; END";
    }
}