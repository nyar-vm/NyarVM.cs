namespace Hermes.Migration;

public enum MigrationOperationType
{
    AddColumn,
    DropColumn,
    AlterColumn,
    CreateTable,
    DropTable,
    CreateIndex,
    DropIndex
}

public sealed class MigrationOperation
{
    public MigrationOperationType Type { get; init; }
    public string Table { get; init; } = "";
    public string? Column { get; init; }
    public string? DataType { get; init; }
    public bool Nullable { get; init; }
    public object? DefaultValue { get; init; }
    public string? IndexName { get; init; }
    public IReadOnlyList<string> IndexColumns { get; init; } = [];
}