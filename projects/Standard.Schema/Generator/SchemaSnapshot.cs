using System.Text.Json;
using Hermes.Compiler;

namespace Hermes.Generator;

public sealed class SchemaSnapshot
{
    private static readonly string SnapshotDir = ".hermes";
    private static readonly string SnapshotFile = "snapshot.json";

    private SchemaSnapshot(string schemaSourcePath, string snapshotDirectory, DateTime createdAt, string ns,
        IReadOnlyList<string> typeNames)
    {
        SchemaSourcePath = schemaSourcePath;
        SnapshotDirectory = snapshotDirectory;
        CreatedAt = createdAt;
        Namespace = ns;
        TypeNames = typeNames;
    }

    public string SchemaSourcePath { get; }
    public string SnapshotDirectory { get; }
    public DateTime CreatedAt { get; }
    public string Namespace { get; }
    public IReadOnlyList<string> TypeNames { get; }

    public static SchemaSnapshot Save(SchemaIR schema, string schemaSourcePath, string outputRoot)
    {
        var snapshotDir = Path.Combine(outputRoot, SnapshotDir);
        Directory.CreateDirectory(snapshotDir);

        var typeNames = ExtractTypeNames(schema);

        var snapshot = new SchemaSnapshot(
            schemaSourcePath,
            snapshotDir,
            DateTime.UtcNow,
            schema.Namespace,
            typeNames
        );

        var json = JsonSerializer.Serialize(new SnapshotData
        {
            SchemaSourcePath = schemaSourcePath,
            CreatedAt = snapshot.CreatedAt,
            Namespace = schema.Namespace,
            TypeNames = typeNames
        }, new JsonSerializerOptions { WriteIndented = true });

        var snapshotPath = Path.Combine(snapshotDir, SnapshotFile);
        File.WriteAllText(snapshotPath, json);

        if (!string.IsNullOrEmpty(schemaSourcePath) && File.Exists(schemaSourcePath))
        {
            var sourceCopyPath = Path.Combine(snapshotDir, "schema.her");
            File.Copy(schemaSourcePath, sourceCopyPath, true);
        }

        return snapshot;
    }

    public static SchemaSnapshot? TryLoad(string outputRoot)
    {
        var snapshotDir = Path.Combine(outputRoot, SnapshotDir);
        var snapshotPath = Path.Combine(snapshotDir, SnapshotFile);

        if (!File.Exists(snapshotPath)) return null;

        var json = File.ReadAllText(snapshotPath);
        var data = JsonSerializer.Deserialize<SnapshotData>(json);

        if (data is null) return null;

        return new SchemaSnapshot(
            data.SchemaSourcePath,
            snapshotDir,
            data.CreatedAt,
            data.Namespace,
            data.TypeNames
        );
    }

    public SchemaIR? LoadPreviousSchema()
    {
        var sourceCopyPath = Path.Combine(SnapshotDirectory, "schema.her");

        if (!File.Exists(sourceCopyPath)) return null;

        var compiler = new HermesCompiler();
        var result = compiler.Compile(sourceCopyPath);

        return result.Success ? result.Schema : null;
    }

    private static List<string> ExtractTypeNames(SchemaIR schema)
    {
        var names = new List<string>();

        foreach (var cls in schema.Classes) names.Add(cls.Name);

        foreach (var en in schema.Enums) names.Add(en.Name);

        foreach (var f in schema.Flags) names.Add(f.Name);

        foreach (var u in schema.Unions) names.Add(u.Name);

        foreach (var s in schema.Storages) names.Add(s.Name);

        foreach (var s in schema.Services) names.Add(s.Name);

        return names;
    }

    private sealed class SnapshotData
    {
        public string SchemaSourcePath { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public string Namespace { get; set; } = "";
        public List<string> TypeNames { get; set; } = [];
    }
}