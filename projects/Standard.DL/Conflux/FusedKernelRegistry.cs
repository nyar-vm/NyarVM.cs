using Std.DL.Flux;

namespace Std.DL.Conflux;

/// <summary>
///     融合算子描述符 —— 描述一个融合算子的前向和反向计算逻辑
/// </summary>
/// <param name="Name">融合算子名称（如 "Conv2D_BN_ReLU"）</param>
/// <param name="ForwardFn">前向计算函数，接收输入张量数组，返回输出张量</param>
/// <param name="BackwardFn">反向梯度函数，接收输入张量数组和上游梯度数组，返回输入梯度张量；可为 null 表示不支持反向传播</param>
public record FusedKernelDescriptor(
    string Name,
    Func<ArrayND[], ArrayND> ForwardFn,
    Func<ArrayND[], ArrayND[], ArrayND>? BackwardFn
);

/// <summary>
///     融合算子注册表 —— 管理融合算子的注册、查找和执行
///     预注册常见融合模式：Conv2D+BN+ReLU、Dense+ReLU、Dense+GELU、Dense+SiLU
/// </summary>
public sealed class FusedKernelRegistry
{
    #region 字段

    private readonly Dictionary<string, FusedKernelDescriptor> _kernels;

    #endregion

    #region 构造函数

    /// <summary>
    ///     创建融合算子注册表并预注册常见融合模式
    /// </summary>
    public FusedKernelRegistry()
    {
        _kernels = new Dictionary<string, FusedKernelDescriptor>();
        RegisterBuiltinKernels();
    }

    #endregion

    #region 公开方法

    /// <summary>
    ///     注册融合算子
    /// </summary>
    /// <param name="kernel">融合算子描述符</param>
    /// <exception cref="ArgumentException">当同名算子已注册时抛出</exception>
    public void Register(FusedKernelDescriptor kernel)
    {
        if (!_kernels.TryAdd(kernel.Name, kernel)) throw new ArgumentException($"融合算子已注册：{kernel.Name}");
    }

    /// <summary>
    ///     按名称查找融合算子
    /// </summary>
    /// <param name="name">融合算子名称</param>
    /// <param name="kernel">找到的融合算子描述符；未找到时为 null</param>
    /// <returns>是否找到</returns>
    public bool TryGet(string name, out FusedKernelDescriptor? kernel)
    {
        return _kernels.TryGetValue(name, out kernel);
    }

    /// <summary>
    ///     执行融合算子的前向计算
    /// </summary>
    /// <param name="name">融合算子名称</param>
    /// <param name="inputs">输入张量数组</param>
    /// <returns>输出张量</returns>
    /// <exception cref="KeyNotFoundException">当指定名称的融合算子未注册时抛出</exception>
    public ArrayND Execute(string name, ArrayND[] inputs)
    {
        if (!_kernels.TryGetValue(name, out var kernel)) throw new KeyNotFoundException($"未注册的融合算子：{name}");

        return kernel.ForwardFn(inputs);
    }

    /// <summary>
    ///     获取所有已注册的融合算子名称
    /// </summary>
    public IReadOnlyList<string> RegisteredNames => _kernels.Keys.ToList().AsReadOnly();

    /// <summary>
    ///     已注册的融合算子数量
    /// </summary>
    public int Count => _kernels.Count;

    #endregion

    #region 预注册融合模式

    /// <summary>
    ///     注册内置的常见融合模式
    /// </summary>
    private void RegisterBuiltinKernels()
    {
        Register(Conv2DBnReLU());
        Register(MatMulBiasAddReLU());
        Register(MatMulBiasAddGELU());
        Register(MatMulBiasAddSiLU());
    }

    /// <summary>
    ///     Conv2D + BatchNorm + ReLU 融合
    ///     输入：[0] 卷积输出, [1] BN gamma, [2] BN beta
    ///     计算：ReLU(gamma * conv_output + beta)
    ///     假设 BN 参数已折叠为仿射变换（scale + shift）
    /// </summary>
    private static FusedKernelDescriptor Conv2DBnReLU()
    {
        return new FusedKernelDescriptor(
            "Conv2D_BN_ReLU",
            inputs =>
            {
                var convOutput = inputs[0];
                var gamma = inputs[1];
                var beta = inputs[2];
                var scaled = convOutput * gamma;
                var shifted = scaled + beta;
                return Activations.ReLUForward(shifted);
            },
            null
        );
    }

    /// <summary>
    ///     MatMul + BiasAdd + ReLU 融合（Dense + ReLU）
    ///     输入：[0] 输入张量, [1] 权重, [2] 偏置
    ///     计算：ReLU(MatMul(input, weight) + bias)
    /// </summary>
    private static FusedKernelDescriptor MatMulBiasAddReLU()
    {
        return new FusedKernelDescriptor(
            "MatMul_BiasAdd_ReLU",
            inputs =>
            {
                var matmulResult = ArrayND.MatMul(inputs[0], inputs[1]);
                var biased = matmulResult + inputs[2];
                return Activations.ReLUForward(biased);
            },
            null
        );
    }

    /// <summary>
    ///     MatMul + BiasAdd + GELU 融合（Dense + GELU）
    ///     输入：[0] 输入张量, [1] 权重, [2] 偏置
    ///     计算：GELU(MatMul(input, weight) + bias)
    /// </summary>
    private static FusedKernelDescriptor MatMulBiasAddGELU()
    {
        return new FusedKernelDescriptor(
            "MatMul_BiasAdd_GELU",
            inputs =>
            {
                var matmulResult = ArrayND.MatMul(inputs[0], inputs[1]);
                var biased = matmulResult + inputs[2];
                return Activations.GELUForward(biased);
            },
            null
        );
    }

    /// <summary>
    ///     MatMul + BiasAdd + SiLU 融合（Dense + SiLU，SwiGLU 模式）
    ///     输入：[0] 输入张量, [1] 权重, [2] 偏置
    ///     计算：SiLU(MatMul(input, weight) + bias)
    /// </summary>
    private static FusedKernelDescriptor MatMulBiasAddSiLU()
    {
        return new FusedKernelDescriptor(
            "MatMul_BiasAdd_SiLU",
            inputs =>
            {
                var matmulResult = ArrayND.MatMul(inputs[0], inputs[1]);
                var biased = matmulResult + inputs[2];
                return Activations.SiLUForward(biased);
            },
            null
        );
    }

    #endregion
}