using Nyar.Analyzer.Semantic;
using Nyar.Assembler;
using Nyar.Assembler.Backends.Clr;
using Nyar.Assembler.Backends.Jvm;
using Nyar.Assembler.Backends.Wasm;
using Nyar.Language.Valkyrie.Formatter;
using Nyar.Language.Valkyrie.Compiler.Hir;
using Nyar.Language.Valkyrie.Compiler.Lir;
using Nyar.Language.Valkyrie.Compiler.Meta;
using Nyar.Language.Valkyrie.Compiler.Mir;
using Nyar.Language.Valkyrie.Compiler.Pipeline;
using Nyar.Language.Valkyrie.Compiler.Semantic;
using Nyar.Language.Valkyrie.Compiler.Targets;
using Nyar.Language.Valkyrie.Semantic;
using Nyar.Language.Valkyrie.TypeChecker;
using Nyar.Types.Targets;
using Nyar.VM.NyarVM;
using Std.Data.Binary.Frame;
using Std.Data.Text.Diagnostics;
using Std.Data.Text.Parsing;
using Std.Data.Text.Syntax;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace Nyar.Language.Valkyrie.Compiler;

/// <summary>
///     Valkyrie 新编译器门面
///     主链路：AST -> HIR -> MIR(EGraph) -> LIR -> TargetArtifactEmitter -> ArtifactSet
/// </summary>
public sealed class ValkyrieCompiler
{
    private sealed record CompilationExecutionResult(
        SemanticModel semantics,
        HirModule hir,
        ArtifactSet artifact_set,
        TargetProfile target_profile);

    public sealed record PreparedCompilation(
        IReadOnlyList<CompilationUnit> compilation_units,
        BuildPlan frontend_plan);

    private readonly CanonicalTargetRegistry _canonical_target_registry;
    private readonly ICompilationCache? _cache;
    private readonly TargetArtifactEmitter _emitter;
    private readonly HirBuilder _hir_builder;
    private readonly LirBuilder _lir_builder;
    private readonly MirBuilder _mir_builder;
    private readonly Dictionary<string, string> _source_content_hashes;
    private readonly ValkyrieWorkspaceServices _workspace;

    /// <summary>
    ///     初始化编译器
    /// </summary>
    public ValkyrieCompiler(ICompilationCache? cache = null)
    {
        _cache = cache;
        _workspace = new ValkyrieWorkspaceServices();
        _hir_builder = new HirBuilder();
        _mir_builder = new MirBuilder(_workspace.source_analyzer.diagnostics);
        _lir_builder = new LirBuilder();
        _canonical_target_registry = new CanonicalTargetRegistry();
        _emitter = new TargetArtifactEmitter();
        _source_content_hashes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        register_default_backends();
    }

    /// <summary>
    ///     诊断信息收集器
    /// </summary>
    public DiagnosticSink diagnostics => _workspace.source_analyzer.diagnostics;

    /// <summary>
    ///     词法与语法分析一步完成
    /// </summary>
    public ParseResult<CompilationUnit> parse_source(string source, string filePath = "")
    {
        return _workspace.parse(source, filePath);
    }

    /// <summary>
    ///     词法分析
    /// </summary>
    public IReadOnlyList<GreenLeafNode> lex(string source)
    {
        return _workspace.source_analyzer.lex(source);
    }

    /// <summary>
    ///     对文件执行带缓存的词法分析。
    /// </summary>
    public IReadOnlyList<GreenLeafNode> lex_file(string filePath)
    {
        var normalizedPath = normalize_file_path(filePath);
        var source = File.ReadAllText(normalizedPath);
        return lex_with_cache(source, normalizedPath);
    }

    /// <summary>
    ///     语法分析
    /// </summary>
    public CompilationUnit parse(IReadOnlyList<GreenLeafNode> tokens)
    {
        var result = _workspace.source_analyzer.parse(tokens);
        return result.value!;
    }

