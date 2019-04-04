using Nyar.Dialect;
using Nyar.Dialect.Neural;
using Nyar.EGraph;
using Nyar.IR.Extractor;
using Nyar.IR.Intent;
using Nyar.ObjectAlgebra;
using Nyar.Types.Targets;

namespace Std.DL.Compiler.NyarBridge;

/// <summary>
///     Nyar EGraph 优化管线适配器
///     使用 DialectRegistry 注册方言规则并执行饱和优化，
///     遵循"Nyar 一切分析与优化"的架构原则
/// </summary>
public static class TensorIkunOptimizer
{
    /// <summary>
    ///     使用 Nyar EGraph 优化管线对计算图进行优化
    /// </summary>
    /// <param name="egraph">等价图</param>
    /// <param name="rootId">根节点 ID</param>
    /// <param name="maxIterations">最大饱和迭代次数</param>
    /// <returns>优化结果，包含优化后的 EGraph 和新的根节点 ID</returns>
    public static OptimizationResult Optimize(EGraph<AlgebraNode> egraph, Id rootId, int maxIterations = 10)
    {
        var registry = new DialectRegistry();
        registry.register(new TensorDialect());
        registry.register_all_rules(egraph, maxIterations);

        var optimizedRoot = egraph.union_find.find(rootId);

        var costModel = registry.build_cost_model();
        var extractor = new Extractor(egraph, costModel);
        var bestProgram = extractor.Extract(optimizedRoot);

        return new OptimizationResult(
            egraph,
            optimizedRoot,
            bestProgram,
            maxIterations,
            0);
    }

    /// <summary>
    ///     使用目标感知优化管线进行优化
    /// </summary>
    /// <param name="egraph">等价图</param>
    /// <param name="rootId">根节点 ID</param>
    /// <param name="target">编译目标</param>
    /// <returns>优化结果</returns>
    public static OptimizationResult OptimizeForTarget(EGraph<AlgebraNode> egraph, Id rootId, CompilationTarget target)
    {
        var registry = new DialectRegistry();
        registry.register(new TensorDialect());
        registry.register_all_rules(egraph);

        var optimizedRoot = egraph.union_find.find(rootId);

        var costModel = registry.build_cost_model();
        var extractor = new Extractor(egraph, costModel);
        var bestProgram = extractor.Extract(optimizedRoot);

        return new OptimizationResult(
            egraph,
            optimizedRoot,
            bestProgram,
            0,
            0);
    }

    /// <summary>
    ///     统计优化效果
    /// </summary>
    /// <param name="original">原始节点数</param>
    /// <param name="optimized">优化后节点数</param>
    /// <returns>优化率（0-1），如 0.3 表示减少了 30% 的操作数</returns>
    public static float GetOptimizationRate(int original, int optimized)
    {
        if (original == 0) return 0.0f;

        return 1.0f - (float)optimized / original;
    }
}

/// <summary>
///     EGraph 优化结果
/// </summary>
/// <param name="Egraph">优化后的 EGraph</param>
/// <param name="RootId">优化后的根节点 ID</param>
/// <param name="BestProgram">提取的最优程序（Oa）</param>
/// <param name="Iterations">饱和迭代次数</param>
/// <param name="UnionsApplied">应用的 Union 操作数</param>
public sealed record OptimizationResult(
    EGraph<AlgebraNode> Egraph,
    Id RootId,
    AlgebraNode BestProgram,
    int Iterations,
    int UnionsApplied);