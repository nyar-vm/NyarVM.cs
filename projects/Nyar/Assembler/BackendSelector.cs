using Nyar.Types.Targets;

namespace Nyar.Assembler;

/// <summary>
///     元编译后端选择器。
///     只依赖根包定义的后端元数据，不感知任何具体平台实现。
/// </summary>
public sealed class BackendSelector
{
    private const int _exact_match_score = 100;
    private const int _compatible_match_score = 50;
    private const int _generic_match_score = 10;

    private readonly List<ICodeGenBackend> _backends;

    /// <summary>
    ///     创建后端选择器
    /// </summary>
    /// <param name="backends">可用后端列表。</param>
    public BackendSelector(IEnumerable<ICodeGenBackend> backends)
    {
        _backends = [.. backends];
    }

    /// <summary>
    ///     根据架构选择最佳后端
    /// </summary>
    /// <param name="arch">目标架构。</param>
    /// <returns>最匹配的后端；如果不存在则返回 null。</returns>
    public ICodeGenBackend? select_backend(TargetArch arch)
    {
        ICodeGenBackend? bestBackend = null;
        var bestScore = -1;

        foreach (var backend in _backends)
        {
            var score = calculate_score(backend, arch);
            if (score > bestScore)
            {
                bestScore = score;
                bestBackend = backend;
            }
        }

        return bestBackend;
    }

    /// <summary>
    ///     根据架构选择指定输出类型的最佳后端。
    /// </summary>
    /// <typeparam name="TOutput">目标输出类型。</typeparam>
    /// <param name="arch">目标架构。</param>
    /// <returns>最匹配的强类型后端；如果不存在则返回 null。</returns>
    public ICodeGenBackend<TOutput>? select_backend<TOutput>(TargetArch arch)
    {
        ICodeGenBackend<TOutput>? bestBackend = null;
        var bestScore = -1;

        foreach (var backend in _backends.OfType<ICodeGenBackend<TOutput>>())
        {
            var score = calculate_score(backend, arch);
            if (score > bestScore)
            {
                bestScore = score;
                bestBackend = backend;
            }
        }

        return bestBackend;
    }

    /// <summary>
    ///     尝试根据架构选择指定输出类型的最佳后端。
    /// </summary>
    /// <typeparam name="TOutput">目标输出类型。</typeparam>
    /// <param name="arch">目标架构。</param>
    /// <param name="backend">匹配到的后端。</param>
    /// <returns>是否成功选择到匹配的强类型后端。</returns>
    public bool try_select_backend<TOutput>(TargetArch arch, out ICodeGenBackend<TOutput>? backend)
    {
        backend = select_backend<TOutput>(arch);
        return backend is not null;
    }

    /// <summary>
    ///     注册后端
    /// </summary>
    /// <param name="backend">后端实例。</param>
    public void register_backend(ICodeGenBackend backend)
    {
        _backends.Add(backend);
    }

    /// <summary>
    ///     注册强类型后端。
    /// </summary>
    /// <typeparam name="TOutput">目标输出类型。</typeparam>
    /// <param name="backend">后端实例。</param>
    public void register_backend<TOutput>(ICodeGenBackend<TOutput> backend)
    {
        register_backend((ICodeGenBackend)backend);
    }

    /// <summary>
    ///     获取所有已注册后端
    /// </summary>
    /// <returns>后端列表。</returns>
    public IReadOnlyList<ICodeGenBackend> get_all_backends()
    {
        return _backends.AsReadOnly();
    }

    /// <summary>
    ///     获取所有已注册的指定输出类型后端。
    /// </summary>
    /// <typeparam name="TOutput">目标输出类型。</typeparam>
    /// <returns>强类型后端列表。</returns>
    public IReadOnlyList<ICodeGenBackend<TOutput>> get_backends<TOutput>()
    {
        return [.. _backends.OfType<ICodeGenBackend<TOutput>>()];
    }

    /// <summary>
    ///     计算目标匹配分数
    /// </summary>
    /// <param name="backend">候选后端。</param>
    /// <param name="arch">目标架构。</param>
    /// <returns>匹配分数，-1 表示不兼容。</returns>
    private static int calculate_score(ICodeGenBackend backend, TargetArch arch)
    {
        if (!backend.supported_archs.Contains(arch)) return -1;

        if (backend.supported_archs.Count == 1) return _exact_match_score;

        if (backend.supported_archs.Count <= 3) return _compatible_match_score;

        return _generic_match_score;
    }
}