    /// <summary>
    ///     语义分析
    /// </summary>
    public SemanticModel analyze(CompilationUnit ast, BuildPlan plan)
    {
        var filePath = plan.file_path ?? plan.module_name;
        var bridge = new ValkyrieSemanticBridge();
        var typeCheckResult = run_type_check([ast]);
        return bridge.build_semantic_model(typeCheckResult, [ast], filePath);
    }

    /// <summary>
    ///     多文件语义分析：Phase 1 收集全局声明，Phase 2 按拓扑序逐文件分析
    /// </summary>
    public SemanticModel analyze(IReadOnlyList<CompilationUnit> asts, BuildPlan plan)
    {
        var filePath = plan.file_path ?? plan.module_name;

        // Phase 1: 收集全局声明
        var collector = new DeclarationCollector();
        var globalTable = collector.collect(asts);

        // Phase 2: 按拓扑序逐文件分析
        var dependencyGraph = new ModuleDependencyGraph();
        var sortedAsts = dependencyGraph.topological_sort(asts, globalTable);
        var bridge = new ValkyrieSemanticBridge();
        var typeCheckResult = run_type_check(sortedAsts);
        return bridge.build_semantic_model(typeCheckResult, sortedAsts, filePath);
    }

    /// <summary>
    ///     构建 HIR
    /// </summary>
    public HirModule build_hir(CompilationUnit ast, SemanticModel semantics, BuildPlan plan)
    {
        return _hir_builder.build(ast, semantics, plan.module_name);
    }

    public HirModule build_hir(IReadOnlyList<CompilationUnit> asts, SemanticModel semantics, BuildPlan plan)
    {
        return _hir_builder.build(asts, semantics, plan.module_name);
    }

    /// <summary>
    ///     构建 MIR（EGraph + Oa）
    /// </summary>
    public MirModule build_mir(HirModule hir, BuildPlan plan, TargetProfile targetProfile)
    {
        var targetArchTag = resolve_target_arch_tag(targetProfile);
        return _mir_builder.build(hir, targetArchTag, plan.preferred_logical_entry);
    }

    /// <summary>
    ///     构建 LIR（Nyar Standard IR / GenerateModule）
    /// </summary>
    public LirModule build_lir(MirModule mir, BuildPlan plan)
    {
        return _lir_builder.build(mir);
    }

    /// <summary>
    ///     编译源码到目标产物
    ///     完整执行：Lex -> MSP Expand -> Parse -> Analyze -> HIR -> MIR -> LIR -> TargetEmit -> ArtifactSet
    /// </summary>
    /// <param name="source">源码文本</param>
    /// <param name="plan">构建计划</param>
    /// <returns>产物集（含主产物、sidecar、运行契约）</returns>
    /// <exception cref="InvalidOperationException">词法、语法、语义分析失败，或目标不受支持</exception>
    public ArtifactSet compile_to_target(string source, BuildPlan plan)
    {
        diagnostics.clear();

        var tokens = lex(source);
        if (diagnostics.has_errors) throw new InvalidOperationException("词法分析失败，无法继续编译。");

        var ast = parse(tokens);
        if (diagnostics.has_errors) throw new InvalidOperationException("语法分析失败，无法继续编译。");
        var execution = execute_pipeline([ast], plan, "语义分析失败，无法继续编译。");
        var runContract = build_run_contract(execution.hir, execution.target_profile, plan.preferred_logical_entry);
        return execution.artifact_set.with_run_contract(runContract);
    }

