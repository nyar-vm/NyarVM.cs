namespace Hermes.YYDB.Query;

/// <summary>
///     UPSERT 查询（INSERT ... ON CONFLICT ... DO UPDATE）
/// </summary>
public sealed class UpsertQuery : QueryExpression
{
    public UpsertQuery(
        string targetTypeName,
        IReadOnlyList<FieldAssignment> insertAssignments,
        IReadOnlyList<string> conflictFields,
        UpsertStrategy strategy = UpsertStrategy.DoUpdate,
        IReadOnlyList<FieldAssignment>? updateAssignments = null,
        QueryPredicate? updatePredicate = null)
        : base(targetTypeName)
    {
        InsertAssignments = insertAssignments;
        ConflictFields = conflictFields;
        Strategy = strategy;
        UpdateAssignments = updateAssignments ?? [];
        UpdatePredicate = updatePredicate;
    }

    public override string QueryKind => "upsert";

    /// <summary>
    ///     插入字段赋值列表
    /// </summary>
    public IReadOnlyList<FieldAssignment> InsertAssignments { get; }

    /// <summary>
    ///     冲突字段列表（ON CONFLICT 的目标列）
    /// </summary>
    public IReadOnlyList<string> ConflictFields { get; }

    /// <summary>
    ///     更新字段赋值列表（DO UPDATE SET ...）
    /// </summary>
    public IReadOnlyList<FieldAssignment> UpdateAssignments { get; }

    /// <summary>
    ///     UPSERT 策略
    /// </summary>
    public UpsertStrategy Strategy { get; }

    /// <summary>
    ///     更新条件（DO UPDATE WHERE ...）
    /// </summary>
    public QueryPredicate? UpdatePredicate { get; }
}

/// <summary>
///     UPSERT 策略
/// </summary>
public enum UpsertStrategy
{
    /// <summary>
    ///     ON CONFLICT DO UPDATE — 冲突时更新
    /// </summary>
    DoUpdate,

    /// <summary>
    ///     ON CONFLICT DO NOTHING — 冲突时忽略
    /// </summary>
    DoNothing
}