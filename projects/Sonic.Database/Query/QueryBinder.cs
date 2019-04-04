using Olympus.Athena.Core;
using Std.Data.Text.Sql;

namespace Olympus.Athena.Query;

#region BoundColumnRef 绑定列引用

/// <summary>
///     绑定后的列引用，包含表名和类型信息
/// </summary>
public sealed class BoundColumnRef : SqlExpression
{
    /// <summary>
    ///     创建绑定列引用
    /// </summary>
    public BoundColumnRef(string name, string? tableName = null, DataType columnType = DataType.Null,
        int columnIndex = -1)
    {
        Name = name;
        TableName = tableName;
        ColumnType = columnType;
        ColumnIndex = columnIndex;
    }

    /// <summary>
    ///     所属表名
    /// </summary>
    public string? TableName { get; }

    /// <summary>
    ///     列名
    /// </summary>
    public string Name { get; }

    /// <summary>
    ///     列的数据类型
    /// </summary>
    public DataType ColumnType { get; set; }

    /// <summary>
    ///     列在表 Schema 中的索引
    /// </summary>
    public int ColumnIndex { get; set; }

    /// <inheritdoc />
    public override string ToString()
    {
        return TableName is not null ? $"{TableName}.{Name}" : Name;
    }
}

#endregion

#region BoundSelectStmt 绑定查询语句

/// <summary>
///     绑定后的 SELECT 查询语句，使用 Oak.Sql 类型
/// </summary>
public sealed class BoundSelectStmt
{
    /// <summary>
    ///     创建绑定查询语句
    /// </summary>
    public BoundSelectStmt(IReadOnlyList<SqlColumn> columns)
    {
        Columns = columns;
        Joins = [];
        OrderBy = [];
        GroupBy = [];
    }

    /// <summary>
    ///     SELECT 子句的列列表（Oak.SqlColumn）
    /// </summary>
    public IReadOnlyList<SqlColumn> Columns { get; }

    /// <summary>
    ///     FROM 子句的表引用
    /// </summary>
    public SqlTableRef? From { get; set; }

    /// <summary>
    ///     WHERE 子句的过滤表达式（已绑定）
    /// </summary>
    public SqlExpression? Where { get; set; }

    /// <summary>
    ///     JOIN 表引用列表
    /// </summary>
    public IReadOnlyList<SqlTableRef> Joins { get; set; }

    /// <summary>
    ///     ORDER BY 子句列表
    /// </summary>
    public IReadOnlyList<OrderByItem> OrderBy { get; set; }

    /// <summary>
    ///     GROUP BY 子句列表
    /// </summary>
    public IReadOnlyList<SqlExpression> GroupBy { get; set; }

    /// <summary>
    ///     LIMIT 数量
    /// </summary>
    public int? Limit { get; set; }

    /// <summary>
    ///     OFFSET 偏移量
    /// </summary>
    public int? Offset { get; set; }

    /// <summary>
    ///     是否 SELECT DISTINCT
    /// </summary>
    public bool Distinct { get; set; }
}

#endregion

#region BoundQuery 绑定查询

/// <summary>
///     绑定后的完整查询，包含类型信息和表定义引用
/// </summary>
public sealed class BoundQuery
{
    /// <summary>
    ///     创建绑定查询
    /// </summary>
    public BoundQuery(BoundSelectStmt original, IReadOnlyDictionary<string, DataType> columnTypes,
        TableDefinition? fromTable)
    {
        Original = original;
        ColumnTypes = columnTypes;
        FromTable = fromTable;
    }

    /// <summary>
    ///     绑定后的查询语句
    /// </summary>
    public BoundSelectStmt Original { get; }

    /// <summary>
    ///     输出列名到数据类型的映射
    /// </summary>
    public IReadOnlyDictionary<string, DataType> ColumnTypes { get; }

    /// <summary>
    ///     FROM 子句对应的表定义
    /// </summary>
    public TableDefinition? FromTable { get; }
}

#endregion

#region QueryBinder 查询绑定器

/// <summary>
///     查询绑定器，将 Oak.Sql AST 绑定到具体的表定义，检查列引用并推导类型
/// </summary>
public sealed class QueryBinder
{
    #region 字段

    private readonly IReadOnlyDictionary<string, TableDefinition> _tables;

    #endregion

    #region 构造函数

    /// <summary>
    ///     创建查询绑定器
    /// </summary>
    /// <param name="tables">表名到表定义的映射</param>
    public QueryBinder(IReadOnlyDictionary<string, TableDefinition> tables)
    {
        _tables = tables;
    }

