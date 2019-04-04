using System.Text.Json;

namespace Atlas.CLI;

public sealed class GenerateManager
{
    private readonly string _database_provider;
    private readonly bool _dry_run;
    private readonly List<string> _generators;
    private readonly string _namespace;
    private readonly string _output_path;
    private readonly string _schema_path;

    public GenerateManager(AtlasConfig config, bool dryRun = false)
    {
        _schema_path = config.schema_path;
        _output_path = config.output_path;
        _generators = config.generators;
        _namespace = config.@namespace ?? "Models";
        _database_provider = config.database_provider;
        _dry_run = dryRun;
    }

    public GenerateResult generate()
    {
        try
        {
            var compiler = new HermesCompiler();
            var compilationResult = compiler.Compile(_schema_path);

            if (!compilationResult.Success)
            {
                var errors = compilationResult.Diagnostics.Diagnostics
                    .Where(d => d.Level == SchemaDiagnosticLevel.Error)
                    .Select(d => $"[{d.Code}] {d.Message}")
                    .ToList();

                var warnings = compilationResult.Diagnostics.Diagnostics
                    .Where(d => d.Level == SchemaDiagnosticLevel.Warning)
                    .Select(d => $"[{d.Code}] {d.Message}")
                    .ToList();

                if (warnings.Count > 0)
                {
                    Console.WriteLine("⚠️ 编译警告:");
                    foreach (var w in warnings) Console.WriteLine($"  {w}");
                }

                if (compilationResult.Schema is not null)
                {
                    var compiledSchema = compilationResult.Schema;
                    Console.WriteLine("📊 已收集的类型:");
                    Console.WriteLine($"   Classes: {string.Join(", ", compiledSchema.Classes.Select(c => c.Name))}");
                    Console.WriteLine($"   Enums: {string.Join(", ", compiledSchema.Enums.Select(e => e.Name))}");
                    Console.WriteLine($"   Unions: {string.Join(", ", compiledSchema.Unions.Select(u => u.Name))}");
                    Console.WriteLine($"   Flags: {string.Join(", ", compiledSchema.Flags.Select(f => f.Name))}");
                    Console.WriteLine(
                        $"   Models: {string.Join(", ", compiledSchema.Storages.SelectMany(s => s.Models.Select(m => m.Name)))}");
                }

                return GenerateResult.fail($"Schema 编译失败:\n  {string.Join("\n  ", errors)}");
            }

            var schema = compilationResult.Schema!;
            var ns = !string.IsNullOrEmpty(_namespace) ? _namespace : schema.Namespace;

            Console.WriteLine($"📊 Schema 分析: {schema.Classes.Count} 个 class, {schema.Enums.Count} 个 enum, " +
                              $"{schema.Storages.Sum(s => s.Models.Count)} 个 model, {schema.Unions.Count} 个 union");

            var dispatcher = new GeneratorDispatcher();
            foreach (var genName in _generators)
                switch (genName.ToLower())
                {
                    case "csharp":
                        dispatcher.Register(new CSharpGenerator());
                        break;
                    case "dto":
                        dispatcher.Register(new DtoGenerator());
                        break;
                    case "sql":
                    case "sql-ddl":
                    case "sql_ddl":
                        dispatcher.Register(new SqlPlugin());
                        break;
                    default:
                        Console.WriteLine($"⚠️ 未知生成器: {genName}");
                        break;
                }

            var hermesOutputPath = Path.Combine(_output_path, "Hermes");
            var generatorConfigs = build_generator_configs(ns, hermesOutputPath);

            var dispatchResult = dispatcher.Dispatch(schema, hermesOutputPath, generatorConfigs, _schema_path);

            if (!dispatchResult.Success)
                return GenerateResult.fail($"生成失败:\n  {string.Join("\n  ", dispatchResult.Errors)}");

            var atlasOutputPath = Path.Combine(_output_path, "Atlas");
            var atlasGenerator = new AtlasCodeGenerator(atlasOutputPath);
            var atlasResult = atlasGenerator.Generate(schema, _schema_path, ns);

            if (_dry_run)
            {
                Console.WriteLine("🏃 DryRun 模式，预览生成文件：");
                Console.WriteLine("  === Hermes ===");
                foreach (var file in dispatchResult.Files)
                    Console.WriteLine($"  📄 {file.Path} ({file.Content.Length} 字符)");

                Console.WriteLine("  === Atlas ===");
                foreach (var file in atlasResult.Files)
                    Console.WriteLine($"  📄 {file.Path} ({file.Content.Length} 字符)");

                return new GenerateResult
                {
                    success = true,
                    files_written = 0,
                    warnings = dispatchResult.Warnings.ToList()
                };
            }

            var hermesWritten = write_hermes_files(dispatchResult.Files);
            var atlasWritten = write_atlas_files(atlasResult.Files);
            var totalWritten = hermesWritten + atlasWritten;

            save_snapshot(schema);

            foreach (var warning in dispatchResult.Warnings) Console.WriteLine($"⚠️ {warning}");

            Console.WriteLine($"✅ Generate 完成: {totalWritten} 个文件已生成 (Hermes: {hermesWritten}, Atlas: {atlasWritten})");

            return new GenerateResult
            {
                success = true,
                files_written = totalWritten,
                warnings = dispatchResult.Warnings.ToList()
            };
        }
        catch (Exception ex)
        {
            return GenerateResult.fail($"生成失败: {ex.Message}");
        }
    }

