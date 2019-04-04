using System.Text;
using Nyar.Dialect.Query;
using Std.Data.Text.Sql;

namespace Olympus.Athena.Query;

#region SqlIntentBuilder SQL 意图构建器

/// <summary>
///     将 BoundQuery 转换为 OA 代数调用链，INode 作为简单载体记录每一步 Algebra 调用
/// </summary>
public sealed class SqlIntentBuilder : IQueryAlgebra<INode>
{
    /// <inheritdoc />
    public INode table_scan(string table, string? alias)
    {
        var args = new List<object> { table };
        if (alias != null) args.Add(alias);

        return new INode(nameof(table_scan), args, []);
    }

    /// <inheritdoc />
    public INode index_scan(string table, string indexName, INode predicate)
    {
        return new INode(nameof(index_scan), [table, indexName], [predicate]);
    }

    /// <inheritdoc />
    public INode partition_scan(string table, IReadOnlyList<Guid> partitions)
    {
        return new INode(nameof(partition_scan), [table, partitions], []);
    }

    /// <inheritdoc />
    public INode empty_source()
    {
        return new INode(nameof(empty_source), [], []);
    }

    /// <inheritdoc />
    public INode project(INode child, IReadOnlyList<string> columns)
    {
        return new INode(nameof(project), [columns], [child]);
    }

    /// <inheritdoc />
    public INode project_expr(INode child, IReadOnlyList<(SqlExpression Expr, string? Alias)> columns)
    {
        return new INode(nameof(project_expr), [columns], [child]);
    }

    /// <inheritdoc />
    public INode star_project(INode child)
    {
        return new INode(nameof(star_project), [], [child]);
    }

    /// <inheritdoc />
    public INode filter(INode child, SqlExpression predicate)
    {
        return new INode(nameof(filter), [predicate], [child]);
    }

    /// <inheritdoc />
    public INode where(INode child, SqlExpression predicate)
    {
        return filter(child, predicate);
    }

    /// <inheritdoc />
    public INode hash_join(INode left, INode right, SqlExpression condition, JoinKind kind)
    {
        return new INode(nameof(hash_join), [condition, kind], [left, right]);
    }

    /// <inheritdoc />
    public INode merge_join(INode left, INode right, SqlExpression condition, JoinKind kind)
    {
        return new INode(nameof(merge_join), [condition, kind], [left, right]);
    }

    /// <inheritdoc />
    public INode nested_loop_join(INode left, INode right, SqlExpression condition, JoinKind kind)
    {
        return new INode(nameof(nested_loop_join), [condition, kind], [left, right]);
    }

    /// <inheritdoc />
    public INode cross_join(INode left, INode right)
    {
        return new INode(nameof(cross_join), [], [left, right]);
    }

    /// <inheritdoc />
    public INode hash_aggregate(INode child, IReadOnlyList<SqlExpression> keys,
        IReadOnlyList<(string Func, SqlExpression Arg, string? Alias)> aggregates)
    {
        return new INode(nameof(hash_aggregate), [keys, aggregates], [child]);
    }

    /// <inheritdoc />
    public INode pre_aggregate(INode child, IReadOnlyList<SqlExpression> partialKeys,
        IReadOnlyList<(string Func, SqlExpression Arg)> partialAggs)
    {
        return new INode(nameof(pre_aggregate), [partialKeys, partialAggs], [child]);
    }

    /// <inheritdoc />
    public INode sort(INode child, IReadOnlyList<OrderByItem> orderBy)
    {
        return new INode(nameof(sort), [orderBy], [child]);
    }

    /// <inheritdoc />
    public INode top_k(INode child, int k, IReadOnlyList<OrderByItem> orderBy)
    {
        return new INode(nameof(top_k), [k, orderBy], [child]);
    }

    /// <inheritdoc />
    public INode limit(INode child, int count, int offset)
    {
        return new INode(nameof(limit), [count, offset], [child]);
    }