    #endregion

    #region 公共方法

    /// <summary>
    ///     绑定 Oak.Sql 的 SelectStatement，解析列引用并推导类型
    /// </summary>
    /// <param name="node">Oak.Sql 解析后的 SQL 节点</param>
    /// <returns>绑定后的查询</returns>
    public BoundQuery Bind(SqlNode node)
    {
        if (node is not SelectStatement select) throw new AthenaException("仅支持 SELECT 查询");

        TableDefinition? fromTable = null;
        if (select.from != null && _tables.TryGetValue(select.from.name, out var table)) fromTable = table;

        var boundColumns = new List<SqlColumn>();
        foreach (var col in select.columns)
            boundColumns.Add(new SqlColumn(BindExpr(col.expression, fromTable), col.alias));

        var joins = select.joins.Select(j =>
            new SqlTableRef(j.name, j.alias, j.join_type,
                j.on_condition != null ? BindExpr(j.on_condition, fromTable) : null)).ToList();

        var bound = new BoundSelectStmt(boundColumns)
        {
            From = select.from is not null
                ? new SqlTableRef(select.from.name, select.from.alias, select.from.join_type,
                    select.from.on_condition != null ? BindExpr(select.from.on_condition, fromTable) : null)
                : null,
            Where = select.where != null ? BindExpr(select.where, fromTable) : null,
            Distinct = select.distinct,
            Limit = select.limit,
            Offset = select.offset,
            Joins = joins,
            OrderBy =
            [
                .. select.order_by.Select(ob =>
                    new OrderByItem(BindExpr(ob.expression, fromTable), ob.descending, ob.nulls_first, ob.nulls_last))
            ],
            GroupBy = [.. select.group_by.Select(gb => BindExpr(gb, fromTable))]
        };

        var columnTypes = BuildColumnTypes(boundColumns, fromTable);

        return new BoundQuery(bound, columnTypes, fromTable);
    }

    #endregion

    #region 内部方法

    private SqlExpression BindExpr(SqlExpression expr, TableDefinition? fromTable)
    {
        switch (expr)
        {
            case ColumnRef colRef:
            {
                if (fromTable != null)
                {
                    var schema = fromTable.Schema;
                    for (var i = 0; i < schema.Count; i++)
                        if (string.Equals(schema[i].Name, colRef.name, StringComparison.OrdinalIgnoreCase))
                            return new BoundColumnRef(colRef.name, fromTable.Name, schema[i].DataType, i);

                    throw new AthenaException($"列 \"{colRef.name}\" 在表 \"{fromTable.Name}\" 中不存在");
                }

                return new BoundColumnRef(colRef.name);
            }
            case BinaryExpr binExpr:
            {
                var left = BindExpr(binExpr.left, fromTable);
                var right = BindExpr(binExpr.right, fromTable);
                return new BinaryExpr(left, binExpr.@operator, right);
            }
            case FunctionCall funcCall:
            {
                var boundArgs = new List<SqlExpression>();
                foreach (var arg in funcCall.arguments) boundArgs.Add(BindExpr(arg, fromTable));

                return new FunctionCall(funcCall.name, boundArgs, funcCall.distinct);
            }
            case UnaryExpr unaryExpr:
            {
                return new UnaryExpr(unaryExpr.@operator, BindExpr(unaryExpr.operand, fromTable));
            }
            case LiteralValue:
            case StarExpression:
            case IsNullExpr:
            {
                return expr;
            }
            default:
            {
                return expr;
            }
        }
    }

    private static IReadOnlyDictionary<string, DataType> BuildColumnTypes(List<SqlColumn> columns,
        TableDefinition? fromTable)
    {
        var types = new Dictionary<string, DataType>(StringComparer.OrdinalIgnoreCase);

        foreach (var col in columns)
            switch (col.expression)
            {
                case BoundColumnRef bcr:
                    types[bcr.Name] = bcr.ColumnType;
                    break;
                case ColumnRef cr when fromTable != null:
                {
                    var schema = fromTable.Schema;
                    for (var i = 0; i < schema.Count; i++)
                        if (string.Equals(schema[i].Name, cr.name, StringComparison.OrdinalIgnoreCase))
                        {
                            types[cr.name] = schema[i].DataType;
                            break;
                        }

                    break;
                }
                case StarExpression:
                    if (fromTable != null)
                        foreach (var cd in fromTable.Schema.Columns)
                            types[cd.Name] = cd.DataType;

                    break;
            }

        return types.AsReadOnly();
    }

    #endregion
}

#endregion