    /// <summary>
    ///     同步 Schema 到数据库
    /// </summary>
    public async Task<SyncResult> sync_to_database(string connectionString, bool dryRun = false)
    {
        var compiler = new HermesCompiler();
        var compilationResult = compiler.Compile(_schema_path);

        if (!compilationResult.Success) return SyncResult.Fail("Schema 编译失败");

        var plugin = new SqlPlugin();
        var options = new SyncOptions
        {
            Dialect = _database_provider,
            DryRun = dryRun
        };

        return await plugin.SyncToDatabaseAsync(compilationResult.Schema!, connectionString, options);
    }

    /// <summary>
    ///     执行 DDL 到数据库
    /// </summary>
    public async Task<ExecuteResult> execute_ddl(string connectionString)
    {
        var compiler = new HermesCompiler();
        var compilationResult = compiler.Compile(_schema_path);

        if (!compilationResult.Success) return ExecuteResult.Fail("Schema 编译失败");

        var plugin = new SqlPlugin();
        return await plugin.ExecuteDdlAsync(compilationResult.Schema!, connectionString, _database_provider);
    }

    private Dictionary<string, Dictionary<string, object>> build_generator_configs(string ns, string hermesOutputPath)
    {
        var configs = new Dictionary<string, Dictionary<string, object>>();

        foreach (var genName in _generators)
        {
            var normalizedName = genName.ToLower() switch
            {
                "sql_ddl" => "sql",
                "sql-ddl" => "sql",
                _ => genName.ToLower()
            };

            var config = new Dictionary<string, object>
            {
                ["enabled"] = true
            };

            switch (normalizedName)
            {
                case "csharp":
                    config["namespace"] = ns;
                    config["targets"] = "record";
                    config["output"] = Path.Combine(hermesOutputPath, "Models");
                    break;
                case "dto":
                    config["namespace"] = ns;
                    config["targets"] = "dto";
                    config["output"] = Path.Combine(hermesOutputPath, "Dto");
                    break;
                case "sql":
                    config["targets"] = _database_provider;
                    config["output"] = Path.Combine(hermesOutputPath, "Migrations");
                    config["include-dml"] = false;
                    break;
            }

            configs[normalizedName] = config;
        }

        return configs;
    }

    private int write_hermes_files(IReadOnlyList<GeneratedFile> files)
    {
        var filesWritten = 0;

        foreach (var file in files)
        {
            if (file.ChangeKind == FileChangeKind.Deleted)
            {
                if (File.Exists(file.Path))
                {
                    File.Delete(file.Path);
                    Console.WriteLine($"  🗑️ {file.Path}");
                }

                continue;
            }

            var dir = Path.GetDirectoryName(file.Path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);

            File.WriteAllText(file.Path, file.Content);
            filesWritten++;
            Console.WriteLine($"  [Hermes] ✅ {file.Path}");
        }

        return filesWritten;
    }

    private int write_atlas_files(List<AtlasGenFile> files)
    {
        var filesWritten = 0;

        foreach (var file in files)
        {
            var dir = Path.GetDirectoryName(file.Path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);

            File.WriteAllText(file.Path, file.Content);
            filesWritten++;
            Console.WriteLine($"  [Atlas]  ✅ {file.Path}");
        }

        return filesWritten;
    }

    private void save_snapshot(SchemaIR schema)
    {
        var snapshotDir = Path.Combine(_output_path, ".hermes");
        if (!Directory.Exists(snapshotDir)) Directory.CreateDirectory(snapshotDir);

        var typeNames = new List<string>();
        typeNames.AddRange(schema.Classes.Select(c => c.Name));
        typeNames.AddRange(schema.Enums.Select(e => e.Name));
        typeNames.AddRange(schema.Flags.Select(f => f.Name));
        typeNames.AddRange(schema.Unions.Select(u => u.Name));
        foreach (var storage in schema.Storages)
        {
            typeNames.Add(storage.Name);
            typeNames.AddRange(storage.Models.Select(m => m.Name));
            typeNames.AddRange(storage.Streams.Select(s => s.Name));
            typeNames.AddRange(storage.Caches.Select(c => c.Name));
        }

        var snapshot = new SnapshotData
        {
            schema_source_path = Path.GetFullPath(_schema_path),
            created_at = DateTime.UtcNow,
            @namespace = schema.Namespace,
            type_names = typeNames
        };

        var json = JsonSerializer.Serialize(snapshot, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(Path.Combine(snapshotDir, "snapshot.json"), json);
    }

    private sealed class SnapshotData
    {
        public string schema_source_path { get; set; } = "";
        public DateTime created_at { get; set; }
        public string @namespace { get; set; } = "";
        public List<string> type_names { get; set; } = [];
    }
}

public sealed class GenerateResult
{
    public bool success { get; init; }
    public int files_written { get; init; }
    public List<string> warnings { get; init; } = [];
    public string? error_message { get; init; }

    public static GenerateResult fail(string message)
    {
        return new GenerateResult { success = false, error_message = message };
    }
}