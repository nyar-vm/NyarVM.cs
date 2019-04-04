using Nyar.Assembler;
using Nyar.Language.Valkyrie.Compiler.Packaging;
using Nyar.Language.Valkyrie.Compiler.Pipeline;
using Nyar.Types.Targets;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Nyar.Language.Valkyrie.Compiler.Targets;

/// <summary>
///     将 `LIR/GenerateModule` 发射为目标交付产物
/// </summary>
public sealed class TargetArtifactEmitter
{
    private readonly BackendSelector _backend_selector;
    private readonly ITargetPackager _packager;

    /// <summary>
    ///     初始化发射器
    /// </summary>
    public TargetArtifactEmitter()
    {
        _backend_selector = new BackendSelector([]);
        _packager = new DefaultTargetPackager();
    }

    /// <summary>
    ///     注册后端
    /// </summary>
    /// <param name="backend">代码生成后端</param>
    public void register_backend(ICodeGenBackend backend)
    {
        _backend_selector.register_backend(backend);
    }

    /// <summary>
    ///     将 `GenerateModule` 按编译目标发射为 `ArtifactSet`
    /// </summary>
    /// <param name="module">LIR 模块</param>
    /// <param name="target">编译目标</param>
    /// <param name="options">编译选项</param>
    /// <returns>产物集</returns>
    /// <exception cref="InvalidOperationException">未找到目标后端，或后端校验失败</exception>
    public ArtifactSet emit(GenerateModule module, CompilationTarget target, CompilationOptions? options = null)
    {
        var backend = select_and_validate_backend(module, target);

        // 收集所有函数类型的导出，每个导出对应一个 [main] 函数
        var functionExports = module.exports
            .Where(e => e.kind == GenerateExportKind.function)
            .ToList();

        // 无多入口场景：走原有单编译路径
        if (functionExports.Count <= 1)
        {
            return emit_single(module, target, backend, options);
        }

        // 多入口场景：每个 [main] 函数独立编译为一个可执行程序集
        return emit_multi_entry(module, target, backend, functionExports, options);
    }

    /// <summary>
    ///     选择后端并校验模块
    /// </summary>
    private ICodeGenBackend select_and_validate_backend(GenerateModule module, CompilationTarget target)
    {
        var backend = _backend_selector.select_backend(target.arch);
        if (backend is null)
        {
            throw new InvalidOperationException($"未找到目标后端：{target.arch}");
        }

        if (!backend.validate(module, out var diagnostics))
        {
            var reason = diagnostics.Count == 0
                ? "后端校验失败"
                : string.Join("; ", diagnostics.Select(diagnostic => diagnostic.message));
            throw new InvalidOperationException(reason);
        }

        return backend;
    }

    /// <summary>
    ///     单入口编译：原有路径
    /// </summary>
    private ArtifactSet emit_single(
        GenerateModule module,
        CompilationTarget target,
        ICodeGenBackend backend,
        CompilationOptions? options)
    {
        var stopwatch = Stopwatch.StartNew();
        Console.WriteLine($"[TargetArtifactEmitter] emit_single start: backend={backend.name}, module={module.name}, target={target.arch}");
        var effectiveOptions = options ?? new CompilationOptions();
        effectiveOptions.target = target;
        var generated = backend.compile(module, effectiveOptions);
        Console.WriteLine($"[TargetArtifactEmitter] backend.compile completed in {stopwatch.ElapsedMilliseconds} ms");

        stopwatch.Restart();
        var targetProfile = build_target_profile(target);
        var packaged = _packager.package(module.name, generated, targetProfile);
        Console.WriteLine($"[TargetArtifactEmitter] package completed in {stopwatch.ElapsedMilliseconds} ms");
        return packaged;
    }

