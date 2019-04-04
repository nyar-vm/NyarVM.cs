using System.Reflection;
using Hermes.Migration;

namespace Hermes.Generator;

public sealed class GeneratorDispatcher
{
    private readonly Dictionary<string, IGenerator> _generators = new();

    public void Register(IGenerator generator)
    {
        _generators[generator.Name] = generator;
    }

    /// <summary>
    ///     注册默认生成器——通过反射扫描当前 AppDomain 中所有实现了 <see cref="IGenerator" /> 接口的类
    /// </summary>
    public void RegisterDefaultGenerators()
    {
        var generatorType = typeof(IGenerator);

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            try
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (type.IsAbstract || type.IsInterface || !generatorType.IsAssignableFrom(type)) continue;

                    if (_generators.Values.Any(g => g.GetType() == type)) continue;

                    try
                    {
                        if (Activator.CreateInstance(type) is IGenerator generator) Register(generator);
                    }
                    catch (MissingMethodException)
                    {
                    }
                    catch (TypeLoadException)
                    {
                    }
                }
            }
            catch (ReflectionTypeLoadException)
            {
            }
    }

    public DispatcherResult Dispatch(SchemaIR schema, string outputPath,
        Dictionary<string, Dictionary<string, object>> generatorConfigs, string? schemaPath = null)
    {
        var allFiles = new List<GeneratedFile>();
        var allErrors = new List<string>();
        var allWarnings = new List<string>();

        foreach (var (name, config) in generatorConfigs)
        {
            if (!_generators.TryGetValue(name, out var generator))
            {
                allErrors.Add($"未注册的生成器: {name}");
                continue;
            }

            var enabled = config.TryGetValue("enabled", out var enabledVal) && enabledVal is bool b && b;
            if (!enabled) continue;

            var context = new GeneratorContext
            {
                Schema = schema,
                SchemaPath = schemaPath ?? schema.SourceFile ?? "",
                OutputPath =
                    config.TryGetValue("output", out var output) ? output.ToString() ?? outputPath : outputPath,
                Options = config,
                Diagnostics = new Nyar.Dialect.Schema.Diagnostics.SchemaDiagnosticSink()
            };

            var result = generator.Generate(context);
            allFiles.AddRange(result.Files);
            allErrors.AddRange(result.Errors);
            allWarnings.AddRange(result.Warnings);
        }

        return new DispatcherResult(allFiles, allErrors, allWarnings);
    }

    public IncrementalDispatchResult DispatchIncremental(
        SchemaIR schema,
        SchemaIR previousSchema,
        string outputPath,
        Dictionary<string, Dictionary<string, object>> generatorConfigs,
        string? schemaPath = null)
    {
        var differ = new SchemaDiffer();
        var diff = differ.Diff(previousSchema, schema);

        if (!diff.HasChanges) return new IncrementalDispatchResult([], [], [], [], 0, 0);

        var extractor = new ChangedTypeExtractor();
        var (changedTypeNames, removedTypeNames) = extractor.Extract(diff);

        var allFiles = new List<GeneratedFile>();
        var allErrors = new List<string>();
        var allWarnings = new List<string>();
        var skippedCount = 0;

        foreach (var (name, config) in generatorConfigs)
        {
            if (!_generators.TryGetValue(name, out var generator))
            {
                allErrors.Add($"未注册的生成器: {name}");
                continue;
            }

            var enabled = config.TryGetValue("enabled", out var enabledVal) && enabledVal is bool b && b;
            if (!enabled) continue;

            var generatorOutputPath = config.TryGetValue("output", out var output)
                ? output.ToString() ?? outputPath
                : outputPath;

            if (generator is IIncrementalGenerator incrementalGenerator)
            {
                var incrementalContext = new IncrementalGeneratorContext
                {
                    Schema = schema,
                    PreviousSchema = previousSchema,
                    Diff = diff,
                    ChangedTypeNames = changedTypeNames,
                    RemovedTypeNames = removedTypeNames,
                    SchemaPath = schemaPath ?? schema.SourceFile ?? "",
                    OutputPath = generatorOutputPath,
                    Options = config,
                    Diagnostics = new Nyar.Dialect.Schema.Diagnostics.SchemaDiagnosticSink()
                };

                var result = incrementalGenerator.GenerateIncremental(incrementalContext);
                allFiles.AddRange(result.Files);
                allErrors.AddRange(result.Errors);
                allWarnings.AddRange(result.Warnings);
            }
            else
            {
                var context = new GeneratorContext
                {
                    Schema = schema,
                    SchemaPath = schemaPath ?? schema.SourceFile ?? "",
                    OutputPath = generatorOutputPath,
                    Options = config,
                    Diagnostics = new Nyar.Dialect.Schema.Diagnostics.SchemaDiagnosticSink()
                };

                var result = generator.Generate(context);

                foreach (var file in result.Files)
                {
                    if (!string.IsNullOrEmpty(file.TypeName) && !changedTypeNames.Contains(file.TypeName))
                    {
                        skippedCount++;
                        continue;
                    }

                    if (!string.IsNullOrEmpty(file.TypeName) && changedTypeNames.Contains(file.TypeName))
                        file.ChangeKind = FileChangeKind.Modified;

                    allFiles.Add(file);
                }

                allErrors.AddRange(result.Errors);
                allWarnings.AddRange(result.Warnings);
            }
        }

        var deletedFiles = GenerateDeletedFileMarkers(removedTypeNames, outputPath, generatorConfigs);

        return new IncrementalDispatchResult(allFiles, deletedFiles, allErrors, allWarnings, changedTypeNames.Count,
            removedTypeNames.Count, skippedCount);
    }

    private static List<GeneratedFile> GenerateDeletedFileMarkers(
        ISet<string> removedTypeNames,
        string outputPath,
        Dictionary<string, Dictionary<string, object>> generatorConfigs)
    {
        var deletedFiles = new List<GeneratedFile>();

        foreach (var typeName in removedTypeNames)
        foreach (var (genName, config) in generatorConfigs)
        {
            var enabled = config.TryGetValue("enabled", out var enabledVal) && enabledVal is bool b && b;
            if (!enabled) continue;

            var generatorOutputPath = config.TryGetValue("output", out var output)
                ? output.ToString() ?? outputPath
                : outputPath;

            var extensions = GetFileExtensions(genName);
            foreach (var ext in extensions)
                deletedFiles.Add(new GeneratedFile
                {
                    Path = Path.Combine(generatorOutputPath, $"{typeName}{ext}"),
                    Content = "",
                    Generator = genName,
                    ChangeKind = FileChangeKind.Deleted,
                    TypeName = typeName
                });
        }

        return deletedFiles;
    }

    private static string[] GetFileExtensions(string generatorName)
    {
        return generatorName switch
        {
            "csharp" => [".cs"],
            "dto" => [".cs"],
            "typescript" => [".ts"],
            "java" => [".java"],
            "go" => [".go"],
            "rust" => [".rs"],
            "sql-ddl" => [".sql"],
            "ggscript" => [".ggs"],
            _ => [".gen"]
        };
    }
}

