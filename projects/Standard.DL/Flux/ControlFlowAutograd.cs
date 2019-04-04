namespace Std.DL.Flux;

/// <summary>
///     控制流自动微分：条件分支和循环的梯度传播
///     条件分支：梯度仅流经被选中的路径
///     循环扫描：梯度通过所有迭代步累积
/// </summary>
public static class ControlFlowAutograd
{
    /// <summary>
    ///     条件前向：根据条件选择两个计算路径之一
    ///     梯度仅流经被选中的路径，未选中路径的梯度为零
    /// </summary>
    /// <param name="condition">条件值（true 选择 trueBranch，false 选择 falseBranch）</param>
    /// <param name="trueBranch">条件为 true 时的计算函数</param>
    /// <param name="falseBranch">条件为 false 时的计算函数</param>
    /// <param name="input">输入张量</param>
    /// <param name="ctx">自动微分上下文</param>
    /// <returns>选中路径的输出张量</returns>
    public static ArrayND Conditional(
        bool condition,
        Func<ArrayND, AutogradContext, ArrayND> trueBranch,
        Func<ArrayND, AutogradContext, ArrayND> falseBranch,
        ArrayND input,
        AutogradContext ctx)
    {
        ArgumentNullException.ThrowIfNull(trueBranch);
        ArgumentNullException.ThrowIfNull(falseBranch);

        if (condition)
        {
            var output = trueBranch(input, ctx);
            ctx.Record(output, [input], grad => { return [grad[0]]; });
            return output;
        }
        else
        {
            var output = falseBranch(input, ctx);
            ctx.Record(output, [input], grad => { return [grad[0]]; });
            return output;
        }
    }

    /// <summary>
    ///     条件前向（双输入版）：两个分支可以接收不同的输入
    /// </summary>
    public static ArrayND Conditional(
        bool condition,
        Func<ArrayND, AutogradContext, ArrayND> trueBranch,
        Func<ArrayND, AutogradContext, ArrayND> falseBranch,
        ArrayND trueInput,
        ArrayND falseInput,
        AutogradContext ctx)
    {
        ArgumentNullException.ThrowIfNull(trueBranch);
        ArgumentNullException.ThrowIfNull(falseBranch);

        if (condition)
        {
            var output = trueBranch(trueInput, ctx);
            ctx.Record(output, [trueInput], grad =>
            {
                var falseGrad = ArrayND.Zeros(falseInput.Shape);
                return [grad[0], falseGrad];
            });
            return output;
        }
        else
        {
            var output = falseBranch(falseInput, ctx);
            ctx.Record(output, [falseInput], grad =>
            {
                var trueGrad = ArrayND.Zeros(trueInput.Shape);
                return [trueGrad, grad[0]];
            });
            return output;
        }
    }

    /// <summary>
    ///     扫描循环：将函数迭代应用 n 次，梯度通过所有迭代步反向累积
    ///     等价于函数复合的链式法则：d(f^n)/dx = df/df_{n-1} * df_{n-1}/df_{n-2} * ... * df_0/dx
    /// </summary>
    /// <param name="iterations">迭代次数</param>
    /// <param name="body">循环体函数：接收当前状态，返回下一状态</param>
    /// <param name="initial">初始输入张量</param>
    /// <param name="ctx">自动微分上下文</param>
    /// <returns>最终迭代输出</returns>
    public static ArrayND ScanLoop(
        int iterations,
        Func<ArrayND, AutogradContext, ArrayND> body,
        ArrayND initial,
        AutogradContext ctx)
    {
        ArgumentNullException.ThrowIfNull(body);
        ArgumentOutOfRangeException.ThrowIfNegative(iterations);

        var current = initial;

        for (var i = 0; i < iterations; i++)
        {
            var input = current;
            current = body(input, ctx);

            var iteration = i;
            ctx.Record(current, [input], grad => { return [grad[0]]; });
        }

        return current;
    }

    /// <summary>
    ///     带状态的扫描循环：每步产生一个输出，所有输出被收集
    ///     梯度通过所有时间步反向传播（BPTT）
    /// </summary>
    /// <param name="iterations">迭代次数</param>
    /// <param name="body">循环体函数：接收 (当前状态, 步索引)，返回 (输出, 下一状态)</param>
    /// <param name="initialState">初始状态张量</param>
    /// <param name="ctx">自动微分上下文</param>
    /// <returns>所有步的输出数组和最终状态</returns>
    public static (ArrayND[] Outputs, ArrayND FinalState) ScanLoopWithState(
        int iterations,
        Func<ArrayND, int, AutogradContext, (ArrayND Output, ArrayND NextState)> body,
        ArrayND initialState,
        AutogradContext ctx)
    {
        ArgumentNullException.ThrowIfNull(body);
        ArgumentOutOfRangeException.ThrowIfNegative(iterations);

        var outputs = new ArrayND[iterations];
        var state = initialState;

        for (var i = 0; i < iterations; i++)
        {
            var currentState = state;
            var (output, nextState) = body(currentState, i, ctx);

            ctx.Record(output, [currentState], grad => { return [grad[0]]; });

            ctx.Record(nextState, [currentState], grad => { return [grad[0]]; });

            outputs[i] = output;
            state = nextState;
        }

        return (outputs, state);
    }

    /// <summary>
    ///     While 循环：条件满足时持续迭代
    ///     梯度通过所有已执行的迭代步反向传播
    /// </summary>
    /// <param name="condition">继续条件：接收当前状态，返回是否继续</param>
    /// <param name="body">循环体函数</param>
    /// <param name="initial">初始输入张量</param>
    /// <param name="ctx">自动微分上下文</param>
    /// <param name="maxIterations">最大迭代次数（防止无限循环）</param>
    /// <returns>最终状态</returns>
    public static ArrayND WhileLoop(
        Func<ArrayND, bool> condition,
        Func<ArrayND, AutogradContext, ArrayND> body,
        ArrayND initial,
        AutogradContext ctx,
        int maxIterations = 1000)
    {
        ArgumentNullException.ThrowIfNull(condition);
        ArgumentNullException.ThrowIfNull(body);

        var current = initial;
        var iteration = 0;

        while (condition(current) && iteration < maxIterations)
        {
            var input = current;
            current = body(input, ctx);

            ctx.Record(current, [input], grad => { return [grad[0]]; });

            iteration++;
        }

        return current;
    }
}