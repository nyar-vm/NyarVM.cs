namespace Nyar.Language.Valkyrie.Compiler.Mir;

/// <summary>
///     MIR 优化管线的统计数据
/// </summary>
/// <param name="NodeCountBefore">优化前的节点数量</param>
/// <param name="NodeCountAfter">优化后的节点数量</param>
/// <param name="ElapsedMs">优化耗时（毫秒）</param>
public sealed record OptimizationStats(
    int node_count_before,
    int node_count_after,
    long elapsed_ms
);