public sealed class DispatcherResult
{
    public DispatcherResult(IReadOnlyList<GeneratedFile> files, IReadOnlyList<string> errors,
        IReadOnlyList<string> warnings)
    {
        Files = files;
        Errors = errors;
        Warnings = warnings;
    }

    public IReadOnlyList<GeneratedFile> Files { get; }
    public IReadOnlyList<string> Errors { get; }
    public IReadOnlyList<string> Warnings { get; }
    public bool Success => Errors.Count == 0;
}

public sealed class IncrementalDispatchResult
{
    public IncrementalDispatchResult(
        IReadOnlyList<GeneratedFile> files,
        IReadOnlyList<GeneratedFile> deletedFiles,
        IReadOnlyList<string> errors,
        IReadOnlyList<string> warnings,
        int changedTypeCount,
        int removedTypeCount,
        int skippedTypeCount = 0)
    {
        Files = files;
        DeletedFiles = deletedFiles;
        Errors = errors;
        Warnings = warnings;
        ChangedTypeCount = changedTypeCount;
        RemovedTypeCount = removedTypeCount;
        SkippedTypeCount = skippedTypeCount;
    }

    public IReadOnlyList<GeneratedFile> Files { get; }
    public IReadOnlyList<GeneratedFile> DeletedFiles { get; }
    public IReadOnlyList<string> Errors { get; }
    public IReadOnlyList<string> Warnings { get; }
    public int ChangedTypeCount { get; }
    public int RemovedTypeCount { get; }
    public int SkippedTypeCount { get; }
    public bool Success => Errors.Count == 0;
}