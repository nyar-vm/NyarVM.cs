namespace Std.Data.Text.Sql;

public sealed class AddColumnAction : AlterTableAction
{
    public AddColumnAction(ColumnDef column)
    {
        this.column = column;
    }

    public ColumnDef column { get; }

    public override string ToString()
    {
        return $"ADD COLUMN {column}";
    }
}