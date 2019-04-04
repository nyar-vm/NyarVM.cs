using System.Text;

namespace Hermes.Migration;

public sealed class MigrationPlanSerializer
{
    public string Serialize(MigrationPlan plan)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"version: {plan.Version}");
        sb.AppendLine($"description: {plan.Description}");
        sb.AppendLine($"created: {plan.Created:O}");
        sb.AppendLine();

        sb.AppendLine("changes:");
        foreach (var op in plan.Changes) SerializeOperation(sb, op, "  ");

        sb.AppendLine();
        sb.AppendLine("rollback:");
        foreach (var op in plan.Rollback) SerializeOperation(sb, op, "  ");

        return sb.ToString();
    }

    public void SaveToFile(MigrationPlan plan, string outputPath)
    {
        var fileName = $"{plan.Version}_{SanitizeFileName(plan.Description)}.migration.hermes";
        var filePath = Path.Combine(outputPath, fileName);
        Directory.CreateDirectory(outputPath);
        File.WriteAllText(filePath, Serialize(plan));
    }

    private static void SerializeOperation(StringBuilder sb, MigrationOperation op, string indent)
    {
        sb.AppendLine($"{indent}- type: {FormatOperationType(op.type)}");
        sb.AppendLine($"{indent}  table: {op.Table}");

        if (op.Column is not null) sb.AppendLine($"{indent}  column: {op.Column}");

        if (op.DataType is not null) sb.AppendLine($"{indent}  data_type: {op.DataType}");

        if (op.Nullable) sb.AppendLine($"{indent}  nullable: true");

        if (op.DefaultValue is not null) sb.AppendLine($"{indent}  default: {op.DefaultValue}");

        if (op.IndexName is not null) sb.AppendLine($"{indent}  index: {op.IndexName}");

        if (op.IndexColumns.Count > 0) sb.AppendLine($"{indent}  columns: [{string.Join(", ", op.IndexColumns)}]");
    }

    private static string FormatOperationType(MigrationOperationType type)
    {
        return type switch
        {
            MigrationOperationType.AddColumn => "add_column",
            MigrationOperationType.DropColumn => "drop_column",
            MigrationOperationType.AlterColumn => "alter_column",
            MigrationOperationType.CreateTable => "create_table",
            MigrationOperationType.DropTable => "drop_table",
            MigrationOperationType.CreateIndex => "create_index",
            MigrationOperationType.DropIndex => "drop_index",
            _ => type.ToString().ToLowerInvariant()
        };
    }

    private static string SanitizeFileName(string name)
    {
        return name.Replace(' ', '_');
    }
}