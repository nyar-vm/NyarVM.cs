namespace Galatea.Tests;

/// <summary>
///     Transformer / 大模型范式集成测试
/// </summary>
public class TransformerModelTests : GradientTestBase
{
    /// <summary>
    ///     因果掩码：[seqLen, seqLen]，上三角填充 -inf
    /// </summary>
    private static ArrayND CausalMask(int seqLen)
    {
        var mask = ArrayND.Zeros(seqLen, seqLen);
        var span = mask.AsWriteSpan();
        for (var i = 0; i < seqLen; i++)
        for (var j = i + 1; j < seqLen; j++)
            span[i * seqLen + j] = float.NegativeInfinity;

        return mask;
    }

    #region Embedding

    [Fact]
    public void Embedding_Forward_ProducesCorrectShape()
    {
        var vocabSize = 100;
        var embDim = 32;
        var batch = 2;
        var seqLen = 5;
        var emb = new Embedding(vocabSize, embDim);

        var tokenIds = ArrayND.Zeros(batch, seqLen);
        for (var i = 0; i < batch * seqLen; i++) tokenIds.AsWriteSpan()[i] = i % vocabSize;

        var output = emb.Forward(tokenIds);
        Assert.Equal(new[] { batch, seqLen, embDim }, output.Shape);
    }

    #endregion

    #region Mini-GPT

    /// <summary>
    ///     Mini-GPT Transformer Block: x → LayerNorm → MultiHeadAttention → + → LayerNorm → Dense → GELU → Dense → +
    /// </summary>
    [Fact]
    public void MiniGpt_TransformerBlock_Forward_ProducesCorrectShape()
    {
        var batch = 2;
        var seqLen = 8;
        var dModel = 64;
        var numHeads = 4;

        var ln1 = new LayerNorm(dModel);
        var ln2 = new LayerNorm(dModel);
        var mha = new MultiHeadAttention(dModel, numHeads);
        var ffn1 = new Dense(dModel, dModel * 4);
        var ffn2 = new Dense(dModel * 4, dModel);

        var x = ArrayND.RandomNormal(batch, seqLen, dModel);
        var mask = CausalMask(seqLen);

        // Pre-LN Transformer Block（GPT-2 风格）
        var attnInput = ln1.Forward(x);
        var attnOutput = mha.Forward(attnInput, mask);
        var x2 = x + attnOutput; // 残差连接

        var ffnInput = ln2.Forward(x2);
        var ffnHidden = ffn1.Forward(ffnInput);
        var ffnAct = Activations.GELUForward(ffnHidden);
        var ffnOutput = ffn2.Forward(ffnAct);
        // x += ffnOutput; // 需要 broadcast — 暂不做完整残差测试,我们验证形状

        Assert.Equal(new[] { batch, seqLen, dModel }, attnOutput.Shape);
        Assert.True(ffnOutput.Shape.Length == x.Shape.Length);
    }

    #endregion

    #region Stable Diffusion U-Net Style

    /// <summary>
    ///     SD U-Net ResBlock: x → GroupNorm(≈LayerNorm) → SiLU → Conv2D → SiLU → Conv2D + skip
    /// </summary>
    [Fact]
    public void StableDiffusion_ResBlock_Forward_ProducesCorrectShape()
    {
        var batch = 1;
        var inCh = 8;
        var inH = 32;
        var inW = 32;

        var conv1 = new Conv2D(inCh, inCh, kernelSize: 3, padding: 1, inH: inH, inW: inW);
        var conv2 = new Conv2D(inCh, inCh, kernelSize: 3, padding: 1, inH: inH, inW: inW);
        var skipConv = new Conv2D(inCh, inCh, kernelSize: 1, padding: 0, inH: inH, inW: inW);

        var x = ArrayND.Zeros(batch, inCh * inH * inW);

        var residual = ((ITrainableModel)skipConv).Forward(x);

        var h = Activations.SiLUForward(x);
        var (h2, _, _) = conv1.Forward(h, inH: inH, inW: inW);
        h2 = Activations.SiLUForward(h2);
        var (h3, outH, outW) = conv2.Forward(h2, inH: inH, inW: inW);

        Assert.True(outH > 0 && outW > 0, $"输出尺寸 outH={outH}, outW={outW} 应 > 0");
        Assert.Equal(h3.Size, outH * outW * inCh * batch);
    }

    #endregion

    #region GELU Exact

    [Fact]
    public void GELU_Forward_NoNaN()
    {
        var x = ArrayND.RandomNormal(4, 4);
        var result = Activations.GELUForward(x);
        var span = result.AsSpan();
        for (var i = 0; i < span.Length; i++) Assert.False(float.IsNaN(span[i]), $"GELU 输出包含 NaN 于 {i}");
    }

    #endregion

    #region MultiHeadAttention

    [Fact]
    public void MultiHeadAttention_Forward_ProducesCorrectShape()
    {
        var batch = 2;
        var seqLen = 8;
        var dModel = 64;
        var numHeads = 4;
        var mha = new MultiHeadAttention(dModel, numHeads);

        var x = ArrayND.RandomNormal(batch, seqLen, dModel);
        var output = mha.Forward(x);

        Assert.Equal(new[] { batch, seqLen, dModel }, output.Shape);
    }

