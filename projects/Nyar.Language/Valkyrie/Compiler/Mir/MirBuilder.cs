using Nyar.Language.Valkyrie.Compiler.Hir;
using Std.Data.Text.Diagnostics;

namespace Nyar.Language.Valkyrie.Compiler.Mir;

/// <summary>
///     MIR 构建入口，委托降级到 <see cref="HirToMirLowerer" /> 并运行优化管线
/// </summary>
public sealed class MirBuilder
{
    private readonly HirToMirLowerer _lowerer;
    private readonly MirOptimizationPipeline _pipeline;

    /// <summary>
    ///     初始化 MIR 构建器
    /// </summary>
    /// <param name="diagnostics">可选的诊断收集器</param>
    public MirBuilder(DiagnosticSink? diagnostics = null)
    {
        _lowerer = new HirToMirLowerer(diagnostics);
        _pipeline = new MirOptimizationPipeline();
    }

    /// <summary>
    ///     将 HIR 模块构建为优化后的 MIR 模块
    /// </summary>
    /// <param name="hir">HIR 模块</param>
    /// <param name="targetArchTag">目标架构标签</param>
    /// <returns>优化后的 MIR 模块</returns>
    public MirModule build(HirModule hir, string targetArchTag, string? preferredLogicalEntry = null)
    {
        var mir = _lowerer.lower(hir, targetArchTag, preferredLogicalEntry);
        // TODO: 临时禁用优化管线以诊断空函数体问题
        // if (mir.root.HasValue)
        // {
        //     mir.root = _pipeline.run(mir.graph, mir.root.Value);
        // }

        return mir;
    }
}
