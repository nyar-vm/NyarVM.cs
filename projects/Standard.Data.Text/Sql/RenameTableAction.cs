namespace Std.Data.Text.Sql;

public sealed class RenameTableAction : AlterTableAction
{
    public RenameTableAction(string newName)
    {
        new_name = newName;
    }

    public string new_name { get; }

    public override string ToString()
    {
        return $"RENAME TO {new_name}";
    }
}