namespace Std.DL.Flux;

/// <summary>
///     填充掩码工具 —— 处理变长序列的 padding mask + causal mask 组合
///     用于 LLM 训练中批量处理不同长度的序列
/// </summary>
public static class PaddingMask
{
    /// <summary>
    ///     从序列长度数组创建填充掩码
    ///     padding 位置为 -inf，有效位置为 0
    /// </summary>
    /// <param name="seqLengths">每个序列的实际长度 [batch]</param>
    /// <param name="maxSeqLen">最大序列长度</param>
    /// <returns>填充掩码 [batch, 1, 1, maxSeqLen]</returns>
    public static ArrayND Create(ArrayND seqLengths, int maxSeqLen)
    {
        var batch = seqLengths.Shape[0];
        var mask = ArrayND.Zeros(batch, 1, 1, maxSeqLen);
        var spanLens = seqLengths.AsSpan();
        var spanMask = mask.AsWriteSpan();

        for (var n = 0; n < batch; n++)
        {
            var len = (int)spanLens[n];
            for (var s = len; s < maxSeqLen; s++) spanMask[n * maxSeqLen + s] = float.NegativeInfinity;
        }

        return mask;
    }

    /// <summary>
    ///     创建因果掩码 + 填充掩码的组合
    ///     同时满足：1) 不能看到未来 token（因果）2) 不能看到 padding token
    /// </summary>
    /// <param name="seqLengths">每个序列的实际长度 [batch]</param>
    /// <param name="maxSeqLen">最大序列长度</param>
    /// <returns>组合掩码 [batch, 1, maxSeqLen, maxSeqLen]</returns>
    public static ArrayND CreateCausalWithPadding(ArrayND seqLengths, int maxSeqLen)
    {
        var batch = seqLengths.Shape[0];
        var mask = ArrayND.Zeros(batch, 1, maxSeqLen, maxSeqLen);
        var spanLens = seqLengths.AsSpan();
        var spanMask = mask.AsWriteSpan();

        for (var n = 0; n < batch; n++)
        {
            var len = (int)spanLens[n];

            for (var i = 0; i < maxSeqLen; i++)
            for (var j = 0; j < maxSeqLen; j++)
            {
                var isCausal = j > i;
                var isPadding = i >= len || j >= len;
                if (isCausal || isPadding)
                    spanMask[n * maxSeqLen * maxSeqLen + i * maxSeqLen + j] = float.NegativeInfinity;
            }
        }

        return mask;
    }

    /// <summary>
    ///     将 2D 因果掩码 [seqLen, seqLen] 扩展为 4D [batch, 1, seqLen, seqLen]
    /// </summary>
    /// <param name="causalMask">2D 因果掩码 [seqLen, seqLen]</param>
    /// <param name="batch">批次大小</param>
    /// <returns>4D 掩码 [batch, 1, seqLen, seqLen]</returns>
    public static ArrayND ExpandCausalMask(ArrayND causalMask, int batch)
    {
        var seqLen = causalMask.Shape[0];
        var result = ArrayND.Zeros(batch, 1, seqLen, seqLen);
        var spanSrc = causalMask.AsSpan();
        var spanDst = result.AsWriteSpan();

        for (var n = 0; n < batch; n++)
        {
            var dstOff = n * seqLen * seqLen;
            for (var i = 0; i < seqLen * seqLen; i++) spanDst[dstOff + i] = spanSrc[i];
        }

        return result;
    }

    /// <summary>
    ///     从 token ID 创建填充掩码（padding token ID 对应的位置为 -inf）
    /// </summary>
    /// <param name="inputIds">token ID [batch, seqLen]</param>
    /// <param name="padTokenId">填充 token 的 ID</param>
    /// <returns>填充掩码 [batch, 1, 1, seqLen]</returns>
    public static ArrayND FromInputIds(ArrayND inputIds, int padTokenId)
    {
        var batch = inputIds.Shape[0];
        var seqLen = inputIds.Shape[1];
        var mask = ArrayND.Zeros(batch, 1, 1, seqLen);
        var spanIds = inputIds.AsSpan();
        var spanMask = mask.AsWriteSpan();

        for (var n = 0; n < batch; n++)
        for (var s = 0; s < seqLen; s++)
            if ((int)spanIds[n * seqLen + s] == padTokenId)
                spanMask[n * seqLen + s] = float.NegativeInfinity;

        return mask;
    }

    /// <summary>
    ///     从 token ID 创建因果 + 填充组合掩码
    /// </summary>
    /// <param name="inputIds">token ID [batch, seqLen]</param>
    /// <param name="padTokenId">填充 token 的 ID</param>
    /// <returns>组合掩码 [batch, 1, seqLen, seqLen]</returns>
    public static ArrayND CreateCausalFromInputIds(ArrayND inputIds, int padTokenId)
    {
        var batch = inputIds.Shape[0];
        var seqLen = inputIds.Shape[1];
        var mask = ArrayND.Zeros(batch, 1, seqLen, seqLen);
        var spanIds = inputIds.AsSpan();
        var spanMask = mask.AsWriteSpan();

        for (var n = 0; n < batch; n++)
        for (var i = 0; i < seqLen; i++)
        {
            var isRowPad = (int)spanIds[n * seqLen + i] == padTokenId;

            for (var j = 0; j < seqLen; j++)
            {
                var isCausal = j > i;
                var isColPad = (int)spanIds[n * seqLen + j] == padTokenId;

                if (isCausal || isRowPad || isColPad)
                    spanMask[n * seqLen * seqLen + i * seqLen + j] = float.NegativeInfinity;
            }
        }

        return mask;
    }
}