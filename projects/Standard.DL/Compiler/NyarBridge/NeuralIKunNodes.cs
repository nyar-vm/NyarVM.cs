using Nyar.IR.Intent;

namespace Std.DL.Compiler.NyarBridge;

/// <summary>
///     ⛔ Pool 类型枚举（遗留）。IKun 节点宇宙已冻结，
///     请使用 OA algebra + ENode 开放模型替代。
/// </summary>
[Obsolete("IKun 节点宇宙已冻结。请使用 OA algebra + ENode 开放模型")]
public enum PoolType
{
    /// <summary>最大值池化</summary>
    Max,

    /// <summary>平均值池化</summary>
    Avg
}

#region 激活函数节点

/// <summary>
///     ReLU 激活函数节点
/// </summary>
/// <param name="Input">输入张量 Id。</param>
public sealed record Relu(Id Input) : IKun
{
    /// <inheritdoc />
    public override IReadOnlyList<Id> ChildIds()
    {
        return [Input];
    }

    /// <inheritdoc />
    public override IKun MapChildren(Func<Id, Id> f)
    {
        return this with { Input = f(Input) };
    }
}

/// <summary>
///     Softmax 激活函数节点
/// </summary>
/// <param name="Input">输入张量 Id。</param>
/// <param name="Axis">计算 Softmax 的轴。</param>
public sealed record Softmax(Id Input, int Axis) : IKun
{
    /// <inheritdoc />
    public override IReadOnlyList<Id> ChildIds()
    {
        return [Input];
    }

    /// <inheritdoc />
    public override IKun MapChildren(Func<Id, Id> f)
    {
        return this with { Input = f(Input) };
    }
}

/// <summary>
///     GELU 激活函数节点
/// </summary>
/// <param name="Input">输入张量 Id。</param>
public sealed record Gelu(Id Input) : IKun
{
    /// <inheritdoc />
    public override IReadOnlyList<Id> ChildIds()
    {
        return [Input];
    }

    /// <inheritdoc />
    public override IKun MapChildren(Func<Id, Id> f)
    {
        return this with { Input = f(Input) };
    }
}

/// <summary>
///     SiLU（Swish）激活函数节点
/// </summary>
/// <param name="Input">输入张量 Id。</param>
public sealed record Silu(Id Input) : IKun
{
    /// <inheritdoc />
    public override IReadOnlyList<Id> ChildIds()
    {
        return [Input];
    }

    /// <inheritdoc />
    public override IKun MapChildren(Func<Id, Id> f)
    {
        return this with { Input = f(Input) };
    }
}

#endregion

#region 线性代数节点

/// <summary>
///     矩阵乘法节点
/// </summary>
/// <param name="A">左矩阵 Id。</param>
/// <param name="B">右矩阵 Id。</param>
public sealed record MatMul(Id A, Id B) : IKun
{
    /// <inheritdoc />
    public override IReadOnlyList<Id> ChildIds()
    {
        return [A, B];
    }

    /// <inheritdoc />
    public override IKun MapChildren(Func<Id, Id> f)
    {
        return this with { A = f(A), B = f(B) };
    }
}

/// <summary>
///     卷积节点
/// </summary>
/// <param name="Input">输入张量 Id。</param>
/// <param name="Weight">卷积核权重 Id。</param>
/// <param name="Bias">可选偏置 Id。</param>
/// <param name="Strides">步长 (H, W)。</param>
/// <param name="Padding">填充 (H, W)。</param>
public sealed record Conv2D(Id Input, Id Weight, Id? Bias, (int H, int W) Strides, (int H, int W) Padding) : IKun
{
    /// <inheritdoc />
    public override IReadOnlyList<Id> ChildIds()
    {
        var list = new List<Id> { Input, Weight };
        if (Bias.HasValue) list.Add(Bias.Value);

        return list;
    }

    /// <inheritdoc />
    public override IKun MapChildren(Func<Id, Id> f)
    {
        return this with
        {
            Input = f(Input),
            Weight = f(Weight),
            Bias = Bias.HasValue ? f(Bias.Value) : null
        };
    }
}

#endregion

#region 池化节点

/// <summary>
///     池化节点（支持 Max 和 Avg）
/// </summary>
/// <param name="Type">池化类型。</param>
/// <param name="Input">输入张量 Id。</param>
/// <param name="Kernel">卷积核大小 (H, W)。</param>
/// <param name="Stride">步长 (H, W)。</param>
public sealed record Pool(PoolType Type, Id Input, (int H, int W) Kernel, (int H, int W) Stride) : IKun
{
    /// <inheritdoc />
    public override IReadOnlyList<Id> ChildIds()
    {
        return [Input];
    }

    /// <inheritdoc />
    public override IKun MapChildren(Func<Id, Id> f)
    {
        return this with { Input = f(Input) };
    }
}

#endregion

#region 归一化节点

/// <summary>
///     BatchNorm 归一化节点
/// </summary>
/// <param name="Input">输入张量 Id。</param>
/// <param name="Scale">缩放参数 Id。</param>
/// <param name="Bias">偏置参数 Id。</param>
/// <param name="Mean">运行均值 Id。</param>
/// <param name="Var">运行方差 Id。</param>
/// <param name="Epsilon">数值稳定性参数。</param>
public sealed record BatchNorm(Id Input, Id Scale, Id Bias, Id Mean, Id Var, double Epsilon) : IKun
{
    /// <inheritdoc />
    public override IReadOnlyList<Id> ChildIds()
    {
        return [Input, Scale, Bias, Mean, Var];
    }

