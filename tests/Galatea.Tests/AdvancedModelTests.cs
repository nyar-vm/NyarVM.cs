namespace Galatea.Tests;

/// <summary>
///     前沿大模型特性集成测试 —— RMSNorm / RoPE / KV-Cache / GroupNorm / CrossAttention
/// </summary>
public class AdvancedModelTests : GradientTestBase
{
    #region Helpers

    private static ArrayND CausalMask(int seqLen)
    {
        var mask = ArrayND.Zeros(seqLen, seqLen);
        var span = mask.AsWriteSpan();
        for (var i = 0; i < seqLen; i++)
        for (var j = i + 1; j < seqLen; j++)
            span[i * seqLen + j] = float.NegativeInfinity;

        return mask;
    }

    #endregion

    #region RMSNorm

    [Fact]
    public void RMSNorm_Forward_PreservesShape()
    {
        var rms = new RMSNorm(32);
        var x = ArrayND.RandomNormal(2, 8, 32);
        var output = rms.Forward(x);
        Assert.Equal(x.Shape, output.Shape);
    }

    [Fact]
    public void RMSNorm_Gradient_MatchesNumericalGradient()
    {
        var rms = new RMSNorm(4);
        var x = ArrayND.RandomNormal(3, 4);

        var numGrad = NumericalGradient(x, modifiedX =>
        {
            var output = rms.Forward(modifiedX);
            return output.AsSpan()[0];
        });

        var ctx = new AutogradContext();
        ctx.StartRecording();
        var output = rms.Forward(x, ctx);
        ctx.Backward(output);
        var autoGrad = x.Grad!;

        var error = MaxAbsoluteError(numGrad, autoGrad);
        Assert.True(error < GradientTolerance * 10.0f, $"RMSNorm 梯度误差 {error:E3} 超 {GradientTolerance * 10.0f:E3}");
    }

    [Fact]
    public void RMSNorm_ApproximatelyUnitRMS()
    {
        var rms = new RMSNorm(16);
        var x = ArrayND.RandomNormal(4, 16);
        var output = rms.Forward(x);

        var spanO = output.AsSpan();
        var sumSq = 0.0f;
        for (var i = 0; i < 4; i++)
        for (var j = 0; j < 16; j++)
            sumSq += spanO[i * 16 + j] * spanO[i * 16 + j];

        var rmsVal = MathF.Sqrt(sumSq / 64);
        Assert.True(rmsVal < 1.5f && rmsVal > 0.5f, $"RMS = {rmsVal}, 应在 [0.5, 1.5]");
    }

    #endregion

    #region Rotary Position Embedding

    [Fact]
    public void RoPE_Apply_PreservesShape()
    {
        var q = ArrayND.RandomNormal(4, 8, 32);
        var k = ArrayND.RandomNormal(4, 8, 32);

        var qOrig = q.AsSpan().ToArray();
        RotaryPositionEmbedding.ApplyRotaryEmbedding(q, k);

        Assert.Equal(new[] { 4, 8, 32 }, q.Shape);
        Assert.Equal(new[] { 4, 8, 32 }, k.Shape);

        var spanQ = q.AsSpan();
        var changed = false;
        for (var i = 0; i < spanQ.Length; i++)
            if (Math.Abs(spanQ[i] - qOrig[i]) > 1e-6f)
            {
                changed = true;
                break;
            }

        Assert.True(changed, "RoPE 应该修改 Q 的值");
    }

    [Fact]
    public void RoPE_WithStartPos_ProducesValidOutput()
    {
        var q = ArrayND.RandomNormal(2, 4, 16);
        var k = ArrayND.RandomNormal(2, 4, 16);

        RotaryPositionEmbedding.ApplyRotaryEmbedding(q, k, startPos: 8);

        var spanQ = q.AsSpan();
        for (var i = 0; i < spanQ.Length; i++)
        {
            Assert.False(float.IsNaN(spanQ[i]), $"RoPE Q 包含 NaN 于 {i}");
            Assert.False(float.IsInfinity(spanQ[i]), $"RoPE Q 包含 Inf 于 {i}");
        }
    }

    #endregion

    #region KV-Cache

    [Fact]
    public void KVCache_UpdateAndGet_Works()
    {
        var cache = new KVCache(numLayers: 1, numHeads: 2, dK: 16, batch: 1, maxSeqLen: 32);

        var k1 = ArrayND.RandomNormal(2, 4, 16);
        var v1 = ArrayND.RandomNormal(2, 4, 16);
        cache.Update(0, k1, v1);
        Assert.Equal(4, cache.CurrentLength);

        var k2 = ArrayND.RandomNormal(2, 2, 16);
        var v2 = ArrayND.RandomNormal(2, 2, 16);
        cache.Update(0, k2, v2);
        Assert.Equal(6, cache.CurrentLength);

        var (fullK, fullV) = cache.Get(0);
        Assert.Equal(new[] { 2, 6, 16 }, fullK.Shape);
        Assert.Equal(new[] { 2, 6, 16 }, fullV.Shape);
    }