    public ArtifactSet compile_files_to_target(IReadOnlyList<string> sourceFiles, BuildPlan plan)
    {
        diagnostics.clear();
        var totalStopwatch = Stopwatch.StartNew();

        static void print_phase(string phaseName, Stopwatch stopwatch)
        {
            Console.WriteLine($"[ValkyrieCompiler] {phaseName} completed in {stopwatch.ElapsedMilliseconds} ms");
        }

        var phaseStopwatch = Stopwatch.StartNew();
        var compilationUnits = parse_compilation_units(sourceFiles);
        print_phase("parse_compilation_units", phaseStopwatch);
        if (diagnostics.has_errors)
        {
            var firstError = diagnostics.messages.FirstOrDefault(d => d.severity.is_error_level());
            throw new InvalidOperationException($"语法分析失败，无法继续编译。首个错误：{firstError.message}");
        }

        phaseStopwatch.Restart();
        var execution = execute_pipeline(compilationUnits, plan, "语义分析失败，无法继续编译。");
        print_phase("compile_pipeline", phaseStopwatch);

        phaseStopwatch.Restart();
        var runContract = build_run_contract(execution.hir, execution.target_profile, plan.preferred_logical_entry);
        print_phase("build_run_contract", phaseStopwatch);
        Console.WriteLine($"[ValkyrieCompiler] total completed in {totalStopwatch.ElapsedMilliseconds} ms");
        return execution.artifact_set.with_run_contract(runContract);
    }

    /// <summary>
    ///     预解析源码文件，供后续多次编译复用前端结果。
    /// </summary>
    public PreparedCompilation prepare_files(IReadOnlyList<string> sourceFiles, BuildPlan plan)
    {
        diagnostics.clear();
        var compilationUnits = parse_compilation_units(sourceFiles);
        if (diagnostics.has_errors)
        {
            var firstError = diagnostics.messages.FirstOrDefault(d => d.severity.is_error_level());
            throw new InvalidOperationException($"语法分析失败，无法继续预处理。首个错误：{firstError.message}");
        }

        return new PreparedCompilation(compilationUnits, plan);
    }

    /// <summary>
    ///     基于已准备好的前端结果继续完成后续编译。
    /// </summary>
    public ArtifactSet compile_prepared(PreparedCompilation prepared, BuildPlan plan)
    {
        diagnostics.clear();
        var execution = execute_pipeline(prepared.compilation_units, plan, "语义分析失败，无法继续编译。");
        var runContract = build_run_contract(execution.hir, execution.target_profile, plan.preferred_logical_entry);
        return execution.artifact_set.with_run_contract(runContract);
    }

    /// <summary>
    ///     执行与 `build` 一致的完整检查链路：解析、元语言展开、类型检查、语义分析、HIR、MIR、LIR 与目标校验。
    ///     该过程不落盘产物，但要求同一输入在当前环境下可成功通过 `build` 的编译链路。
    /// </summary>
    public SemanticModel check_files(IReadOnlyList<string> sourceFiles, BuildPlan plan)
    {
        diagnostics.clear();

        var compilationUnits = parse_compilation_units(sourceFiles);
        if (diagnostics.has_errors)
        {
            var firstError = diagnostics.messages.FirstOrDefault(d => d.severity.is_error_level());
            throw new InvalidOperationException($"语法分析失败，无法继续检查。首个错误：{firstError.message}");
        }

        var stagedUnits = stage_compilation_units(compilationUnits, plan.canonical_triple);
        var cachedSemantics = try_get_cached_semantics(stagedUnits, plan);
        if (cachedSemantics is not null)
        {
            append_semantic_diagnostics(cachedSemantics);
            if (cachedSemantics.has_errors)
            {
                throw new InvalidOperationException("语义分析失败，无法继续检查。");
            }

            return cachedSemantics;
        }

        var execution = execute_pipeline(compilationUnits, plan, "语义分析失败，无法继续检查。");
        return execution.semantics;
    }

    private IReadOnlyList<CompilationUnit> parse_compilation_units(IReadOnlyList<string> sourceFiles)
    {
        diagnostics.clear();
        var compilationUnits = new List<CompilationUnit>(sourceFiles.Count);
        foreach (var sourceFile in sourceFiles)
        {
            var normalizedPath = normalize_file_path(sourceFile);
            var source = File.ReadAllText(normalizedPath);
            _source_content_hashes[normalizedPath] = compute_content_hash(source);

            var beforeErrorCount = diagnostics.messages.Count(d => d.severity.is_error_level());
            var tokens = lex_with_cache(source, normalizedPath);
            var parseResult = _workspace.source_analyzer.parse(tokens);
            var afterErrorCount = diagnostics.messages.Count(d => d.severity.is_error_level());
            if (afterErrorCount > beforeErrorCount)
            {
                var newErrors = diagnostics.messages
                    .Where(d => d.severity.is_error_level())
                    .Skip(beforeErrorCount);
                foreach (var err in newErrors)
                {
                    Console.WriteLine($"解析错误 [{sourceFile}]: {err.message}");
                }
            }

            if (parseResult.value is not null)
            {
                compilationUnits.Add(ensure_file_path(parseResult.value, normalizedPath));
            }
        }

        return compilationUnits;
    }