    /// <summary>
    ///     将 BoundQuery 转换为 OA 代数调用链
    /// </summary>
    /// <param name="bound">绑定后的查询</param>
    /// <returns>意图节点</returns>
    public INode Build(BoundQuery bound)
    {
        var original = bound.Original;
        INode node;

        if (original.From != null)
            node = table_scan(original.From.name, original.From.alias);
        else
            node = empty_source();

        foreach (var join in original.Joins)
        {
            var right = table_scan(join.name, join.alias);
            var joinKind = MapJoinKind(join.join_type);
            if (join.on_condition != null)
                node = hash_join(node, right, join.on_condition, joinKind);
            else
                node = cross_join(node, right);
        }

        if (original.Where != null) node = filter(node, original.Where);

        if (original.GroupBy.Count > 0)
        {
            var aggregates = ExtractAggregates(original.Columns);
            node = hash_aggregate(node, original.GroupBy, aggregates);
        }

        var projCols = ExtractProjectColumns(original.Columns);
        node = project_expr(node, projCols);

        if (original.OrderBy.Count > 0 && original.Limit.HasValue)
            node = top_k(node, original.Limit.Value, original.OrderBy);
        else if (original.OrderBy.Count > 0)
            node = sort(node, original.OrderBy);
        else if (original.Limit.HasValue) node = limit(node, original.Limit.Value, original.Offset ?? 0);

        return node;
    }

    private static JoinKind MapJoinKind(string? joinType)
    {
        if (joinType == null) return JoinKind.inner;

        return joinType.ToUpperInvariant() switch
        {
            "LEFT" => JoinKind.left,
            "RIGHT" => JoinKind.right,
            "FULL" => JoinKind.full,
            "CROSS" => JoinKind.cross,
            _ => JoinKind.inner
        };
    }

    private static IReadOnlyList<(SqlExpression Expr, string? Alias)> ExtractProjectColumns(
        IReadOnlyList<SqlColumn> columns)
    {
        return columns.Select(c => (Expression: c.expression, (string?)c.alias)).ToList().AsReadOnly();
    }

    private static IReadOnlyList<(string Func, SqlExpression Arg, string? Alias)> ExtractAggregates(
        IReadOnlyList<SqlColumn> columns)
    {
        var result = new List<(string Func, SqlExpression Arg, string? Alias)>();
        foreach (var col in columns)
            if (col.expression is FunctionCall funcCall)
            {
                var arg = funcCall.arguments.Count > 0 ? funcCall.arguments[0] : new LiteralValue(string.Empty);
                result.Add((funcCall.name, arg, col.alias));
            }

        return result.AsReadOnly();
    }
}

#endregion

#region INode 意图节点

/// <summary>
///     OA 代数调用节点，记录每一步 Algebra 调用信息
/// </summary>
public sealed class INode
{
    /// <summary>
    ///     创建代数调用节点
    /// </summary>
    /// <param name="method">方法名</param>
    /// <param name="args">参数列表</param>
    /// <param name="children">子节点列表</param>
    public INode(string method, IReadOnlyList<object> args, IReadOnlyList<INode> children)
    {
        Method = method;
        Args = args;
        Children = children;
    }

    /// <summary>
    ///     调用的代数方法名
    /// </summary>
    public string Method { get; }

    /// <summary>
    ///     调用参数列表
    /// </summary>
    public IReadOnlyList<object> Args { get; }

    /// <summary>
    ///     子节点列表
    /// </summary>
    public IReadOnlyList<INode> Children { get; }

    /// <inheritdoc />
    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.Append(Method);
        sb.Append('(');

        for (var i = 0; i < Args.Count; i++)
        {
            if (i > 0) sb.Append(", ");

            sb.Append(Args[i]);
        }

        if (Children.Count > 0)
        {
            if (Args.Count > 0) sb.Append(", ");

            sb.Append("children: ");
            sb.Append(Children.Count);
        }

        sb.Append(')');
        return sb.ToString();
    }
}

#endregion