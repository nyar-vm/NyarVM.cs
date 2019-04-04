namespace Hermes.Migration;

public sealed class MigrationPlan
{
    public string Version { get; init; } = "";
    public string Description { get; init; } = "";
    public DateTime Created { get; init; } = DateTime.UtcNow;
    public List<MigrationOperation> Changes { get; init; } = [];
    public List<MigrationOperation> Rollback { get; init; } = [];
}