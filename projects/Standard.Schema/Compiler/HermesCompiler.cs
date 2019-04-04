namespace Hermes.Compiler;

public sealed class CompilationResult
{
    public CompilationResult(SchemaIR? schema, SchemaDiagnosticSink diagnostics)
    {
        Schema = schema;
        Diagnostics = diagnostics;
    }

    public SchemaIR? Schema { get; }
    public SchemaDiagnosticSink Diagnostics { get; }
    public bool HasErrors => Diagnostics.HasErrors;
    public bool Success => !HasErrors && Schema is not null;
}

public sealed class HermesCompiler
{
    private readonly AstToIrConverter _converter;
    private readonly ValkyrieLanguage _language;
    private readonly ValkyrieLexer _lexer;
    private readonly ValkyrieParser _parser;
    private readonly TypeChecker _typeChecker;

    public HermesCompiler()
    {
        _language = ValkyrieLanguage.Schema;
        _lexer = new ValkyrieLexer(_language);
        _parser = new ValkyrieParser(_language);
        _converter = new AstToIrConverter();
        _typeChecker = new TypeChecker();
    }

    public CompilationResult Compile(string filePath)
    {
        if (Directory.Exists(filePath)) return CompileDirectory(filePath);

        var diagnostics = new SchemaDiagnosticSink();

        try
        {
            var source = File.ReadAllText(filePath);

            var tokens = _lexer.Tokenize(source);

            var ast = _parser.Parse(tokens);

            if (ast is not CompilationUnit compilationUnit)
            {
                diagnostics.AddError(filePath, 0, 0, "HER3001", "解析结果不是 CompilationUnit");
                return new CompilationResult(null, diagnostics);
            }

            var ir = _converter.Convert(compilationUnit, filePath, source);

            _typeChecker.Check(ir, diagnostics);

            return new CompilationResult(ir, diagnostics);
        }
        catch (FileNotFoundException)
        {
            diagnostics.AddError(filePath, 0, 0, "HER3002", $"文件未找到：{filePath}");
            return new CompilationResult(null, diagnostics);
        }
        catch (IOException ex)
        {
            diagnostics.AddError(filePath, 0, 0, "HER3003", $"文件读取失败：{ex.Message}");
            return new CompilationResult(null, diagnostics);
        }
        catch (InvalidOperationException ex)
        {
            diagnostics.AddError(filePath, 0, 0, "HER3000", $"编译过程中发生语义错误：{ex.Message}");
            return new CompilationResult(null, diagnostics);
        }
        catch (ArgumentException ex)
        {
            diagnostics.AddError(filePath, 0, 0, "HER3000", $"编译过程中发生参数错误：{ex.Message}");
            return new CompilationResult(null, diagnostics);
        }
    }

    public CompilationResult CompileDirectory(string directoryPath)
    {
        var diagnostics = new SchemaDiagnosticSink();

        try
        {
            var schemaFiles = Directory.GetFiles(directoryPath, "*.hermes", SearchOption.AllDirectories)
                .Concat(Directory.GetFiles(directoryPath, "*.her", SearchOption.AllDirectories))
                .OrderBy(f => f)
                .ToList();

            if (schemaFiles.Count == 0)
            {
                diagnostics.AddError(directoryPath, 0, 0, "HER3004", $"目录中未找到 .hermes 或 .he 文件：{directoryPath}");
                return new CompilationResult(null, diagnostics);
            }

            var mergedIr = CompileAndMerge(schemaFiles, diagnostics);
            if (mergedIr is null) return new CompilationResult(null, diagnostics);

            _typeChecker.Check(mergedIr, diagnostics);

            return new CompilationResult(mergedIr, diagnostics);
        }
        catch (DirectoryNotFoundException ex)
        {
            diagnostics.AddError(directoryPath, 0, 0, "HER3002", $"目录未找到：{ex.Message}");
            return new CompilationResult(null, diagnostics);
        }
        catch (IOException ex)
        {
            diagnostics.AddError(directoryPath, 0, 0, "HER3003", $"目录读取失败：{ex.Message}");
            return new CompilationResult(null, diagnostics);
        }
        catch (UnauthorizedAccessException ex)
        {
            diagnostics.AddError(directoryPath, 0, 0, "HER3003", $"目录访问被拒绝：{ex.Message}");
            return new CompilationResult(null, diagnostics);
        }
    }

