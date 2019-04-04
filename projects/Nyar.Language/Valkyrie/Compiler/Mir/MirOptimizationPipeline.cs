using Nyar.Dialect;
using Nyar.Dialect.Standard;
using Nyar.EGraph;
using Nyar.IR.Extractor;
using Nyar.IR.Intent;
using Nyar.ObjectAlgebra;
using Nyar.Optimizer.CostModels;

namespace Nyar.Language.Valkyrie.Compiler.Mir;

/// <summary>
///     ⛔ MIR 优化管线（遗留），使用 DialectRegistry + EGraph&lt;AlgebraNode&gt; 执行饱和优化。
///     已迁移至 OA 开放模型：请使用 EGraph&lt;ENode&gt; + IDialectRuntime 替代。
///     详见 .trae/specs/oa-full-pipeline/spec.md。
/// </summary>
[Obsolete("MirOptimizationPipeline 依赖 EGraph<AlgebraNode> 封闭节点宇宙。请使用 EGraph<ENode> + IDialectRuntime 开放模型")]
public sealed class MirOptimizationPipeline
{
    private readonly DialectRegistry _registry;
    private readonly ICostModel _costModel;

    /// <summary>
    ///     创建优化管线，使用 Standard 方言和默认成本模型
    /// </summary>
    public MirOptimizationPipeline()
    {
        _registry = new DialectRegistry();
        _registry.register(new StandardDialect());
        _costModel = _registry.build_cost_model();
    }

    /// <summary>
    ///     运行优化管线，在 EGraph 上原地执行饱和优化并提取最优程序
    /// </summary>
    /// <param name="graph">EGraph 图</param>
    /// <param name="root">根节点 Id</param>
    /// <returns>优化后的根节点 Id（经并查集规范化）</returns>
    public Id run(EGraph<AlgebraNode> graph, Id root)
    {
        _registry.register_all_rules(graph);
        var optimizedRoot = graph.union_find.find(root);
        var extractor = new Extractor(graph, _costModel);
        extractor.extract(optimizedRoot);
        return optimizedRoot;
    }
}