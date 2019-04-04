namespace Std.DL.Flux;

/// <summary>
///     Token 嵌入层 —— 将整数 token ID 映射为稠密向量
/// </summary>
public class Embedding : ILayer
{
    private readonly int _embeddingDim;
    private readonly int _numEmbeddings;

    /// <summary>
    ///     创建嵌入层
    /// </summary>
    /// <param name="numEmbeddings">词汇表大小</param>
    /// <param name="embeddingDim">嵌入维度</param>
    public Embedding(int numEmbeddings, int embeddingDim)
    {
        _numEmbeddings = numEmbeddings;
        _embeddingDim = embeddingDim;
        Weight = ArrayND.HeNormal(embeddingDim, numEmbeddings, embeddingDim);
    }

    /// <summary>
    ///     嵌入权重表 [numEmbeddings, embeddingDim]
    /// </summary>
    public ArrayND Weight { get; }

    /// <summary>
    ///     获取可训练参数
    /// </summary>
    public IEnumerable<IParameter> Parameters()
    {
        yield return new Parameter(Weight);
    }

    /// <summary>
    ///     前向：根据 token ID 查找嵌入向量
    /// </summary>
    /// <param name="tokenIds">token ID 数组 [batch, seqLen]</param>
    /// <returns>嵌入向量 [batch, seqLen, embeddingDim]</returns>
    public ArrayND Forward(ArrayND tokenIds)
    {
        var batch = tokenIds.Shape[0];
        var seqLen = tokenIds.Shape[1];
        var result = ArrayND.Zeros(batch, seqLen, _embeddingDim);
        var spanW = Weight.AsSpan();
        var spanIds = tokenIds.AsSpan();
        var spanOut = result.AsWriteSpan();

        for (var b = 0; b < batch; b++)
        for (var s = 0; s < seqLen; s++)
        {
            var tokenId = (int)spanIds[b * seqLen + s];
            if (tokenId < 0 || tokenId >= _numEmbeddings)
                throw new ArgumentOutOfRangeException(nameof(tokenIds), $"Token ID {tokenId} 越界 [0, {_numEmbeddings})");
            var wOff = tokenId * _embeddingDim;
            var oOff = (b * seqLen + s) * _embeddingDim;
            for (var d = 0; d < _embeddingDim; d++) spanOut[oOff + d] = spanW[wOff + d];
        }

        return result;
    }

    /// <summary>
    ///     带自动微分的前向
    /// </summary>
    public ArrayND Forward(ArrayND tokenIds, AutogradContext ctx)
    {
        var output = Forward(tokenIds);

        ctx.Record(output, [Weight, tokenIds], outputGrads =>
        {
            var dOutput = outputGrads[0];
            var dWeight = ArrayND.Zeros(Weight.Shape);
            var spanDOut = dOutput.AsSpan();
            var spanDW = dWeight.AsWriteSpan();
            var spanIds = tokenIds.AsSpan();
            var batch = tokenIds.Shape[0];
            var seqLen = tokenIds.Shape[1];

            for (var b = 0; b < batch; b++)
            for (var s = 0; s < seqLen; s++)
            {
                var tokenId = (int)spanIds[b * seqLen + s];
                var wOff = tokenId * _embeddingDim;
                var oOff = (b * seqLen + s) * _embeddingDim;
                for (var d = 0; d < _embeddingDim; d++) spanDW[wOff + d] += spanDOut[oOff + d];
            }

            return [dWeight, null];
        });

        return output;
    }
}