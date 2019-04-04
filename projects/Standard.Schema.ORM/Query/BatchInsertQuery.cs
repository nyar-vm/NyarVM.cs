namespace Hermes.YYDB.Query;

/// <summary>
///     批量插入查询——将多行数据一次性插入目标表
/// </summary>
public sealed class BatchInsertQuery : QueryExpression
{
    public BatchInsertQuery(string targetTypeName, IReadOnlyList<IReadOnlyList<FieldAssignment>> rows,
        int batchSize = 0)
        : base(targetTypeName)
    {
        Rows = rows;
        BatchSize = batchSize;
    }

    public override string QueryKind => "batch_insert";

    /// <summary>
    ///     每行数据的字段赋值列表
    /// </summary>
    public IReadOnlyList<IReadOnlyList<FieldAssignment>> Rows { get; }

    /// <summary>
    ///     批次大小（0 表示不限制）
    /// </summary>
    public int BatchSize { get; }
}