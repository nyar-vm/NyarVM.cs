namespace Std.Data.Text.Sql;

public sealed class RenameColumnAction : AlterTableAction
{
    public RenameColumnAction(string oldName, string newName)
    {
        old_name = oldName;
        new_name = newName;
    }

    public string old_name { get; }
    public string new_name { get; }

    public override string ToString()
    {
        return $"RENAME COLUMN {old_name} TO {new_name}";
    }
}