    /// <inheritdoc />
    public override IKun MapChildren(Func<Id, Id> f)
    {
        return this with
        {
            Input = f(Input),
            Scale = f(Scale),
            Bias = f(Bias),
            Mean = f(Mean),
            Var = f(Var)
        };
    }
}

/// <summary>
///     LayerNorm 归一化节点
/// </summary>
/// <param name="Input">输入张量 Id。</param>
/// <param name="Weight">权重参数 Id。</param>
/// <param name="Bias">偏置参数 Id。</param>
/// <param name="Epsilon">数值稳定性参数。</param>
public sealed record LayerNorm(Id Input, Id Weight, Id Bias, double Epsilon) : IKun
{
    /// <inheritdoc />
    public override IReadOnlyList<Id> ChildIds()
    {
        return [Input, Weight, Bias];
    }

    /// <inheritdoc />
    public override IKun MapChildren(Func<Id, Id> f)
    {
        return this with
        {
            Input = f(Input),
            Weight = f(Weight),
            Bias = f(Bias)
        };
    }
}

#endregion

#region 嵌入与变换节点

/// <summary>
///     Embedding 嵌入查询节点
/// </summary>
/// <param name="Input">输入索引 Id。</param>
/// <param name="Weight">嵌入权重矩阵 Id。</param>
public sealed record Embedding(Id Input, Id Weight) : IKun
{
    /// <inheritdoc />
    public override IReadOnlyList<Id> ChildIds()
    {
        return [Input, Weight];
    }

    /// <inheritdoc />
    public override IKun MapChildren(Func<Id, Id> f)
    {
        return this with { Input = f(Input), Weight = f(Weight) };
    }
}

/// <summary>
///     Flatten 展平节点
/// </summary>
/// <param name="Input">输入张量 Id。</param>
/// <param name="StartDim">开始展平的维度索引。</param>
public sealed record Flatten(Id Input, int StartDim) : IKun
{
    /// <inheritdoc />
    public override IReadOnlyList<Id> ChildIds()
    {
        return [Input];
    }

    /// <inheritdoc />
    public override IKun MapChildren(Func<Id, Id> f)
    {
        return this with { Input = f(Input) };
    }
}

/// <summary>
///     Dropout 随机丢弃节点
/// </summary>
/// <param name="Input">输入张量 Id。</param>
/// <param name="Rate">丢弃概率。</param>
public sealed record Dropout(Id Input, float Rate) : IKun
{
    /// <inheritdoc />
    public override IReadOnlyList<Id> ChildIds()
    {
        return [Input];
    }

    /// <inheritdoc />
    public override IKun MapChildren(Func<Id, Id> f)
    {
        return this with { Input = f(Input) };
    }
}

/// <summary>
///     Reshape 形状变换节点
/// </summary>
/// <param name="Input">输入张量 Id。</param>
/// <param name="Shape">目标形状列表。</param>
public sealed record Reshape(Id Input, IReadOnlyList<int> Shape) : IKun
{
    /// <inheritdoc />
    public override IReadOnlyList<Id> ChildIds()
    {
        return [Input];
    }

    /// <inheritdoc />
    public override IKun MapChildren(Func<Id, Id> f)
    {
        return this with { Input = f(Input) };
    }
}

/// <summary>
///     Concat 拼接节点
/// </summary>
/// <param name="Inputs">输入张量 Id 列表。</param>
/// <param name="Axis">拼接轴。</param>
public sealed record Concat(IReadOnlyList<Id> Inputs, int Axis) : IKun
{
    /// <inheritdoc />
    public override IReadOnlyList<Id> ChildIds()
    {
        return Inputs;
    }

    /// <inheritdoc />
    public override IKun MapChildren(Func<Id, Id> f)
    {
        return this with { Inputs = [.. Inputs.Select(f)] };
    }
}

#endregion

#region 逐元素运算节点

/// <summary>
///     逐元素加法节点
/// </summary>
/// <param name="A">左操作数 Id。</param>
/// <param name="B">右操作数 Id。</param>
public sealed record ElementWiseAdd(Id A, Id B) : IKun
{
    /// <inheritdoc />
    public override IReadOnlyList<Id> ChildIds()
    {
        return [A, B];
    }

    /// <inheritdoc />
    public override IKun MapChildren(Func<Id, Id> f)
    {
        return this with { A = f(A), B = f(B) };
    }
}

/// <summary>
///     逐元素乘法节点
/// </summary>
/// <param name="A">左操作数 Id。</param>
/// <param name="B">右操作数 Id。</param>
public sealed record ElementWiseMul(Id A, Id B) : IKun
{
    /// <inheritdoc />
    public override IReadOnlyList<Id> ChildIds()
    {
        return [A, B];
    }

    /// <inheritdoc />
    public override IKun MapChildren(Func<Id, Id> f)
    {
        return this with { A = f(A), B = f(B) };
    }
}

#endregion