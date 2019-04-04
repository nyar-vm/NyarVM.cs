using Nyar.Dialect.Core.Nodes;
using Nyar.IR.Intent;
using Nyar.Types;

namespace Nyar.Optimizer.CostModels;

/// <summary>
///     WASM 代码大小成本模型，以生成指令的字节数为成本度量。
///     适用于 --optimize=aggressive 且目标为 WASM 的场景，优先选择代码体积更小的等价程序。
///     成本存储在 Memory 分量中，Latency 保持 1 以确保基本排序。
/// </summary>
public sealed class WasmSizeCostModel : ICostModel
{
    /// <inheritdoc />
    public CostVector node_cost(AlgebraNode node)
    {
        var size = EstimateWasmSize(node);
        return new CostVector(1.0, 0.0, size, 0);
    }

    /// <inheritdoc />
    public int compare(CostVector a, CostVector b)
    {
        return a.CompareTo(b);
    }

    private static long EstimateWasmSize(AlgebraNode node)
    {
        return node switch
        {
            Literal<long> => 6,
            Literal<double> => 9,
            Literal<bool> => 2,
            Literal<string> => 6,
            Literal<object?> => 1,
            VarDecl => 4,
            Attrib => 2,
            TypeRef => 2,
            UnionType => 3,
            IntersectType => 3,
            FuncType => 4,
            ListType => 3,
            ArrType => 4,
            TypeAlias => 3,
            Sym => 4,
            Import => 4,
            Export => 3,
            Mod => 2,
            Add or Sub or Cmp => 3,
            Neg or Not => 2,
            Lambda => 3,
            Apply => 3,
            Choice => 5,
            Compose => 1,
            Map => 8,
            Filter => 8,
            Reduce => 8,
            StateUp => 4,
            Ret => 2,
            Map or LoopMap or LoopReduce => 5,
            Lifecycle => 4,
            Meta => 2,
            Trap => 3,
            ArrayLit or Table => 4,
            Seq => 1,
            Repeat => 5,
            _ => 4
        };
    }
}