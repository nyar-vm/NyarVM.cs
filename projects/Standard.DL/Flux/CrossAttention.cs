namespace Std.DL.Flux;

/// <summary>
///     Cross-Attention —— Stable Diffusion 文本条件注入
///     Q 来自潜在表示（图像），K/V 来自文本编码
/// </summary>
public static class CrossAttention
{
    /// <summary>
    ///     Cross-Attention：Q 来自 latent，K/V 来自 context
    ///     Scores = softmax(Q_latent × K_context^T / sqrt(dK), axis=-1); Output = Scores × V_context
    /// </summary>
    /// <param name="q">来自潜在表示的查询 [..., seqLenQ, dK]</param>
    /// <param name="k">来自文本上下文的键 [..., seqLenK, dK]</param>
    /// <param name="v">来自文本上下文的值 [..., seqLenK, dV]</param>
    /// <returns>注意力输出 [..., seqLenQ, dV]</returns>
    public static ArrayND Forward(ArrayND q, ArrayND k, ArrayND v)
    {
        return Attention.ScaledDotProductAttention(q, k, v);
    }
}

/// <summary>
///     Cross-Attention 层 —— 完整的 Cross-Attention 模块
/// </summary>
public class CrossAttentionLayer : ILayer
{
    private readonly int _dContext;
    private readonly int _dK;
    private readonly int _dLatent;
    private readonly Dense _kProj;
    private readonly int _numHeads;
    private readonly Dense _outProj;
    private readonly Dense _qProj;
    private readonly Dense _vProj;

    /// <summary>
    ///     创建 Cross-Attention 层
    /// </summary>
    /// <param name="dLatent">潜在表示维度（Q 来源）</param>
    /// <param name="dContext">文本上下文维度（K/V 来源）</param>
    /// <param name="numHeads">注意力头数</param>
    public CrossAttentionLayer(int dLatent, int dContext, int numHeads)
    {
        _dLatent = dLatent;
        _dContext = dContext;
        _numHeads = numHeads;
        _dK = dLatent / numHeads;
        _qProj = new Dense(dLatent, dLatent);
        _kProj = new Dense(dContext, dLatent);
        _vProj = new Dense(dContext, dLatent);
        _outProj = new Dense(dLatent, dLatent);
    }

    /// <summary>
    ///     获取可训练参数
    /// </summary>
    public IEnumerable<IParameter> Parameters()
    {
        return _qProj.Parameters()
            .Concat(_kProj.Parameters())
            .Concat(_vProj.Parameters())
            .Concat(_outProj.Parameters());
    }

    /// <summary>
    ///     前向传播
    /// </summary>
    /// <param name="latent">潜在表示 [batch, seqLenQ, dLatent]</param>
    /// <param name="context">文本上下文 [batch, seqLenK, dContext]</param>
    /// <returns>注意力输出 [batch, seqLenQ, dLatent]</returns>
    public ArrayND Forward(ArrayND latent, ArrayND context)
    {
        var batch = latent.Shape[0];
        var seqLenQ = latent.Shape[1];
        var seqLenK = context.Shape[1];

        var q = _qProj.forward(latent).Reshape(batch * _numHeads, seqLenQ, _dK);
        var k = _kProj.forward(context).Reshape(batch * _numHeads, seqLenK, _dK);
        var v = _vProj.forward(context).Reshape(batch * _numHeads, seqLenK, _dK);

        var attnOut = Attention.ScaledDotProductAttention(q, k, v);
        attnOut = attnOut.Reshape(batch, _numHeads, seqLenQ, _dK).Transpose(1, 2);
        attnOut = attnOut.Reshape(batch, seqLenQ, _dLatent);

        return _outProj.forward(attnOut);
    }
}