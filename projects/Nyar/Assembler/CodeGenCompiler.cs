using Nyar.Types.Targets;
using Std.Data.Text.Diagnostics;

namespace Nyar.Assembler;

/// <summary>
///     代码生成编排入口。
///     负责基于目标架构选择后端、执行验证并返回强类型输出。
/// </summary>
public sealed class CodeGenCompiler
{
    private readonly BackendSelector _backend_selector;

    /// <summary>
    ///     使用后端集合创建编排器。
    /// </summary>
    /// <param name="backends">可用后端。</param>
    public CodeGenCompiler(IEnumerable<ICodeGenBackend> backends)
        : this(new BackendSelector(backends))
    {
    }

    /// <summary>
    ///     使用现有后端选择器创建编排器。
    /// </summary>
    /// <param name="backendSelector">后端选择器。</param>
    public CodeGenCompiler(BackendSelector backendSelector)
    {
        ArgumentNullException.ThrowIfNull(backendSelector);
        _backend_selector = backendSelector;
    }

    /// <summary>
    ///     为给定输出类型选择强类型后端。
    /// </summary>
    /// <typeparam name="TOutput">目标输出类型。</typeparam>
    /// <param name="options">编译选项。</param>
    /// <returns>匹配到的强类型后端；如果不存在则返回 null。</returns>
    public ICodeGenBackend<TOutput>? select_backend<TOutput>(CompilationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return _backend_selector.select_backend<TOutput>(resolve_target_arch(options));
    }

    /// <summary>
    ///     执行强类型后端编译。
    /// </summary>
    /// <typeparam name="TOutput">目标输出类型。</typeparam>
    /// <param name="module">待编译模块。</param>
    /// <param name="options">编译选项。</param>
    /// <returns>强类型编译结果。</returns>
    public BackendCompilationResult<TOutput> compile<TOutput>(GenerateModule module, CompilationOptions options)
    {
        ArgumentNullException.ThrowIfNull(module);
        ArgumentNullException.ThrowIfNull(options);

        var targetArch = resolve_target_arch(options);
        var backend = _backend_selector.select_backend<TOutput>(targetArch);

        if (backend is null)
            return new BackendCompilationResult<TOutput>
            {
                target_arch = targetArch,
                diagnostics =
                [
                    new Diagnostic(
                        default,
                        $"未找到支持目标架构 `{targetArch}` 且输出类型为 `{typeof(TOutput).Name}` 的代码生成后端。",
                        DiagnosticSeverity.error)
                ]
            };

        if (!backend.validate(module, out var diagnostics))
            return new BackendCompilationResult<TOutput>
            {
                target_arch = targetArch,
                backend = backend,
                diagnostics = [.. diagnostics]
            };

        var capturedDiagnostics = diagnostics.ToArray();
        if (contains_errors(capturedDiagnostics))
            return new BackendCompilationResult<TOutput>
            {
                target_arch = targetArch,
                backend = backend,
                diagnostics = capturedDiagnostics
            };

        return new BackendCompilationResult<TOutput>
        {
            target_arch = targetArch,
            backend = backend,
            output = backend.compile(module, options),
            diagnostics = capturedDiagnostics
        };
    }

    /// <summary>
    ///     尝试执行强类型后端编译。
    /// </summary>
    /// <typeparam name="TOutput">目标输出类型。</typeparam>
    /// <param name="module">待编译模块。</param>
    /// <param name="options">编译选项。</param>
    /// <param name="result">编译结果。</param>
    /// <returns>是否成功得到强类型输出。</returns>
    public bool try_compile<TOutput>(GenerateModule module, CompilationOptions options,
        out BackendCompilationResult<TOutput> result)
    {
        result = compile<TOutput>(module, options);
        return result.succeeded;
    }

    private static TargetArch resolve_target_arch(CompilationOptions options)
    {
        return options.target?.arch ?? TargetArch.nyar_vm;
    }

    private static bool contains_errors(IReadOnlyList<Diagnostic> diagnostics)
    {
        foreach (var diagnostic in diagnostics)
            if (diagnostic.severity.is_error_level())
                return true;

        return false;
    }
}