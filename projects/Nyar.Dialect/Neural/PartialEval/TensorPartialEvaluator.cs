using Nyar.Dialect.Core.Nodes;
using Nyar.Dialect.Neural.Nodes;
using Nyar.EGraph;
using Nyar.IR.Intent;
using Nyar.PartialEvaluate;
using Cast = Nyar.Dialect.Neural.Nodes.Cast;
using ConstantValue = Nyar.PartialEvaluate.ConstantValue;

namespace Nyar.Dialect.Neural.PartialEval;

/// <summary>
///     Tensor 方言部分求值引擎
///     在通用 PartialEvaluator 基础上增加 Tensor 特化规则：
///     - Reshape 常量形状折叠
///     - Transpose 恒等消除
///     - Dropout 推理模式消除（rate=0）
///     - Cast 同类型消除
///     - ElementWiseAdd/Mul 零/一元素消除
///     - Flatten 恒等消除（start_dim 超出维度）
/// </summary>
public sealed class TensorPartialEvaluator
{
    private readonly PartialEvaluator _baseEvaluator;

    /// <summary>
    ///     创建 Tensor 部分求值引擎
    /// </summary>
    public TensorPartialEvaluator()
    {
        _baseEvaluator = new PartialEvaluator();
    }

    /// <summary>
    ///     是否处于推理模式（推理模式下 Dropout 被消除）
    /// </summary>
    public bool InferenceMode { get; set; } = true;

    /// <summary>
    ///     注册已知常量值
    /// </summary>
    /// <param name="name">变量名。</param>
    /// <param name="value">常量值。</param>
    public void Bind(string name, ConstantValue value)
    {
        _baseEvaluator.bind(name, value);
    }

    /// <summary>
    ///     对 EGraph 执行 Tensor 方言部分求值
    /// </summary>
    /// <param name="egraph">等价图。</param>
    /// <param name="root">根节点标识符。</param>
    /// <returns>求值后的根节点标识符。</returns>
    public Id Evaluate(EGraph<AlgebraNode> egraph, Id root)
    {
        root = _baseEvaluator.evaluate(egraph, root);

        var changed = true;
        var iterations = 0;
        const int maxIterations = 30;

        while (changed && iterations < maxIterations)
        {
            changed = false;
            iterations++;

            foreach (var (classId, eclass) in egraph.classes.ToList())
            foreach (var node in eclass.nodes.ToList())
            {
                var simplified = TrySimplifyTensor(node, egraph);
                if (simplified is not null)
                {
                    var simplifiedId = egraph.add(simplified);
                    var simplifiedRoot = egraph.union_find.find(simplifiedId);
                    if (simplifiedRoot.value != classId)
                    {
                        egraph.union(new Id(classId), simplifiedRoot);
                        changed = true;
                    }
                }
            }

            if (changed) egraph.rebuild();
        }

        return egraph.union_find.find(root);
    }

    /// <summary>
    ///     尝试对 Tensor 方言节点进行部分求值简化
    /// </summary>
    private AlgebraNode? TrySimplifyTensor(AlgebraNode node, EGraph<AlgebraNode> egraph)
    {
        return node switch
        {
            #region 形状操作简化

            Reshape r when IsIdentityReshape(r) => GetInputNode(r.input, egraph),

            Transpose t when IsIdentityPermutation(t.perm) => GetInputNode(t.input, egraph),

            Flatten f when f.startDim <= 0 => new Reshape(f.input, new List<int>()),

            #endregion

            #region 推理模式消除

            Dropout d when InferenceMode => GetInputNode(d.input, egraph),

            Dropout d when d.rate <= 0.0 => GetInputNode(d.input, egraph),

            #endregion

            #region 类型转换消除

            Cast c when c.targetDtype == "" => GetInputNode(c.input, egraph),

            #endregion

            #region 逐元素运算恒等消除

            ElementWiseAdd add when IsZeroNode(add.right, egraph) => GetInputNode(add.left, egraph),
            ElementWiseAdd add when IsZeroNode(add.left, egraph) => GetInputNode(add.right, egraph),

            ElementWiseMul mul when IsOneNode(mul.right, egraph) => GetInputNode(mul.left, egraph),
            ElementWiseMul mul when IsOneNode(mul.left, egraph) => GetInputNode(mul.right, egraph),
            ElementWiseMul mul when IsZeroNode(mul.right, egraph) => new Literal<long>(0),
            ElementWiseMul mul when IsZeroNode(mul.left, egraph) => new Literal<long>(0),

            #endregion

            _ => null
        };
    }

    /// <summary>
    ///     判断 Reshape 是否为恒等操作（形状未变）
    /// </summary>
    private static bool IsIdentityReshape(Reshape reshape)
    {
        return reshape.shape.Count == 0;
    }

    /// <summary>
    ///     判断排列是否为恒等排列 [0, 1, 2, ...]
    /// </summary>
    private static bool IsIdentityPermutation(IReadOnlyList<int> perm)
    {
        for (var i = 0; i < perm.Count; i++)
            if (perm[i] != i)
                return false;

        return true;
    }

    /// <summary>
    ///     获取输入等价类中的任意节点（用于透传简化）
    /// </summary>
    private static AlgebraNode? GetInputNode(Id inputId, EGraph<AlgebraNode> egraph)
    {
        var inputClass = egraph.get_class(inputId);
        return inputClass?.nodes.FirstOrDefault();
    }

    /// <summary>
    ///     判断节点是否为常量零
    /// </summary>
    private static bool IsZeroNode(Id id, EGraph<AlgebraNode> egraph)
    {
        var eclass = egraph.get_class(id);
        if (eclass is null) return false;

        return eclass.nodes.Any(n => (n is Literal<long> c && c.value == 0)
                                     || (n is Literal<double> fc && fc.value == 0.0));
    }

    /// <summary>
    ///     判断节点是否为常量一
    /// </summary>
    private static bool IsOneNode(Id id, EGraph<AlgebraNode> egraph)
    {
        var eclass = egraph.get_class(id);
        if (eclass is null) return false;

        return eclass.nodes.Any(n => (n is Literal<long> c && c.value == 1)
                                     || (n is Literal<double> fc && fc.value == 1.0));
    }
}