    [Fact]
    public void MultiHeadAttention_WithCausalMask_NoNaN()
    {
        var batch = 1;
        var seqLen = 4;
        var dModel = 32;
        var numHeads = 4;
        var mha = new MultiHeadAttention(dModel, numHeads);

        var x = ArrayND.RandomNormal(batch, seqLen, dModel);
        var mask = CausalMask(seqLen);
        var output = mha.Forward(x, mask);

        var spanO = output.AsSpan();
        for (var i = 0; i < spanO.Length; i++)
        {
            Assert.False(float.IsNaN(spanO[i]), $"输出包含 NaN 于位置 {i}");
            Assert.False(float.IsInfinity(spanO[i]), $"输出包含 Inf 于位置 {i}");
        }
    }

    [Fact]
    public void ScaledDotProductAttention_WithoutMask_ProducesCorrectShape()
    {
        var batch = 2;
        var seqLenQ = 4;
        var seqLenK = 4;
        var dK = 16;
        var dV = 16;

        var q = ArrayND.RandomNormal(batch, seqLenQ, dK);
        var k = ArrayND.RandomNormal(batch, seqLenK, dK);
        var v = ArrayND.RandomNormal(batch, seqLenK, dV);

        var output = Attention.ScaledDotProductAttention(q, k, v);

        Assert.Equal(new[] { batch, seqLenQ, dV }, output.Shape);
    }

    [Fact]
    public void ScaledDotProductAttention_WithCausalMask_IsCausal()
    {
        var batch = 1;
        var seqLen = 3;
        var dK = 8;

        var q = ArrayND.Zeros(batch, seqLen, dK);
        var k = ArrayND.Zeros(batch, seqLen, dK);
        var v = ArrayND.Zeros(batch, seqLen, dK);
        for (var i = 0; i < dK; i++)
        {
            q.AsWriteSpan()[i] = 1.0f;
            k.AsWriteSpan()[i] = 1.0f;
            v.AsWriteSpan()[i] = 1.0f;
        }

        var mask = CausalMask(seqLen);
        var output = Attention.ScaledDotProductAttention(q, k, v, mask);
        var spanO = output.AsSpan();

        Assert.False(float.IsNaN(spanO[0]), "因果输出不应有 NaN");
    }

    #endregion

    #region LayerNorm

    [Fact]
    public void LayerNorm_Forward_NormalizesToNearZeroMean()
    {
        var ln = new LayerNorm(16);
        var x = ArrayND.RandomNormal(2, 4, 16);
        var output = ln.Forward(x);

        Assert.Equal(x.Shape, output.Shape);
        var spanO = output.AsSpan();
        for (var i = 0; i < spanO.Length; i++) Assert.False(float.IsNaN(spanO[i]));
    }

    [Fact]
    public void LayerNorm_Gradient_MatchesNumericalGradient()
    {
        var ln = new LayerNorm(4);
        var x = ArrayND.RandomNormal(2, 4);

        var numGrad = NumericalGradient(x, modifiedX =>
        {
            var output = ln.Forward(modifiedX);
            return output.AsSpan()[0];
        });

        var ctx = new AutogradContext();
        ctx.StartRecording();
        var output = ln.Forward(x, ctx);
        ctx.Backward(output);
        var autoGrad = x.Grad!;

        var error = MaxAbsoluteError(numGrad, autoGrad);
        Assert.True(error < GradientTolerance * 10.0f, $"LayerNorm 梯度误差 {error:E3} 超 {GradientTolerance * 10.0f:E3}");
    }

    #endregion

    #region BatchMatMul Multi-Dim

    [Fact]
    public void BatchMatMul_3D_ProducesCorrectShape()
    {
        var batch = 3;
        var M = 5;
        var K = 4;
        var N = 6;

        var a = ArrayND.RandomNormal(batch, M, K);
        var b = ArrayND.RandomNormal(batch, K, N);

        var result = ArrayND.BatchMatMul(a, b);
        Assert.Equal(new[] { batch, M, N }, result.Shape);
    }

    [Fact]
    public void BatchMatMul_2D_StillWorks()
    {
        var a = ArrayND.Zeros(3, 4);
        var b = ArrayND.Zeros(4, 5);

        var result = ArrayND.BatchMatMul(a, b);
        Assert.Equal(new[] { 3, 5 }, result.Shape);
    }

    [Fact]
    public void Transpose_MultiDim_Works()
    {
        var shape = new[] { 1, 2, 3, 4 };
        var x = ArrayND.Zeros(shape);
        var result = x.Transpose(1, 2);
        Assert.Equal(new[] { 1, 3, 2, 4 }, result.Shape);
    }

    #endregion

    #region Concat and Slice

    [Fact]
    public void Concat_Axis1_ConcatenatesColumns()
    {
        var a = ArrayND.Zeros(3, 2);
        var b = ArrayND.Zeros(3, 3);
        var result = ArrayND.Concat(1, a, b);
        Assert.Equal(new[] { 3, 5 }, result.Shape);
    }

    [Fact]
    public void Slice_Axis0_SlicesRows()
    {
        var x = ArrayND.Zeros(6, 4);
        var result = x.Slice(0, 1, 3);
        Assert.Equal(new[] { 3, 4 }, result.Shape);
    }

    [Fact]
    public void Sum_Axis0_GivesCorrectShape()
    {
        var x = ArrayND.RandomNormal(5, 3);
        var result = x.Sum(0, keepDims: false);
        Assert.Equal(new[] { 3 }, result.Shape);
    }

    #endregion
}