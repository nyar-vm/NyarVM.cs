namespace Nyar.Dialect.Neural.Rules;

/// <summary>
///     Tensor 方言内置函数 ID（映射到 Literal&lt;long&gt; 的 long 值）
/// </summary>
public enum TensorBuiltin : long
{
    Conv2D = 0xA001,
    MatMul = 0xA002,
    BatchNorm = 0xA003,
    Relu = 0xA004,
    Softmax = 0xA005,
    Reshape = 0xA006,
    Transpose = 0xA007,
    Cast = 0xA008,
    Silu = 0xA009,
    Gelu = 0xA00A,
    LayerNorm = 0xA00B,
    RmsNorm = 0xA00C,
    LoRA = 0xA00D,
    Embedding = 0xA00E,
    FusedConvBnRelu = 0xA00F,
    MaxPool = 0xA010,
    AvgPool = 0xA011,
    Flatten = 0xA012,
    Dropout = 0xA013,
    Dense = 0xA014,
    Concat = 0xA015,
    Slice = 0xA016,
    ElementWiseAdd = 0xA017,
    ElementWiseMul = 0xA018,
    Sigmoid = 0xA022,
    Tanh = 0xA023,

    FusedAttention = 0xA019,
    RotaryPositionEncoding = 0xA01A,
    KvCache = 0xA01B,
    Checkpoint = 0xA01C,
    Grad = 0xA01D,
    OptimizerStep = 0xA01E,
    LossMse = 0xA01F,
    LossCrossEntropy = 0xA020,
    LossBinaryCrossEntropy = 0xA021
}