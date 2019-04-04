namespace Hermes.YYDB.Query;

public sealed class CteQuery : QueryExpression
{
    public CteQuery(string targetTypeName, IReadOnlyList<CteDefinition> cteDefinitions, QueryExpression mainQuery)
        : base(targetTypeName)
    {
        CteDefinitions = cteDefinitions;
        MainQuery = mainQuery;
    }

    public override string QueryKind => "cte";

    /// <summary>
    ///     CTE 定义列表
    /// </summary>
    public IReadOnlyList<CteDefinition> CteDefinitions { get; }

    /// <summary>
    ///     主查询
    /// </summary>
    public QueryExpression MainQuery { get; }
}

/// <summary>
///     CTE 定义
/// </summary>
public sealed class CteDefinition
{
    public CteDefinition(string name, QueryExpression subQuery, IReadOnlyList<string>? columnNames = null,
        bool isRecursive = false)
    {
        Name = name;
        SubQuery = subQuery;
        ColumnNames = columnNames ?? [];
        IsRecursive = isRecursive;
    }

    /// <summary>
    ///     CTE 名称
    /// </summary>
    public string Name { get; }

    /// <summary>
    ///     CTE 列名列表（可选）
    /// </summary>
    public IReadOnlyList<string> ColumnNames { get; }

    /// <summary>
    ///     CTE 子查询
    /// </summary>
    public QueryExpression SubQuery { get; }

    /// <summary>
    ///     是否为递归 CTE
    /// </summary>
    public bool IsRecursive { get; }
}