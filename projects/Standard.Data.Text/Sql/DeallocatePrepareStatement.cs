namespace Std.Data.Text.Sql;

/// <summary>
///     DEALLOCATE PREPARE 语句
///     。
/// </summary>
public sealed class DeallocatePrepareStatement : SqlNode
{
    public DeallocatePrepareStatement(string name)
    {
        this.name = name;
    }

    public string name { get; }

    public override string ToString()
    {
        return $"DEALLOCATE PREPARE {name}";
    }
}