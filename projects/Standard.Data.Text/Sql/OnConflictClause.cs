namespace Std.Data.Text.Sql;

/// <summary>
///     ON CONFLICT 子句
///     。
/// </summary>
public sealed class OnConflictClause : SqlNode
{
    public OnConflictClause(
        IReadOnlyList<string>? conflictColumns = null,
        ConflictAction? action = null,
        IReadOnlyList<(string Column, SqlExpression Value)>? updateAssignments = null,
        SqlExpression? updateWhere = null)
    {
        conflict_columns = conflictColumns;
        this.action = action;
        update_assignments = updateAssignments;
        update_where = updateWhere;
    }

    public IReadOnlyList<string>? conflict_columns { get; }
    public ConflictAction? action { get; }


    /// <summary>
    ///     DO UPDATE SET 时的赋值列表
    ///     。
    /// </summary>
    public IReadOnlyList<(string Column, SqlExpression Value)>? update_assignments { get; }


    /// <summary>
    ///     DO UPDATE 时的 WHERE 条件
    ///     。
    /// </summary>
    public SqlExpression? update_where { get; }

    public override string ToString()
    {
        var cols = conflict_columns is { Count: > 0 }
            ? $"({string.Join(", ", conflict_columns)})"
            : "";

        var action = this.action switch
        {
            ConflictAction.rollback => "ROLLBACK",
            ConflictAction.abort => "ABORT",
            ConflictAction.fail => "FAIL",
            ConflictAction.ignore => "OR IGNORE", // INSERT OR IGNORE / DO NOTHING
            ConflictAction.replace => "OR REPLACE", // INSERT OR REPLACE / DO UPDATE
            _ => ""
        };

        var upsert = this.action == ConflictAction.replace && update_assignments is { Count: > 0 }
            ? $"DO UPDATE SET {string.Join(", ", update_assignments.Select(a => $"{a.Column} = {a.Value}"))}"
            : this.action == ConflictAction.ignore
                ? "DO NOTHING"
                : "";

        return $"ON CONFLICT{cols}{action} {upsert}".TrimEnd();
    }
}