namespace Nyar.PackageManager.Build;

/// <summary>
///     构建编排器。
///     负责：构建计划管理、缓存命中检查、串行/并发调度、日志聚合。
/// </summary>
public sealed class BuildOrchestrator
{
    private readonly BuildController _build_controller;
    private readonly BuildCache _cache;

    /// <summary>
    ///     初始化构建编排器
    /// </summary>
    /// <param name="outputBaseDirectory">产物输出根目录</param>
    /// <param name="compiler">编译器实例</param>
    /// <param name="cacheDirectory">缓存目录</param>
    public BuildOrchestrator(string outputBaseDirectory = "dist", ICompiler compiler = null!,
        string? cacheDirectory = null)
    {
        _cache = new BuildCache(cacheDirectory ?? Path.Combine(outputBaseDirectory, ".legion-cache"));
        _build_controller = new BuildController(outputBaseDirectory, compiler);
        logger = new BuildLogger();
    }

    /// <summary>
    ///     构建日志器
    /// </summary>
    public BuildLogger logger { get; }

    /// <summary>
    ///     执行单个构建
    /// </summary>
    /// <param name="source">源码文本</param>
    /// <param name="plan">构建计划</param>
    /// <returns>构建结果</returns>
    public BuildResult BuildOne(string source, IBuildPlan plan)
    {
        var cacheKey = BuildCacheKey.from_plan(plan, source);

        if (_cache.try_get(cacheKey, out var cachedResult))
        {
            logger.log_info(
                $"→ 缓存命中：{plan.module_name} ({plan.canonical_triple})");
            return cachedResult;
        }

        logger.log_stage_start(BuildStage.lex, plan.module_name);

        var result = _build_controller.build(source, plan);

        if (result.success)
        {
            logger.log_success(result);
            _cache.store(cacheKey, result);
        }
        else
        {
            logger.log_failure(result);
        }

        return result;
    }

    /// <summary>
    ///     执行多文件构建。
    /// </summary>
    /// <param name="sourceFiles">源码文件列表</param>
    /// <param name="plan">构建计划</param>
    /// <returns>构建结果</returns>
    public BuildResult build_files(IReadOnlyList<string> sourceFiles, IBuildPlan plan)
    {
        BuildCacheKey? cacheKey = null;
        try
        {
            cacheKey = BuildCacheKey.from_plan(plan, sourceFiles);
            if (_cache.try_get(cacheKey, out var cachedResult))
            {
                logger.log_info(
                    $"→ 缓存命中：{plan.module_name} ({plan.canonical_triple})");
                return cachedResult;
            }
        }
        catch (IOException)
        {
            // 读取源码失败时退化为无缓存构建，让下游返回真实失败原因。
        }

        logger.log_stage_start(BuildStage.lex, plan.module_name);

        var result = _build_controller.build_files(sourceFiles, plan);

        if (result.success)
        {
            logger.log_success(result);
            if (cacheKey is not null) _cache.store(cacheKey, result);
        }
        else
        {
            logger.log_failure(result);
        }

        return result;
    }

    /// <summary>
    ///     显示摘要
    /// </summary>
    /// <param name="results">构建结果列表</param>
    public void show_summary(IReadOnlyList<BuildResult> results)
    {
        var succeeded = results.Count(r => r.success);
        var failed = results.Count - succeeded;

        logger.log_info("");
        logger.log_info($"=== 构建摘要：{succeeded} 成功，{failed} 失败 ===");

        foreach (var result in results)
            if (!result.success)
                logger.log_failure(result);
    }
}