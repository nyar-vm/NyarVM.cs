using System.Collections.Concurrent;
using Nyar.Language.Valkyrie.Compiler.Pipeline;

namespace Legion.CLI.Compiler;

/// <summary>
///     并行构建执行器。
///     对多个 target 的 <c>CompilationContext</c> 执行并行编译，收集结果。
///     每个 target 的编译互不依赖，可以安全地并行执行。
/// </summary>
public sealed class ParallelBuildRunner
{
    /// <summary>
    ///     并行构建的最大并发度。
    /// </summary>
    public int max_parallelism { get; set; } = Environment.ProcessorCount;

    /// <summary>
    ///     并行编译多个 target。
    /// </summary>
    /// <param name="contexts">编译上下文列表</param>
    /// <param name="verbose">是否为 verbose 模式</param>
    /// <returns>每个 target 的构建结果</returns>
    public IReadOnlyList<BuildResult> run_all(
        IReadOnlyList<CompilationContext> contexts,
        bool verbose)
    {
        if (contexts.Count == 0) return [];

        if (contexts.Count == 1) return [run_single(contexts[0], verbose)];

        var results = new ConcurrentBag<BuildResult>();
        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = Math.Min(max_parallelism, contexts.Count)
        };

        Parallel.ForEach(contexts, parallelOptions, context =>
        {
            var result = run_single(context, verbose);
            results.Add(result);
        });

        return [.. results];
    }

    /// <summary>
    ///     执行单个 target 的编译。
    /// </summary>
    private static BuildResult run_single(CompilationContext context, bool verbose)
    {
        var compiler = new LegionCompiler();
        var result = compiler.build(context);
        return new BuildResult(context.canonical_triple, result);
    }
}