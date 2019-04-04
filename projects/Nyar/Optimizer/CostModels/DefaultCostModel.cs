using Nyar.Dialect.Core.Nodes;
using Nyar.IR.Intent;
using Nyar.Types;
using Range = Nyar.Dialect.Core.Nodes.Range;
using Tuple = Nyar.Dialect.Core.Nodes.Tuple;

namespace Nyar.Optimizer.CostModels;

/// <summary>
///     默认成本模型，为各类 Oa 节点分配基于延迟的简单成本。
///     同时覆盖 OA 生成的强类型节点（Core 方言），避免回退到默认分支。
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
            // OA Core 方言强类型节点
            // 字面量
            Literal<long> or
                Literal<double> or
                Literal<bool> or
                Literal<string> => C(1, 8),

            // 算术运算
            Add => C(2, 8),
            Sub => C(2, 8),
            Mul => C(3, 8),
            Div or Rem => C(10, 8),
            Neg => C(2, 4),

            // 逻辑运算
            Not => C(2, 4),
            And or Or => C(2, 8),

            // 比较
            Cmp => C(2, 4),

            // 元组
            Tuple => C(2, 8),
            Project => C(1, 4),

            // 内存操作
            Alloc => C(10, 16),
            Free => C(2, 4),
            Load => C(5, 8),
            Store => C(5, 8),

            // 控制流
            Label => C(0, 0),
            Branch => C(1, 4),
            Phi => C(1, 0),
            Call => C(10, 8),
            Ret => C(1, 4),

            // 效应系统
            Perform => C(50, 8),
            Handle => C(5, 8),

            // 声明与符号 - OA 强类型节点
            VarDecl => C(2, 8),
            Attrib => C(1, 4),
            TypeRef => C(1, 4),
            TypeParam => C(1, 4),
            TypeParamConstraint => C(1, 4),
            UnionType => C(1, 4),
            IntersectType => C(1, 4),
            FuncType => C(1, 4),
            ListType => C(1, 4),
            ArrType => C(1, 4),
            TypeAlias => C(1, 4),
            Sym => C(1, 8),
            Import => C(0, 8),
            Export => C(0, 8),
            Mod => C(0, 0),

            // 函数与闭包 - OA 强类型节点
            Lambda => C(3, 16),
            Apply => C(5, 8),
            Closure => C(4, 16),

            // 控制流扩展 - OA 强类型节点
            Choice => C(3, 4),
            StateUp => C(2, 8),
            Lifecycle => C(2, 4),
            Meta => C(1, 4),
            Trap => C(2, 4),
            Compose => C(2, 0),
            Resume => C(2, 4),

            // 数据结构 - OA 强类型节点
            ArrayLit => C(3, 16),
            Range => C(2, 8),
            Pair => C(2, 8),
            Table => C(3, 16),
            GetOrdinalIdx => C(3, 8),
            SetOrdinalIdx => C(5, 8),
            GetOffsetIdx => C(3, 8),
            SetOffsetIdx => C(5, 8),

            // 对象模型 - OA 强类型节点
            Inherit => C(2, 4),
            ClassDef => C(5, 16),

            // 模式匹配 - OA 强类型节点
            Match => C(5, 8),
            MatchArm => C(3, 4),
            Catch => C(3, 8),
            CatchArm => C(2, 4),
            LitPattern => C(1, 4),
            VarPattern => C(1, 4),
            CtorPattern => C(2, 4),
            ObjPatField => C(2, 4),
            ObjPattern => C(3, 4),
            WildPattern => C(0, 0),
            OrPattern => C(2, 4),
            GuardPattern => C(2, 4),
            TypeTestPattern => C(1, 4),

            // 特性组 - OA 强类型节点
            AttrGroup => C(1, 4),

            // 类型转换 - OA 强类型节点
            Cast => C(2, 4),

            // 循环降级 - OA 强类型节点
            LoopFilter => C(6, 8),
            LoopMap => C(6, 8),
            LoopReduce => C(6, 8),

            // 数据操作 - OA 强类型节点
            Map => C(6, 8),
            Filter => C(6, 8),
            Reduce => C(6, 8),

            Seq => C(1, 0),
            Repeat => C(4, 4),

            _ => C(3, 8)
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
    private static CostVector C(double latency, long memory)
    {
        return new CostVector(latency, latency * 0.1, memory, 0);
    }
}
