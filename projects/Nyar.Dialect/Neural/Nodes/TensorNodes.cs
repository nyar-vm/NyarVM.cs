using Nyar.IR.Intent;

namespace Nyar.Dialect.Neural.Nodes;

#region 基础算子

[AlgebraNode]
public sealed partial record Conv2D(
    Id input,
    Id weights,
    Id? bias,
    (int StrideH, int StrideW) stride,
    (int PadH, int PadW) padding) : AlgebraNode;

[AlgebraNode]
public sealed partial record MatMul(Id left, Id right) : AlgebraNode;

/// <summary>
///     全连接层算子（Dense/Linear）
///     y = x @ W + b
/// </summary>
[AlgebraNode]
public sealed partial record Dense(Id input, Id weights, Id? bias) : AlgebraNode;

[AlgebraNode]
public sealed partial record Pool(PoolType type, Id input, (int H, int W) kernelSize, (int H, int W) stride) : AlgebraNode;

public enum PoolType
{
    Max,
    Avg
}

[AlgebraNode]
public sealed partial record BatchNorm(Id input, Id scale, Id bias, Id runningMean, Id runningVar, double epsilon)
    : AlgebraNode;

[AlgebraNode]
public sealed partial record Relu(Id input) : AlgebraNode;

/// <summary>
///     Sigmoid 激活函数算子
///     σ(x) = 1 / (1 + exp(-x))
/// </summary>
[AlgebraNode]
public sealed partial record Sigmoid(Id input) : AlgebraNode;

/// <summary>
///     Tanh 激活函数算子
///     tanh(x) = (exp(x) - exp(-x)) / (exp(x) + exp(-x))
/// </summary>
[AlgebraNode]
public sealed partial record Tanh(Id input) : AlgebraNode;

[AlgebraNode]
public sealed partial record Softmax(Id input, int axis) : AlgebraNode;

[AlgebraNode]
public sealed partial record Reshape(Id input, IReadOnlyList<int> shape) : AlgebraNode;

[AlgebraNode]
public sealed partial record Transpose(Id input, IReadOnlyList<int> perm) : AlgebraNode;

[AlgebraNode]
public sealed partial record Concat(IReadOnlyList<Id> inputs, int axis) : AlgebraNode;

[AlgebraNode]
public sealed partial record Slice(Id input, IReadOnlyList<(int Start, int End, int Step)> ranges) : AlgebraNode;

[AlgebraNode]
public sealed partial record FusedConvBnRelu(
    Id input,
    Id weights,
    Id? bias,
    Id scale,
    Id bnBias,
    Id runningMean,
    Id runningVar,
    double epsilon) : AlgebraNode;

[AlgebraNode]
public sealed partial record Grad(Id loss, IReadOnlyList<Id> parameters) : AlgebraNode;

[AlgebraNode]
public sealed partial record OptimizerStep(
    string optimizerType,
    IReadOnlyList<Id> parameters,
    IReadOnlyList<Id> gradients) : AlgebraNode;

[AlgebraNode]
public sealed partial record Loss(LossType type, Id predicted, Id target) : AlgebraNode;

public enum LossType
{
    Mse,
    CrossEntropy,
    BinaryCrossEntropy
}

#endregion

#region Transformer/LLM 算子

/// <summary>
///     融合注意力算子
///     将 QKV 投影 + Softmax + Dropout 融合为单一算子
///     对应 Valkyrie Neural 的 transformer_block 中的注意力部分
/// </summary>
[AlgebraNode]
public sealed partial record FusedAttention(
    Id query,
    Id key,
    Id value,
    Id? attentionMask,
    int heads,
    int headDim,
    double? dropoutRate) : AlgebraNode;

/// <summary>
///     RMS 归一化算子
///     LLM 常用的归一化层，比 LayerNorm 更高效
/// </summary>
[AlgebraNode]
public sealed partial record RmsNorm(Id input, Id weight, double epsilon) : AlgebraNode;

/// <summary>
///     层归一化算子
///     Transformer 中的标准归一化层
/// </summary>
[AlgebraNode]
public sealed partial record LayerNorm(Id input, Id weight, Id bias, double epsilon) : AlgebraNode;

/// <summary>
///     嵌入查表算子
///     将 token ID 映射到高维向量空间
///     对应 Valkyrie Neural 的 embedding 层
/// </summary>
[AlgebraNode]
public sealed partial record Embedding(Id input, Id weights) : AlgebraNode;

/// <summary>
///     旋转位置编码算子（RoPE）
///     LLM 中主流的相对位置编码方式
///     对应 Valkyrie Neural 的 rotary_position_encoding 层
/// </summary>
[AlgebraNode]
public sealed partial record RotaryPositionEncoding(Id input, Id positionIds, int dim, double theta) : AlgebraNode;

/// <summary>
///     SiLU 激活函数算子
///     LLaMA 等模型中 FFN 的门控激活
///     SiLU(x) = x * sigmoid(x)
/// </summary>
[AlgebraNode]
public sealed partial record Silu(Id input) : AlgebraNode;

/// <summary>
///     GELU 激活函数算子
///     BERT/GPT 等模型中 FFN 的激活函数
/// </summary>
[AlgebraNode]
public sealed partial record Gelu(Id input) : AlgebraNode;

/// <summary>
///     LoRA 适配器算子
///     低秩适配微调：y = x @ W + (x @ A) @ B
///     其中 A 为降维矩阵，B 为升维矩阵
/// </summary>
[AlgebraNode]
public sealed partial record LoRA(Id input, Id weights, Id downProjection, Id upProjection, double alpha) : AlgebraNode;

/// <summary>
///     梯度检查点算子
///     用重计算换显存，训练时选择性地保存中间激活
/// </summary>
[AlgebraNode]
public sealed partial record Checkpoint(Id input, IReadOnlyList<Id> segments) : AlgebraNode;

/// <summary>
///     KV-Cache 管理算子
///     LLM 推理时缓存 Key/Value 张量，避免重复计算
/// </summary>
[AlgebraNode]
public sealed partial record KvCache(Id key, Id value, Id? cacheKey, Id? cacheValue, int seqLen) : AlgebraNode;

/// <summary>
///     类型转换算子
///     用于混合精度训练和量化推理
/// </summary>
[AlgebraNode]
public sealed partial record Cast(Id input, string targetDtype) : AlgebraNode;

/// <summary>
///     Dropout 正则化算子
///     训练时随机置零部分神经元
/// </summary>
[AlgebraNode]
public sealed partial record Dropout(Id input, double rate) : AlgebraNode;

/// <summary>
///     展平算子 —— 将多维张量展平为二维 [batch, features]
/// </summary>
[AlgebraNode]
public sealed partial record Flatten(Id input, int startDim) : AlgebraNode;

/// <summary>
///     逐元素加法
/// </summary>
[AlgebraNode]
public sealed partial record ElementWiseAdd(Id left, Id right) : AlgebraNode;

/// <summary>
///     逐元素乘法
/// </summary>
[AlgebraNode]
public sealed partial record ElementWiseMul(Id left, Id right) : AlgebraNode;

#endregion