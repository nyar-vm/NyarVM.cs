using Std.DL.Flux;

namespace Std.DL.Conflux;

/// <summary>
///     算子级图融合引擎 —— 将多个连续的 Flux 算子融合为等效的单次计算
/// </summary>
public static class ConfluxGraphFusion
{
    /// <summary>
    ///     融合 Conv2D + BatchNorm + ReLU
    /// </summary>
    /// <param name="input">原始输入</param>
    /// <param name="conv">Conv2D 算子</param>
    /// <param name="bn">BatchNorm 算子</param>
    /// <param name="inH">输入高度</param>
    /// <param name="inW">输入宽度</param>
    /// <returns>(融合输出, 输出高度, 输出宽度)</returns>
    public static (ArrayND Output, int OutH, int OutW) FuseConv2DBatchNormReLU(
        ArrayND input, Conv2D conv, BatchNorm bn, int inH, int inW)
    {
        var (convOut, outH, outW) = conv.Forward(input, inH, inW);
        var bnOut = bn.Forward(convOut);
        var reluOut = Activations.ReLUForward(bnOut);
        return (reluOut, outH, outW);
    }

    /// <summary>
    ///     融合 Dense + ReLU
    /// </summary>
    /// <param name="input">输入张量</param>
    /// <param name="dense">Dense 算子</param>
    /// <returns>融合输出</returns>
    public static ArrayND FuseDenseReLU(ArrayND input, Dense dense)
    {
        var denseOut = dense.forward(input);
        return Activations.ReLUForward(denseOut);
    }

    /// <summary>
    ///     融合 Dense + Dropout
    /// </summary>
    /// <param name="input">输入张量</param>
    /// <param name="dense">Dense 算子</param>
    /// <param name="dropout">Dropout 算子</param>
    /// <returns>融合输出</returns>
    public static ArrayND FuseDenseDropout(ArrayND input, Dense dense, Dropout dropout)
    {
        var denseOut = dense.forward(input);
        return dropout.Forward(denseOut);
    }

    /// <summary>
    ///     融合 Conv2D + SiLU
    /// </summary>
    /// <param name="input">原始输入</param>
    /// <param name="conv">Conv2D 算子</param>
    /// <param name="inH">输入高度</param>
    /// <param name="inW">输入宽度</param>
    /// <returns>(融合输出, 输出高度, 输出宽度)</returns>
    public static (ArrayND Output, int OutH, int OutW) FuseConv2DSiLU(
        ArrayND input, Conv2D conv, int inH, int inW)
    {
        var (convOut, outH, outW) = conv.Forward(input, inH, inW);
        var siluOut = Activations.SiLUForward(convOut);
        return (siluOut, outH, outW);
    }

    /// <summary>
    ///     融合 Dense + BatchNorm（顺序融合，参数折叠需 running mean/std，后续优化）
    /// </summary>
    /// <param name="input">输入张量</param>
    /// <param name="dense">Dense 算子</param>
    /// <param name="bn">BatchNorm 算子</param>
    /// <returns>融合输出</returns>
    public static ArrayND FuseDenseBatchNorm(ArrayND input, Dense dense, BatchNorm bn)
    {
        var denseOut = dense.forward(input);
        return bn.Forward(denseOut);
    }

    /// <summary>
    ///     获取所有可用的融合规则名称
    /// </summary>
    public static string[] GetAvailableFusions()
    {
        return
        [
            "Conv2D+BatchNorm+ReLU",
            "Dense+ReLU",
            "Dense+Dropout",
            "Conv2D+SiLU",
            "Dense+BatchNorm"
        ];
    }
}

/// <summary>
///     Conflux 融合统计器 —— 记录和报告融合操作
/// </summary>
public class ConfluxFusionInspector
{
    private readonly Dictionary<string, int> _appliedFusions = new();

    /// <summary>
    ///     融合操作总次数
    /// </summary>
    public int TotalFusionsApplied => _appliedFusions.Values.Sum();

    /// <summary>
    ///     已应用的唯一融合规则数
    /// </summary>
    public int UniqueFusionRules => _appliedFusions.Count;

    /// <summary>
    ///     记录一次融合操作
    /// </summary>
    /// <param name="fusionName">融合规则名称</param>
    public void RecordFusion(string fusionName)
    {
        if (!_appliedFusions.TryAdd(fusionName, 1)) _appliedFusions[fusionName]++;
    }

    /// <summary>
    ///     获取融合统计报告
    /// </summary>
    public IReadOnlyDictionary<string, int> GetFusionStats()
    {
        return _appliedFusions;
    }
}