    private SchemaIR? CompileAndMerge(List<string> schemaFiles, SchemaDiagnosticSink diagnostics)
    {
        var allClasses = new List<ClassDefinition>();
        var allEnums = new List<EnumDefinition>();
        var allFlags = new List<FlagsDefinition>();
        var allUnions = new List<UnionDefinition>();
        var allStorages = new List<StorageDefinition>();
        var allServices = new List<ServiceDefinition>();
        var allMicros = new List<MicroDefinition>();
        var allUsings = new List<UsingEntry>();
        var @namespace = string.Empty;
        var primaryNamespaces = new Dictionary<string, string>();

        foreach (var filePath in schemaFiles)
            try
            {
                var source = File.ReadAllText(filePath);
                var tokens = _lexer.Tokenize(source);
                var ast = _parser.Parse(tokens);

                if (ast is not CompilationUnit compilationUnit)
                {
                    diagnostics.AddError(filePath, 0, 0, "HER3001", "解析结果不是 CompilationUnit");
                    continue;
                }

                var ir = _converter.Convert(compilationUnit, filePath);

                if (!string.IsNullOrEmpty(ir.Namespace))
                {
                    var dir = Path.GetDirectoryName(Path.GetFullPath(filePath)) ?? "";
                    if (ir.NamespaceIsPrimary)
                    {
                        if (primaryNamespaces.TryGetValue(dir, out var existingNs))
                        {
                            if (existingNs != ir.Namespace)
                                diagnostics.AddError(filePath, 0, 0, "HER2003",
                                    $"同一目录下多重主命名空间冲突：文件 '{Path.GetFileName(filePath)}' 声明了 namespace! {ir.Namespace}，" +
                                    $"但该目录已声明了 namespace! {existingNs}。每个目录只能有一个 namespace! 声明");
                        }
                        else
                        {
                            primaryNamespaces[dir] = ir.Namespace;
                            if (string.IsNullOrEmpty(@namespace)) @namespace = ir.Namespace;
                        }
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(@namespace)) @namespace = ir.Namespace;
                    }
                }

                allClasses.AddRange(ir.Classes);
                allEnums.AddRange(ir.Enums);
                allFlags.AddRange(ir.Flags);
                allUnions.AddRange(ir.Unions);
                allServices.AddRange(ir.Services);
                allMicros.AddRange(ir.Micros);
                allUsings.AddRange(ir.Usings);

                MergeStorages(allStorages, ir.Storages);
            }
            catch (FileNotFoundException ex)
            {
                diagnostics.AddError(filePath, 0, 0, "HER3002", $"文件未找到：{ex.Message}");
            }
            catch (IOException ex)
            {
                diagnostics.AddError(filePath, 0, 0, "HER3003", $"文件读取失败：{ex.Message}");
            }
            catch (InvalidOperationException ex)
            {
                diagnostics.AddError(filePath, 0, 0, "HER3000", $"编译文件失败：{ex.Message}");
            }
            catch (ArgumentException ex)
            {
                diagnostics.AddError(filePath, 0, 0, "HER3000", $"编译文件失败：{ex.Message}");
            }

        return new SchemaIR(
            classes: allClasses,
            enums: allEnums,
            flags: allFlags,
            unions: allUnions,
            storages: allStorages,
            services: allServices,
            @namespace: @namespace,
            usings: allUsings,
            micros: allMicros,
            sourceFile: schemaFiles.FirstOrDefault());
    }

    private static void MergeStorages(List<StorageDefinition> existing, IReadOnlyList<StorageDefinition> incoming)
    {
        foreach (var storage in incoming)
        {
            var existingStorage = existing.FirstOrDefault(s => s.Name == storage.Name);
            if (existingStorage is not null)
            {
                var mergedModels = existingStorage.Models.Concat(storage.Models).ToList();
                var mergedStreams = existingStorage.Streams.Concat(storage.Streams).ToList();
                var mergedCaches = existingStorage.Caches.Concat(storage.Caches).ToList();
                var mergedAttrs = existingStorage.Attributes.Concat(storage.Attributes ?? []).ToList();
                var index = existing.IndexOf(existingStorage);
                var entityType = new NamedType(storage.Name);
                existing[index] = new StorageDefinition(storage.Name, entityType, mergedModels, mergedStreams,
                    mergedCaches, mergedAttrs);
            }
            else
            {
                existing.Add(storage);
            }
        }
    }

    public CompilationResult CompileSource(string source, string? filePath = null)
    {
        var diagnostics = new SchemaDiagnosticSink();

        try
        {
            var tokens = _lexer.Tokenize(source);

            var ast = _parser.Parse(tokens);

            if (ast is not CompilationUnit compilationUnit)
            {
                diagnostics.AddError(filePath, 0, 0, "HER3001", "解析结果不是 CompilationUnit");
                return new CompilationResult(null, diagnostics);
            }

            var ir = _converter.Convert(compilationUnit, filePath, source);

            _typeChecker.Check(ir, diagnostics);

            return new CompilationResult(ir, diagnostics);
        }
        catch (InvalidOperationException ex)
        {
            diagnostics.AddError(filePath, 0, 0, "HER3000", $"编译过程中发生语义错误：{ex.Message}");
            return new CompilationResult(null, diagnostics);
        }
        catch (ArgumentException ex)
        {
            diagnostics.AddError(filePath, 0, 0, "HER3000", $"编译过程中发生参数错误：{ex.Message}");
            return new CompilationResult(null, diagnostics);
        }
    }
}