    private CompilationExecutionResult execute_pipeline(
        IReadOnlyList<CompilationUnit> compilationUnits,
        BuildPlan plan,
        string semanticErrorMessage)
    {
        var targetProfile = _canonical_target_registry.resolve(plan.canonical_triple);
        var stagedUnits = stage_compilation_units(compilationUnits, plan.canonical_triple);

        var cachedSemantics = try_get_cached_semantics(stagedUnits, plan);
        if (cachedSemantics?.has_errors == true)
        {
            ensure_no_semantic_errors(cachedSemantics, semanticErrorMessage);
        }

        var semantics = analyze(stagedUnits, plan);
        cache_semantics(stagedUnits, plan, semantics);
        ensure_no_semantic_errors(semantics, semanticErrorMessage);

        var hir = build_hir(stagedUnits, semantics, plan);
        ensure_no_semantic_errors(semantics, semanticErrorMessage);

        var mir = build_mir(hir, plan, targetProfile);
        var lir = build_lir(mir, plan);
        var artifactSet = _emitter.emit_from_profile(lir.module, targetProfile, create_compilation_options(plan));
        return new CompilationExecutionResult(semantics, hir, artifactSet, targetProfile);
    }

    /// <summary>
    ///     将构建计划中的目标附加选项映射为后端编译选项。
    /// </summary>
    private static CompilationOptions create_compilation_options(BuildPlan plan)
    {
        var optimizationLevel = Enum.IsDefined(typeof(OptimizationLevel), plan.optimization_level)
            ? (OptimizationLevel)plan.optimization_level
            : OptimizationLevel.basic;
        return new CompilationOptions
        {
            optimization_level = optimizationLevel,
            generate_source_map = plan.build_options?.source_map ?? false,
            generate_type_script_decls = plan.build_options?.type_script ?? false,
            generate_wat = plan.build_options?.wat ?? false,
            generate_msil = plan.build_options?.msil ?? false
        };
    }

    private static TypeCheckResult run_type_check(IReadOnlyList<CompilationUnit> compilationUnits)
    {
        var diagnostics = new List<TypeDiagnostic>();
        var typeChecker = new global::Nyar.Language.Valkyrie.TypeChecker.TypeChecker();
        foreach (var compilationUnit in compilationUnits)
        {
            var filePath = string.IsNullOrWhiteSpace(compilationUnit.file_path)
                ? null
                : compilationUnit.file_path;
            var result = typeChecker.check(compilationUnit, filePath);
            diagnostics.AddRange(result.diagnostics);
        }

        return new TypeCheckResult(diagnostics);
    }

    private IReadOnlyList<GreenLeafNode> lex_with_cache(string source, string filePath)
    {
        var normalizedPath = normalize_file_path(filePath);
        var contentHash = compute_content_hash(source);
        _source_content_hashes[normalizedPath] = contentHash;
        if (_cache is not null &&
            _cache.try_get_tokens(normalizedPath, contentHash, out var cachedTokens) &&
            cachedTokens is not null)
        {
            return deserialize_tokens(cachedTokens.token_data);
        }

        var beforeErrorCount = diagnostics.messages.Count(d => d.severity.is_error_level());
        var tokens = _workspace.source_analyzer.lex(source);
        var afterErrorCount = diagnostics.messages.Count(d => d.severity.is_error_level());
        if (_cache is not null && afterErrorCount == beforeErrorCount)
        {
            _cache.put_tokens(normalizedPath, contentHash, new TokenCacheEntry
            {
                content_hash = contentHash,
                token_data = serialize_tokens(tokens),
                created_at = DateTimeOffset.UtcNow
            });
        }

        return tokens;
    }