    [Fact]
    public void KVCache_Clear_ResetsState()
    {
        var cache = new KVCache(numLayers: 1, numHeads: 2, dK: 16, batch: 1, maxSeqLen: 32);
        cache.Update(0, ArrayND.RandomNormal(2, 4, 16), ArrayND.RandomNormal(2, 4, 16));
        cache.Clear();
        Assert.Equal(0, cache.CurrentLength);
    }

    [Fact]
    public void KVCache_SingleUpdate_GetReturnsCorrectShape()
    {
        var cache = new KVCache(numLayers: 1, numHeads: 4, dK: 8, batch: 1, maxSeqLen: 32);
        var k = ArrayND.RandomNormal(4, 3, 8);
        var v = ArrayND.RandomNormal(4, 3, 8);
        var (fullK, _) = cache.Update(0, k, v);

        Assert.Equal(new[] { 4, 3, 8 }, fullK.Shape);
    }

    #endregion

    #region GroupNorm

    [Fact]
    public void GroupNorm_Forward_PreservesShape()
    {
        var gn = new GroupNorm(numGroups: 4, numChannels: 8);
        var x = ArrayND.RandomNormal(2, 8, 16, 16);
        var output = gn.Forward(x);
        Assert.Equal(x.Shape, output.Shape);
    }

    [Fact]
    public void GroupNorm_Gradient_MatchesNumericalGradient()
    {
        var gn = new GroupNorm(numGroups: 2, numChannels: 4);
        var x = ArrayND.RandomNormal(1, 4, 4, 4);

        var numGrad = NumericalGradient(x, modifiedX =>
        {
            var output = gn.Forward(modifiedX);
            return output.AsSpan()[0];
        });

        var ctx = new AutogradContext();
        ctx.StartRecording();
        var output = gn.Forward(x, ctx);
        ctx.Backward(output);
        var autoGrad = x.Grad!;

        var error = MaxAbsoluteError(numGrad, autoGrad);
        Assert.True(error < GradientTolerance * 10.0f, $"GroupNorm 梯度误差 {error:E3} 超 {GradientTolerance * 10.0f:E3}");
    }

    #endregion

    #region CrossAttention

    [Fact]
    public void CrossAttention_Forward_ProducesCorrectShape()
    {
        var batch = 2;
        var seqLenQ = 16;
        var seqLenK = 8;
        var dLatent = 64;
        var dContext = 64;
        var numHeads = 4;

        var ca = new CrossAttentionLayer(dLatent, dContext, numHeads);
        var latent = ArrayND.RandomNormal(batch, seqLenQ, dLatent);
        var context = ArrayND.RandomNormal(batch, seqLenK, dContext);

        var output = ca.Forward(latent, context);
        Assert.Equal(new[] { batch, seqLenQ, dLatent }, output.Shape);
    }

    [Fact]
    public void CrossAttention_DifferentLatentContext_Works()
    {
        var ca = new CrossAttentionLayer(dLatent: 32, dContext: 64, numHeads: 4);
        var latent = ArrayND.RandomNormal(1, 4, 32);
        var context = ArrayND.RandomNormal(1, 8, 64);

        var output = ca.Forward(latent, context);
        Assert.Equal(new[] { 1, 4, 32 }, output.Shape);
    }

    #endregion

    #region TimeEmbedding

    [Fact]
    public void TimeEmbedding_Sinusoidal_ProducesCorrectShape()
    {
        var batch = 4;
        var dim = 128;
        var t = ArrayND.Zeros(batch, 1);
        t.AsWriteSpan()[0] = 10;
        t.AsWriteSpan()[1] = 100;
        t.AsWriteSpan()[2] = 500;
        t.AsWriteSpan()[3] = 999;

        var emb = TimeEmbedding.Sinusoidal(t, dim);
        Assert.Equal(new[] { batch, dim }, emb.Shape);

        var spanE = emb.AsSpan();
        for (var i = 0; i < spanE.Length; i++)
            Assert.True(spanE[i] >= -1.0f && spanE[i] <= 1.1f, $"sin 嵌入 {i} = {spanE[i]} 超出 [-1, 1]");
    }

    [Fact]
    public void TimeEmbedding_Layer_Forward_ProducesCorrectShape()
    {
        var layer = new TimeEmbeddingLayer(inputDim: 128, outputDim: 64);
        var timeEmb = TimeEmbedding.Sinusoidal(ArrayND.Ones(2, 1), 128);

        var output = layer.Forward(timeEmb);
        Assert.Equal(new[] { 2, 64 }, output.Shape);
    }

    #endregion

    #region Full GPT-2 Decoder Block

