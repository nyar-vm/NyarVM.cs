using Nyar.Dialect.Core.Nodes;
using Nyar.EGraph;
using Nyar.IR.Intent;

namespace Nyar.PartialEvaluate;

/// <summary>
///     部分求值引擎，将编译时已知的表达式求值为常量
/// </summary>
public sealed class PartialEvaluator
{
    private readonly Dictionary<string, ConstantValue> _known_values;

    /// <summary>
    ///     创建部分求值引擎
    /// </summary>
    public PartialEvaluator()
    {
        _known_values = new Dictionary<string, ConstantValue>();
    }

    /// <summary>
    ///     注册已知常量值
    /// </summary>
    /// <param name="name">变量名。</param>
    /// <param name="value">常量值。</param>
    public void bind(string name, ConstantValue value)
    {
        _known_values[name] = value;
    }

    /// <summary>
    ///     对意图执行部分求值
    /// </summary>
    /// <param name="egraph">E-Graph 实例。</param>
    /// <param name="root">根节点标识符。</param>
    /// <returns>求值后的根节点标识符。</returns>
    public Id evaluate(EGraph<AlgebraNode> egraph, Id root)
    {
        var changed = true;
        var iterations = 0;
        const int maxIterations = 50;

        while (changed && iterations < maxIterations)
        {
            changed = false;
            iterations++;

            foreach (var (classId, eclass) in egraph.classes.ToList())
            foreach (var node in eclass.nodes.ToList())
            {
                var evaluated = try_evaluate(node, egraph);
                if (evaluated is not null)
                {
                    var evalId = egraph.add(evaluated);
                    var rootId = egraph.union_find.find(evalId);
                    if (rootId.value != classId)
                    {
                        egraph.union(new Id(classId), rootId);
                        changed = true;
                    }
                }
            }

            if (changed) egraph.rebuild();
        }

        return egraph.union_find.find(root);
    }

    /// <summary>
    ///     特化函数：为已知参数创建特化版本
    /// </summary>
    /// <param name="function">函数体。</param>
    /// <param name="parameters">参数列表。</param>
    /// <param name="knownArgs">已知参数值。</param>
    /// <returns>特化后的函数体。</returns>
    public AlgebraNode specialize(AlgebraNode function, IReadOnlyList<string> parameters,
        IReadOnlyDictionary<string, ConstantValue> knownArgs)
    {
        // 创建新的求值环境
        var evaluator = new PartialEvaluator();
        foreach (var (name, value) in knownArgs) evaluator.bind(name, value);

        // 在特化环境中求值函数体
        var egraph = new EGraph<AlgebraNode>();
        var funcId = egraph.add(function);
        evaluator.evaluate(egraph, funcId);

        // 提取结果
        var eclass = egraph.get_class(egraph.union_find.find(funcId));
        return eclass?.nodes.FirstOrDefault() ?? function;
    }

    private AlgebraNode? try_evaluate(AlgebraNode node, EGraph<AlgebraNode> egraph)
    {
        return node switch
        {
            // 常量传播
            Sym s when _known_values.TryGetValue(s.name, out var val) => val.to_i_kun(),

            // 二元运算常量折叠
            Add { left: var left, right: var right } =>
                try_fold_binary(left, right, egraph, (a, b) => a + b),
            Sub { left: var left, right: var right } =>
                try_fold_binary(left, right, egraph, (a, b) => a - b),
            Mul { left: var left, right: var right } =>
                try_fold_binary(left, right, egraph, (a, b) => a * b),
            Div { left: var left, right: var right } =>
                try_fold_binary(left, right, egraph, (a, b) => b != 0 ? a / b : null),

            // 一元运算常量折叠
            Neg { operand: var operand } =>
                try_fold_unary(operand, egraph, a => -a),
            Not { operand: var operand } =>
                try_fold_bool_unary(operand, egraph, a => !a),

            // 条件常量折叠
            Choice { condition: var cond, then: var then, elseBranch: var elseBranch } =>
                try_fold_choice(cond, then, elseBranch, egraph),

            // 数组长度常量折叠
            ArrayLit arr => new Literal<long>(arr.elements.Length),

            // 策略节点和物理节点已分离到独立层，PartialEvaluator 仅在纯意图层求值
            _ => null
        };
    }

    private static AlgebraNode? try_fold_binary(Id left, Id right, EGraph<AlgebraNode> egraph, Func<long, long, long?> op)
    {
        var leftValue = get_constant_value(left, egraph);
        var rightValue = get_constant_value(right, egraph);

        if (leftValue is null || rightValue is null) return null;

        var result = op(leftValue.value, rightValue.value);
        return result is not null ? new Literal<long>(result.Value) : null;
    }

    private static AlgebraNode? try_fold_unary(Id operand, EGraph<AlgebraNode> egraph, Func<long, long> op)
    {
        var value = get_constant_value(operand, egraph);
        if (value is null) return null;

        return new Literal<long>(op(value.value));
    }

    private static AlgebraNode? try_fold_bool_unary(Id operand, EGraph<AlgebraNode> egraph, Func<bool, bool> op)
    {
        var value = get_bool_constant_value(operand, egraph);
        if (value is null) return null;

        return new Literal<bool>(op(value.value));
    }

    private static AlgebraNode? try_fold_choice(Id cond, Id thenBranch, Id elseBranch, EGraph<AlgebraNode> egraph)
    {
        var condValue = get_bool_constant_value(cond, egraph);
        if (condValue is null) return null;

        // 返回对应分支的任意节点
        var targetClass = egraph.get_class(condValue.value ? thenBranch : elseBranch);
        return targetClass?.nodes.FirstOrDefault();
    }

    private static Literal<long>? get_constant_value(Id id, EGraph<AlgebraNode> egraph)
    {
        var eclass = egraph.get_class(id);
        return eclass?.nodes.OfType<Literal<long>>().FirstOrDefault();
    }

    private static Literal<bool>? get_bool_constant_value(Id id, EGraph<AlgebraNode> egraph)
    {
        var eclass = egraph.get_class(id);
        return eclass?.nodes.OfType<Literal<bool>>().FirstOrDefault();
    }
}