    private CompilationUnit[] stage_compilation_units(
        IReadOnlyList<CompilationUnit> compilationUnits,
        string canonicalTriple)
    {
        var stager = new MetaStager();
        var stagedUnits = new CompilationUnit[compilationUnits.Count];
        for (var i = 0; i < compilationUnits.Count; i++)
        {
            var unit = compilationUnits[i];
            var normalizedPath = normalize_file_path(unit.file_path);
            var contentHash = get_source_content_hash(unit);
            if (_cache is not null &&
                !string.IsNullOrWhiteSpace(normalizedPath) &&
                !string.IsNullOrWhiteSpace(contentHash) &&
                _cache.try_get_staging(normalizedPath, canonicalTriple, contentHash, out var stagedEntry) &&
                stagedEntry is not null)
            {
                var stagedUnit = parse_cached_staged_unit(stagedEntry.staged_token_data, normalizedPath);
                if (stagedUnit is not null)
                {
                    stagedUnits[i] = stagedUnit;
                    continue;
                }
            }

            var staged = ensure_file_path((CompilationUnit)stager.stage(unit, canonicalTriple), normalizedPath);
            stagedUnits[i] = staged;
            cache_staged_unit(normalizedPath, canonicalTriple, contentHash, staged);
        }

        return stagedUnits;
    }

    private void cache_staged_unit(
        string filePath,
        string canonicalTriple,
        string contentHash,
        CompilationUnit stagedUnit)
    {
        if (_cache is null ||
            string.IsNullOrWhiteSpace(filePath) ||
            string.IsNullOrWhiteSpace(contentHash))
        {
            return;
        }

        var formatted = ValkyrieFormatter.Format(stagedUnit);
        var lexerDiagnostics = new DiagnosticSink();
        var lexer = new Std.Data.Text.Valkyrie.Lexer.ValkyrieLexer(lexerDiagnostics);
        var stagedTokens = lexer.tokenize(formatted);
        if (lexerDiagnostics.has_errors)
        {
            return;
        }

        _cache.put_staging(filePath, canonicalTriple, contentHash, new StageCacheEntry
        {
            canonical_triple = canonicalTriple,
            content_hash = contentHash,
            staged_token_data = serialize_tokens(stagedTokens),
            created_at = DateTimeOffset.UtcNow
        });
    }

    private CompilationUnit? parse_cached_staged_unit(byte[] stagedTokenData, string filePath)
    {
        var tokens = deserialize_tokens(stagedTokenData);
        var parseResult = _workspace.source_analyzer.parse(tokens);
        return parseResult.value is null ? null : ensure_file_path(parseResult.value, filePath);
    }

    private SemanticModel? try_get_cached_semantics(
        IReadOnlyList<CompilationUnit> stagedUnits,
        BuildPlan plan)
    {
        return SemanticModelCacheStore.try_restore(_cache, stagedUnits, plan.canonical_triple);
    }

    private void cache_semantics(
        IReadOnlyList<CompilationUnit> stagedUnits,
        BuildPlan plan,
        SemanticModel semantics)
    {
        SemanticModelCacheStore.store(_cache, stagedUnits, plan.canonical_triple, semantics);
    }

    private string get_source_content_hash(CompilationUnit unit)
    {
        var filePath = normalize_file_path(unit.file_path);
        if (!string.IsNullOrWhiteSpace(filePath) &&
            _source_content_hashes.TryGetValue(filePath, out var cachedHash))
        {
            return cachedHash;
        }

        var formatted = ValkyrieFormatter.Format(unit);
        var hash = compute_content_hash(formatted);
        if (!string.IsNullOrWhiteSpace(filePath))
        {
            _source_content_hashes[filePath] = hash;
        }

        return hash;
    }

    private static CompilationUnit ensure_file_path(CompilationUnit unit, string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !string.IsNullOrWhiteSpace(unit.file_path))
        {
            return unit;
        }

