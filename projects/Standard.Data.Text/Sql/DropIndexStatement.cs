namespace Std.Data.Text.Sql;

/// <summary>
///     DROP INDEX 语句
///     。
/// </summary>
public sealed class DropIndexStatement : SqlNode
{
    public DropIndexStatement(string name, bool ifExists = false)
    {
        this.name = name;
        if_exists = ifExists;
    }

    public string name { get; }
    public bool if_exists { get; }

    public override string ToString()
    {
        var exists = if_exists ? "IF EXISTS " : "";
        return $"DROP INDEX {exists}{name}";
    }
}