using Std.DL.Training;

namespace Std.DL.Flux;

/// <summary>
///     SwiGLU 前馈网络 —— LLaMA / Gemma / Mistral 的标准 FFN
///     结构：output = W2(SiLU(W_gate(x)) ⊙ W_up(x))
///     相比标准 FFN（W2(ReLU(W1(x)))），SwiGLU 用门控机制显著提升性能
/// </summary>
public sealed class SwiGLUFFN : ITrainableModel
{
    private readonly int _dFF;
    private readonly int _dModel;

    /// <summary>
    ///     创建 SwiGLU 前馈网络
    /// </summary>
    /// <param name="dModel">模型维度</param>
    /// <param name="dFF">前馈维度（通常为 dModel * 8/3 并向上取整到 64 的倍数）</param>
    public SwiGLUFFN(int dModel, int dFF)
    {
        _dModel = dModel;
        _dFF = dFF;
        GateProj = new Dense(dModel, dFF);
        UpProj = new Dense(dModel, dFF);
        DownProj = new Dense(dFF, dModel);
    }

    /// <summary>门控投影 W_gate: [dModel, dFF]</summary>
    public Dense GateProj { get; }

    /// <summary>上投影 W_up: [dModel, dFF]</summary>
    public Dense UpProj { get; }

    /// <summary>下投影 W_down: [dFF, dModel]</summary>
    public Dense DownProj { get; }

    /// <summary>
    ///     前向传播：output = DownProj(SiLU(GateProj(x)) ⊙ UpProj(x))
    /// </summary>
    public ArrayND forward(ArrayND input)
    {
        var gate = GateProj.forward(input);
        var siluGate = Activations.SiLUForward(gate);
        var up = UpProj.forward(input);
        var gated = ElementWiseMul(siluGate, up);
        return DownProj.forward(gated);
    }

    /// <summary>
    ///     前向传播（带自动微分记录）
    /// </summary>
    public ArrayND forward(ArrayND input, AutogradContext ctx)
    {
        var gate = GateProj.forward(input, ctx);
        var siluGate = Activations.SiLU(gate, ctx);
        var up = UpProj.forward(input, ctx);
        var gated = ElementWiseMulWithGrad(siluGate, up, ctx);
        return DownProj.forward(gated, ctx);
    }

    /// <summary>
    ///     获取所有可训练参数
    /// </summary>
    public IEnumerable<IParameter> Parameters()
    {
        foreach (var p in GateProj.Parameters()) yield return p;

        foreach (var p in UpProj.Parameters()) yield return p;

        foreach (var p in DownProj.Parameters()) yield return p;
    }

    private static ArrayND ElementWiseMul(ArrayND a, ArrayND b)
    {
        var result = ArrayND.Zeros(a.Shape);
        var spanA = a.AsSpan();
        var spanB = b.AsSpan();
        var spanR = result.AsWriteSpan();
        for (var i = 0; i < spanR.Length; i++) spanR[i] = spanA[i] * spanB[i];
        return result;
    }

    private static ArrayND ElementWiseMulWithGrad(ArrayND a, ArrayND b, AutogradContext ctx)
    {
        var result = ElementWiseMul(a, b);

        ctx.Record(result, [a, b], outputGrads =>
        {
            var dResult = outputGrads[0];
            var spanDR = dResult.AsSpan();
            var spanA = a.AsSpan();
            var spanB = b.AsSpan();

            var dA = ArrayND.Zeros(a.Shape);
            var dB = ArrayND.Zeros(b.Shape);
            var spanDA = dA.AsWriteSpan();
            var spanDB = dB.AsWriteSpan();

            for (var i = 0; i < spanDR.Length; i++)
            {
                spanDA[i] = spanDR[i] * spanB[i];
                spanDB[i] = spanDR[i] * spanA[i];
            }

            return [dA, dB];
        });

        return result;
    }
}