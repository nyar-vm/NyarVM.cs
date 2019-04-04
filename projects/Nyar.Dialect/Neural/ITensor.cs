using Nyar.Dialect.Neural.Nodes;
using Nyar.ObjectAlgebra;

namespace Nyar.Dialect.Neural;

/// <summary>
///     Tensor 方言的 OA 接口定义。
///     该接口声明神经网络、科学计算与 Transformer 相关算子。
/// </summary>
[Dialect("tensor")]
public interface ITensor<T>
{
    /// <summary>
    ///     二维卷积。
    /// </summary>
    [Operator("conv2d")]
    Term<T> Conv2D(
        Term<T> input,
        Term<T> weights,
        Term<T>? bias,
        (int StrideH, int StrideW) stride,
        (int PadH, int PadW) padding);

    /// <summary>
    ///     矩阵乘法。
    /// </summary>
    [Operator("mat_mul")]
    Term<T> MatMul(Term<T> left, Term<T> right);

    /// <summary>
    ///     全连接层。
    /// </summary>
    [Operator("dense")]
    Term<T> Dense(Term<T> input, Term<T> weights, Term<T>? bias);

    /// <summary>
    ///     池化算子。
    /// </summary>
    [Operator("pool")]
    Term<T> Pool(PoolType type, Term<T> input, (int H, int W) kernelSize, (int H, int W) stride);

    /// <summary>
    ///     批归一化。
    /// </summary>
    [Operator("batch_norm")]
    Term<T> BatchNorm(Term<T> input, Term<T> scale, Term<T> bias, Term<T> runningMean, Term<T> runningVar,
        double epsilon);

    /// <summary>
    ///     ReLU 激活。
    /// </summary>
    [Operator("relu")]
    Term<T> Relu(Term<T> input);

    /// <summary>
    ///     Sigmoid 激活。
    /// </summary>
    [Operator("sigmoid")]
    Term<T> Sigmoid(Term<T> input);

    /// <summary>
    ///     Tanh 激活。
    /// </summary>
    [Operator("tanh")]
    Term<T> Tanh(Term<T> input);

    /// <summary>
    ///     Softmax。
    /// </summary>
    [Operator("softmax")]
    Term<T> Softmax(Term<T> input, int axis);

    /// <summary>
    ///     张量重塑。
    /// </summary>
    [Operator("reshape")]
    Term<T> Reshape(Term<T> input, IReadOnlyList<int> shape);

    /// <summary>
    ///     张量转置。
    /// </summary>
    [Operator("transpose")]
    Term<T> Transpose(Term<T> input, IReadOnlyList<int> perm);

    /// <summary>
    ///     张量拼接。
    /// </summary>
    [Operator("concat")]
    Term<T> Concat(IReadOnlyList<Term<T>> inputs, int axis);

    /// <summary>
    ///     张量切片。
    /// </summary>
    [Operator("slice")]
    Term<T> Slice(Term<T> input, IReadOnlyList<(int Start, int End, int Step)> ranges);

    /// <summary>
    ///     融合卷积归一化激活。
    /// </summary>
    [Operator("fused_conv_bn_relu")]
    Term<T> FusedConvBnRelu(
        Term<T> input,
        Term<T> weights,
        Term<T>? bias,
        Term<T> scale,
        Term<T> bnBias,
        Term<T> runningMean,
        Term<T> runningVar,
        double epsilon);

    /// <summary>
    ///     梯度求解。
    /// </summary>
    [Operator("grad")]
    Term<T> Grad(Term<T> loss, IReadOnlyList<Term<T>> parameters);

    /// <summary>
    ///     优化器步进。
    /// </summary>
    [Operator("optimizer_step")]
    Term<T> OptimizerStep(string optimizerType, IReadOnlyList<Term<T>> parameters, IReadOnlyList<Term<T>> gradients);

    /// <summary>
    ///     损失函数。
    /// </summary>
    [Operator("loss")]
    Term<T> Loss(LossType type, Term<T> predicted, Term<T> target);

    /// <summary>
    ///     融合注意力。
    /// </summary>
    [Operator("fused_attention")]
    Term<T> FusedAttention(
        Term<T> query,
        Term<T> key,
        Term<T> value,
        Term<T>? attentionMask,
        int heads,
        int headDim,
        double? dropoutRate);

    /// <summary>
    ///     RMS 归一化。
    /// </summary>
    [Operator("rms_norm")]
    Term<T> RmsNorm(Term<T> input, Term<T> weight, double epsilon);

    /// <summary>
    ///     层归一化。
    /// </summary>
    [Operator("layer_norm")]
    Term<T> LayerNorm(Term<T> input, Term<T> weight, Term<T> bias, double epsilon);

    /// <summary>
    ///     嵌入查表。
    /// </summary>
    [Operator("embedding")]
    Term<T> Embedding(Term<T> input, Term<T> weights);

    /// <summary>
    ///     旋转位置编码。
    /// </summary>
    [Operator("rotary_position_encoding")]
    Term<T> RotaryPositionEncoding(Term<T> input, Term<T> positionIds, int dim, double theta);

    /// <summary>
    ///     SiLU 激活。
    /// </summary>
    [Operator("silu")]
    Term<T> Silu(Term<T> input);

    /// <summary>
    ///     GELU 激活。
    /// </summary>
    [Operator("gelu")]
    Term<T> Gelu(Term<T> input);

    /// <summary>
    ///     LoRA 适配器。
    /// </summary>
    [Operator("lora")]
    Term<T> LoRA(Term<T> input, Term<T> weights, Term<T> downProjection, Term<T> upProjection, double alpha);

    /// <summary>
    ///     梯度检查点。
    /// </summary>
    [Operator("checkpoint")]
    Term<T> Checkpoint(Term<T> input, IReadOnlyList<Term<T>> segments);

    /// <summary>
    ///     KV Cache 管理。
    /// </summary>
    [Operator("kv_cache")]
    Term<T> KvCache(Term<T> key, Term<T> value, Term<T>? cacheKey, Term<T>? cacheValue, int seqLen);

    /// <summary>
    ///     类型转换。
    /// </summary>
    [Operator("cast")]
    Term<T> Cast(Term<T> input, string targetDtype);

    /// <summary>
    ///     Dropout。
    /// </summary>
    [Operator("dropout")]
    Term<T> Dropout(Term<T> input, double rate);

    /// <summary>
    ///     展平张量。
    /// </summary>
    [Operator("flatten")]
    Term<T> Flatten(Term<T> input, int startDim);

    /// <summary>
    ///     逐元素加法。
    /// </summary>
    [Operator("element_wise_add")]
    Term<T> ElementWiseAdd(Term<T> left, Term<T> right);

    /// <summary>
    ///     逐元素乘法。
    /// </summary>
    [Operator("element_wise_mul")]
    Term<T> ElementWiseMul(Term<T> left, Term<T> right);
}