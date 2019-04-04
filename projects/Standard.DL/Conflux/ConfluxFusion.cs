using Std.DL.Flux;

namespace Std.DL.Conflux;

/// <summary>融合器实现</summary>
public sealed class ConfluxFusion : IConfluxFusion, IConfluxInspector
{
    private ArrayND[]? _inputs;
    private ArrayND? _result;
#pragma warning disable CS0649
    private float[]? _weights;
#pragma warning restore CS0649

    /// <summary>使用默认策略融合</summary>
    public ArrayND Fuse(params ArrayND[] inputs)
    {
        return FuseWithStrategy(FusionStrategy.Concatenate, inputs);
    }

    /// <summary>使用指定策略融合</summary>
    public ArrayND FuseWithStrategy(FusionStrategy strategy, params ArrayND[] inputs)
    {
        _inputs = inputs;

        _result = strategy switch
        {
            FusionStrategy.Concatenate => Concatenate(inputs),
            FusionStrategy.Additive => Additive(inputs),
            FusionStrategy.Multiplicative => Multiplicative(inputs),
            FusionStrategy.MeanPooling => MeanPooling(inputs),
            FusionStrategy.MaxPooling => MaxPooling(inputs),
            _ => Concatenate(inputs)
        };

        return _result;
    }

    /// <summary>输入数量</summary>
    public int InputCount => _inputs?.Length ?? 0;

    /// <summary>获取指定输入</summary>
    public ArrayND GetInput(int index)
    {
        return _inputs?[index] ?? throw new IndexOutOfRangeException();
    }

    /// <summary>获取融合结果</summary>
    public ArrayND GetResult()
    {
        return _result ?? throw new InvalidOperationException("无可用结果");
    }

    /// <summary>获取融合权重</summary>
    public float[] GetWeights()
    {
        return _weights ?? [];
    }

    private static ArrayND Concatenate(ArrayND[] inputs)
    {
        var totalSize = inputs.Sum(t => t.Size);
        var result = ArrayND.Zeros(totalSize);
        var offset = 0;
        foreach (var input in inputs)
        {
            var span = input.AsSpan();
            span.CopyTo(result.AsWriteSpan()[offset..]);
            offset += span.Length;
        }

        return result;
    }

    private static ArrayND Additive(ArrayND[] inputs)
    {
        var result = inputs[0].Clone();
        for (var i = 1; i < inputs.Length; i++) result = result + inputs[i];
        return result;
    }

    private static ArrayND Multiplicative(ArrayND[] inputs)
    {
        var result = inputs[0].Clone();
        for (var i = 1; i < inputs.Length; i++) result = result * inputs[i];
        return result;
    }

    private static ArrayND MeanPooling(ArrayND[] inputs)
    {
        var sum = Additive(inputs);
        return sum * (1.0f / inputs.Length);
    }

    private static ArrayND MaxPooling(ArrayND[] inputs)
    {
        var result = inputs[0].Clone();
        for (var i = 1; i < inputs.Length; i++)
        {
            var spanR = result.AsWriteSpan();
            var spanI = inputs[i].AsSpan();
            for (var j = 0; j < spanR.Length; j++) spanR[j] = MathF.Max(spanR[j], spanI[j]);
        }

        return result;
    }
}