        return unit with { file_path = filePath };
    }

    private static string compute_content_hash(string source)
    {
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(source)));
    }

    private static string normalize_file_path(string? filePath)
    {
        return string.IsNullOrWhiteSpace(filePath) ? string.Empty : Path.GetFullPath(filePath);
    }

    private static byte[] serialize_tokens(IReadOnlyList<GreenLeafNode> tokens)
    {
        var writer = new ByteBufferWriter(256);
        writer.write_i32_le(tokens.Count);
        foreach (var token in tokens)
        {
            writer.write_i32_le(token.kind.value);
            writer.write_i32_le(token.width);
            ByteBufferStringIO.write_nullable_string(ref writer, token.text);
        }

        return writer.to_array();
    }

    private static GreenLeafNode[] deserialize_tokens(ReadOnlySpan<byte> data)
    {
        var reader = new ByteBuffer(data);
        var count = reader.read_i32_le();
        var tokens = new GreenLeafNode[count];
        for (var i = 0; i < count; i++)
        {
            var kind = new NodeKind(reader.read_i32_le());
            var width = reader.read_i32_le();
            var text = ByteBufferStringIO.read_nullable_string(ref reader);
            tokens[i] = new GreenLeafNode(kind, width, text);
        }

        return tokens;
    }

    private void append_semantic_diagnostics(SemanticModel semantics)
    {
        foreach (var diagnostic in semantics.diagnostics)
        {
            var filePath = !string.IsNullOrWhiteSpace(diagnostic.source_span.file_path)
                ? diagnostic.source_span.file_path
                : diagnostic.file_path;
            diagnostics.report(
                diagnostic.span,
                diagnostic.message,
                diagnostic.level,
                DiagnosticSink.parse_legacy_code(diagnostic.code),
                filePath,
                diagnostic.source_span);
        }
    }

    private void ensure_no_semantic_errors(SemanticModel semantics, string message)
    {
        if (!semantics.has_errors)
        {
            return;
        }

        append_semantic_diagnostics(semantics);
        throw new InvalidOperationException(message);
    }

    private void register_default_backends()
    {
        _emitter.register_backend(new NyarVmBackend());
        _emitter.register_backend(new ClrBackend());
        _emitter.register_backend(new JvmBackend());
        _emitter.register_backend(new WasmBackend());
    }

    private static RunContract build_run_contract(
        HirModule hir,
        TargetProfile targetProfile,
        string? preferredLogicalEntry)
    {
        var entryPolicy = targetProfile.entry_policy;
        var logicalEntry = hir.functions
                               .FirstOrDefault(function =>
                                   function.is_logical_entry &&
                                   logical_entry_matches(function.name, preferredLogicalEntry))
                               ?.name
                           ?? entryPolicy.default_entry;
        var invocationShape = $"{hir.name}.{logicalEntry}(...)";
        var validationCommand = $"legion build --target {targetProfile.canonical_triple}";
        return new RunContract(logicalEntry, logicalEntry, invocationShape, validationCommand);
    }

    /// <summary>
    ///     判断逻辑入口是否匹配当前构建请求。
    /// </summary>
    private static bool logical_entry_matches(string callableName, string? preferredLogicalEntry)
    {
        if (string.IsNullOrWhiteSpace(preferredLogicalEntry))
        {
            return true;
        }

        return string.Equals(callableName, preferredLogicalEntry, StringComparison.Ordinal) ||
               callableName.EndsWith("." + preferredLogicalEntry, StringComparison.Ordinal);
    }

    private static string resolve_target_arch_tag(TargetProfile targetProfile)
    {
        return targetProfile.backend_family switch
        {
            TargetBackendFamily.jvm => "jvm",
            TargetBackendFamily.clr => "clr",
            TargetBackendFamily.wasm => targetProfile.canonical_triple.StartsWith("wasm64-", StringComparison.OrdinalIgnoreCase)
                ? "wasm64"
                : "wasm32",
            TargetBackendFamily.nyar_vm => "nyarvm",
            _ => string.Empty
        };
    }
}
