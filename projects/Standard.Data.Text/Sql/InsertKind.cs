namespace Std.Data.Text.Sql;

/// <summary>
///     INSERT 操作类型
/// </summary>
public enum InsertKind
{
    @default,
    insert,
    insert_or_replace,
    insert_or_ignore,
    replace,
    upsert
}