    /// <summary>
    ///     多入口编译：每个 [main] 函数先裁剪再独立调用后端，产物合并到一个 ArtifactSet 中
    /// </summary>
    private ArtifactSet emit_multi_entry(
        GenerateModule module,
        CompilationTarget target,
        ICodeGenBackend backend,
        IReadOnlyCollection<GenerateModuleExport> functionExports,
        CompilationOptions? options)
    {
        var allPrimaryArtifacts = new List<CompilerArtifact>();
        var allSidecarArtifacts = new List<CompilerArtifact>();
        var targetProfile = build_target_profile(target);

        var exportsList = functionExports.ToList();
        var results = new (CompilerArtifact primary, List<CompilerArtifact> sidecars)?[exportsList.Count];

        // 预计算可达性上下文，避免每个线程重复扫描模块指令流
        var slicerContext = new GenerateModuleSlicer.Context(module);

        Parallel.For(0, exportsList.Count, i =>
        {
            var export = exportsList[i];
            if (export.function_index < 0 || export.function_index >= module.functions.Count)
            {
                return;
            }

            var funcName = module.functions[export.function_index].name;
            var entryOptions = options is null
                ? new CompilationOptions { target = target, entry_function_name = funcName }
                : new CompilationOptions
                {
                    target = target,
                    entry_function_name = funcName,
                    optimization_level = options.optimization_level,
                    generate_wat = options.generate_wat,
                    generate_source_map = options.generate_source_map,
                    generate_type_script_decls = options.generate_type_script_decls,
                    generate_msil = options.generate_msil
                };

            // 按入口裁剪子模块，减少后端重复编译的函数数量
            var slicedModule = slicerContext.slice_by_entry(funcName);
            var generated = backend.compile(slicedModule, entryOptions);
            var artifactSet = _packager.package(module.name, generated, targetProfile);

            var sidecars = new List<CompilerArtifact>();
            sidecars.AddRange(artifactSet.sidecar_artifacts);
            sidecars.AddRange(artifactSet.debug_artifacts);
            results[i] = (artifactSet.primary_artifact, sidecars);
        });

        foreach (var res in results)
        {
            if (res == null) continue;
            allPrimaryArtifacts.Add(res.Value.primary);
            allSidecarArtifacts.AddRange(res.Value.sidecars);
        }

        // 第一个入口的产物作为主产物，其余入口的 exe/dll 放入 sidecar
        var primary = allPrimaryArtifacts[0];
        var sidecars = new List<CompilerArtifact>();

        // 第一个入口的附属产物
        foreach (var sidecar in allSidecarArtifacts)
        {
            // 去掉路径前缀，只保留文件名（sidecar 产物名形如 "legion.runtimeconfig.json"）
            sidecars.Add(sidecar);
        }

        // 其余入口的主产物也放入 sidecar（如 vcc.exe、voa.exe）
        for (var i = 1; i < allPrimaryArtifacts.Count; i++)
        {
            sidecars.Add(allPrimaryArtifacts[i]);
        }

        return new ArtifactSet(primary, sidecars);
    }

    /// <summary>
    ///     从 CompilationTarget 构建 TargetProfile
    /// </summary>
    private static TargetProfile build_target_profile(CompilationTarget target)
    {
        var canonicalTriple = CanonicalTarget.from_compilation_target(target).ToString();
        if (string.IsNullOrWhiteSpace(canonicalTriple))
        {
            throw new InvalidOperationException("无法为当前目标解析 `CanonicalTriple`。");
        }

        return CanonicalTarget.from_compilation_target(target).to_profile();
    }

    /// <summary>
    ///     从 `TargetProfile` 推导 `CompilationTarget` 并发射产物
    /// </summary>
    /// <param name="module">LIR 模块</param>
    /// <param name="targetProfile">目标配置</param>
    /// <param name="options">编译选项</param>
    /// <returns>产物。</returns>
    public ArtifactSet emit_from_profile(GenerateModule module, TargetProfile targetProfile,
        CompilationOptions? options = null)
    {
        var compilationTarget = CanonicalTarget.parse(targetProfile.canonical_triple).to_compilation_target();
        return emit(module, compilationTarget, options);
    }
}
