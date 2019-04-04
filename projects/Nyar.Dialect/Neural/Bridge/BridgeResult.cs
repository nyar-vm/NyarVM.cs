using Nyar.EGraph;
using Nyar.IR.Intent;

namespace Nyar.Dialect.Neural.Bridge;

/// <summary>
///     桥接转换结果
/// </summary>
public sealed class BridgeResult
{
    /// <summary>
    ///     创建桥接转换结果
    /// </summary>
    public BridgeResult(EGraph<AlgebraNode> egraph, IReadOnlyDictionary<string, Id> nameToId, IReadOnlyList<Id> outputIds,
        BridgeStats stats)
    {
        EGraph = egraph;
        NameToId = nameToId;
        OutputIds = outputIds;
        Stats = stats;
    }

    /// <summary>
    ///     生成的 EGraph
    /// </summary>
    public EGraph<AlgebraNode> EGraph { get; }

    /// <summary>
    ///     操作名到 EGraph Id 的映射
    /// </summary>
    public IReadOnlyDictionary<string, Id> NameToId { get; }

    /// <summary>
    ///     输出 ID 列表
    /// </summary>
    public IReadOnlyList<Id> OutputIds { get; }

    /// <summary>
    ///     转换统计信息
    /// </summary>
    public BridgeStats Stats { get; }
}