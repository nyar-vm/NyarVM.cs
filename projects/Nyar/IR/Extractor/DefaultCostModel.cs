using Nyar.Dialect.Core.Nodes;
using Nyar.IR.Intent;
using Nyar.Optimizer.CostModels;
using Nyar.Types;
using Range = Nyar.Dialect.Core.Nodes.Range;

namespace Nyar.IR.Extractor;

/// <summary>
///     默认成本模型，为各类 Oa 节点分配基于延迟的简单成本。
///     当没有方言提供成本钩子时，作为兜底成本模型使用。
/// </summary>
public sealed class DefaultCostModel : ICostModel
{
    private readonly CostWeights _weights;

    /// <summary>
    ///     使用默认的均衡权重配置构造成本模型实例。
    /// </summary>
    public DefaultCostModel() : this(CostWeights.@default)
    {
    }

    /// <summary>
    ///     使用指定的自定义权重配置构造成本模型实例。
    /// </summary>
    /// <param name="weights">用于计算综合成本分数的权重配置</param>
    public DefaultCostModel(CostWeights weights)
    {
        _weights = weights;
    }

    /// <inheritdoc />
    public CostVector node_cost(AlgebraNode node)
    {
        return node switch
        {
            Literal<object?> nullLit => c(0, 0),

            VarDecl => c(2, 8),
            Attrib => c(1, 4),
            TypeRef => c(1, 4),
            TypeParam => c(1, 4),
            TypeParamConstraint => c(1, 4),
            UnionType => c(1, 4),
            IntersectType => c(1, 4),
            FuncType => c(1, 4),
            ListType => c(1, 4),
            ArrType => c(1, 4),
            TypeAlias => c(1, 4),
            Sym => c(1, 8),
            Import => c(0, 8),
            Export => c(0, 8),
            Mod => c(0, 0),

            // 函数与闭包 - OA 强类型节点
            Lambda => c(3, 16),
            Apply => c(5, 8),
            Closure => c(4, 16),

            // 控制流扩展 - OA 强类型节点
            Choice => c(3, 4),
            StateUp => c(2, 8),
            Lifecycle => c(2, 4),
            Meta => c(1, 4),
            Trap => c(2, 4),
            Compose => c(2, 0),
            Resume => c(2, 4),

            // 数据结构 - OA 强类型节点
            ArrayLit => c(3, 16),
            Range => c(2, 8),
            Pair => c(2, 8),
            Table => c(3, 16),
            GetOrdinalIdx => c(3, 8),
            SetOrdinalIdx => c(5, 8),
            GetOffsetIdx => c(3, 8),
            SetOffsetIdx => c(5, 8),

            // 对象模型 - OA 强类型节点
            Inherit => c(2, 4),
            ClassDef => c(5, 16),

            // 模式匹配 - OA 强类型节点
            Match => c(5, 8),
            MatchArm => c(3, 4),
            Catch => c(3, 8),
            CatchArm => c(2, 4),
            LitPattern => c(1, 4),
            VarPattern => c(1, 4),
            CtorPattern => c(2, 4),
            ObjPatField => c(2, 4),
            ObjPattern => c(3, 4),
            WildPattern => c(0, 0),
            OrPattern => c(2, 4),
            GuardPattern => c(2, 4),
            TypeTestPattern => c(1, 4),

            // 特性组 - OA 强类型节点
            AttrGroup => c(1, 4),

            // 类型转换 - OA 强类型节点
            Cast => c(2, 4),

            // 循环降级 - OA 强类型节点
            LoopFilter => c(6, 8),

            _ => c(3, 8)
        };
    }

    /// <inheritdoc />
    public int compare(CostVector a, CostVector b)
    {
        return _weights.compare(a, b);
    }

    /// <summary>
    ///     根据延迟与内存占用估算全维度成本向量。
    ///     功耗约为延迟的 10%，硬件面积保持为零（软件目标）。
    /// </summary>
    private static CostVector c(double latency, long memory)
    {
        return new CostVector(latency, latency * 0.1, memory, 0);
    }
}