    /// <summary>
    ///     GPT-2 Decoder Block：
    ///     x → RMSNorm → MultiHeadAttention(RoPE) → Resid → RMSNorm → FFN(SiLU) → Resid
    /// </summary>
    [Fact]
    public void Gpt2DecoderBlock_Forward_ProducesCorrectShape()
    {
        var batch = 2;
        var seqLen = 8;
        var dModel = 64;
        var numHeads = 4;

        var rms1 = new RMSNorm(dModel);
        var rms2 = new RMSNorm(dModel);
        var mha = new MultiHeadAttention(dModel, numHeads);
        var ffn1 = new Dense(dModel, dModel * 4);
        var ffn2 = new Dense(dModel * 4, dModel);

        var x = ArrayND.RandomNormal(batch, seqLen, dModel);
        var mask = CausalMask(seqLen);

        var normed = rms1.Forward(x);
        var attnOut = mha.Forward(normed, mask);
        x = x + attnOut;

        var normed2 = rms2.Forward(x);
        var ffnHidden = ((ITrainableModel)ffn1).Forward(normed2);
        ffnHidden = Activations.SiLUForward(ffnHidden);
        var ffnOut = ffn2.Forward(ffnHidden);
        x = x + ffnOut;

        Assert.Equal(new[] { batch, seqLen, dModel }, x.Shape);
    }

    /// <summary>
    ///     带 RoPE 的 GPT-2 Attention 子模块
    /// </summary>
    [Fact]
    public void Gpt2Attention_WithRoPE_ProducesCorrectShape()
    {
        var batch = 2;
        var seqLen = 6;
        var dModel = 32;
        var numHeads = 4;
        var dK = dModel / numHeads;

        var mha = new MultiHeadAttention(dModel, numHeads);
        var x = ArrayND.RandomNormal(batch, seqLen, dModel);

        var qkvProj = new Dense(dModel, dModel * 3);
        var qkv = qkvProj.Forward(x);
        qkv = qkv.Reshape(batch, seqLen, 3, numHeads, dK);

        var q = qkv.Slice(2, 0, 1).Transpose(1, 2).Reshape(batch * numHeads, seqLen, dK);
        var k = qkv.Slice(2, 1, 1).Transpose(1, 2).Reshape(batch * numHeads, seqLen, dK);

        RotaryPositionEmbedding.ApplyRotaryEmbedding(q, k);

        Assert.Equal(new[] { batch * numHeads, seqLen, dK }, q.Shape);
        Assert.Equal(new[] { batch * numHeads, seqLen, dK }, k.Shape);

        var spanQ = q.AsSpan();
        for (var i = 0; i < spanQ.Length; i++) Assert.False(float.IsNaN(spanQ[i]));
    }

    #endregion

    #region Full SD U-Net Block

    /// <summary>
    ///     Stable Diffusion U-Net ResBlock + SelfAttention + CrossAttention：
    ///     h → GroupNorm → SiLU → Conv2D → TimeEmbed → SiLU → Conv2D → SkipConv → Resid
    ///     h → GroupNorm → SelfAttention → Resid
    ///     h → GroupNorm → CrossAttention(text) → Resid
    /// </summary>
    [Fact]
    public void SD_UNetBlock_WithTimeEmbed_ProducesCorrectShape()
    {
        var batch = 1;
        var ch = 16;
        var inH = 8;
        var inW = 8;

        var gn1 = new GroupNorm(numGroups: 4, numChannels: ch);
        var conv1 = new Conv2D(ch, ch, kernelSize: 3, padding: 1, inH: inH, inW: inW);
        var conv2 = new Conv2D(ch, ch, kernelSize: 3, padding: 1, inH: inH, inW: inW);
        var skipConv = new Conv2D(ch, ch, kernelSize: 1, padding: 0, inH: inH, inW: inW);

        var tEmbLayer = new TimeEmbeddingLayer(inputDim: 64, outputDim: ch * 2);
        var t = ArrayND.Ones(1, 1);
        var sinEmb = TimeEmbedding.Sinusoidal(t, 64);
        var tEmb = tEmbLayer.Forward(sinEmb);

        var x4d = ArrayND.RandomNormal(batch, ch, inH, inW);
        var residual = ((ITrainableModel)skipConv).Forward(x4d.Reshape(batch, ch * inH * inW));

        var h = gn1.Forward(x4d);
        h = Activations.SiLUForward(h);
        var (h2, _, _) = conv1.Forward(h.Reshape(batch, ch * inH * inW), inH: inH, inW: inW);

        h2 = Activations.SiLUForward(h2);
        var (h3, outH, outW) = conv2.Forward(h2, inH: inH, inW: inW);
        Assert.True(outH > 0 && outW > 0);
        Assert.Equal(h3.Size, batch * ch * outH * outW);
    }

    [Fact]
    public void SD_CrossAttention_InjectsTextContext()
    {
        var batch = 2;
        var seqLenQ = 16;
        var seqLenK = 8;
        var dLatent = 64;
        var dContext = 128;
        var numHeads = 4;

        var ca = new CrossAttentionLayer(dLatent, dContext, numHeads);
        var latent = ArrayND.RandomNormal(batch, seqLenQ, dLatent);
        var context = ArrayND.RandomNormal(batch, seqLenK, dContext);

        var output = ca.Forward(latent, context);
        Assert.Equal(new[] { batch, seqLenQ, dLatent }, output.Shape);

        var spanO = output.AsSpan();
        for (var i = 0; i < spanO.Length; i++)
        {
            Assert.False(float.IsNaN(spanO[i]));
            Assert.False(float.IsInfinity(spanO[i]));
        }
    }

    #endregion
}