using Std.DL.Training;

namespace Std.DL.Flux;

/// <summary>
///     激活检查点包装器 —— 前向时不保存中间激活，反向时重算。
///     用于训练大模型时节省显存（以计算时间换内存空间）。
/// </summary>
public sealed class Checkpoint : ITrainableModel
{
    private readonly ITrainableModel _inner;

    /// <summary>
    ///     创建检查点包装器
    /// </summary>
    /// <param name="inner">被包装的模型块</param>
    public Checkpoint(ITrainableModel inner)
    {
        _inner = inner;
    }

    /// <summary>
    ///     前向传播（无自动微分，直接透传）
    /// </summary>
    public ArrayND forward(ArrayND input)
    {
        return _inner.forward(input);
    }

    /// <summary>
    ///     前向传播（带检查点 —— 只保存输入，反向时重算整个块）
    /// </summary>
    public ArrayND forward(ArrayND input, AutogradContext ctx)
    {
        var savedInput = ArrayND.Zeros(input.Shape);
        var spanIn = input.AsSpan();
        var spanSaved = savedInput.AsWriteSpan();
        for (var i = 0; i < spanIn.Length; i++) spanSaved[i] = spanIn[i];

        var output = _inner.forward(input);

        ctx.Record(output, [input], grads =>
        {
            var dOutput = grads[0];

            var recomputedCtx = new AutogradContext();
            recomputedCtx.StartRecording();
            var recomputedOutput = _inner.forward(savedInput, recomputedCtx);

            recomputedCtx.BackwardFromGradient(recomputedOutput, dOutput!);

            return [savedInput.Grad];
        });

        return output;
    }

    /// <summary>
    ///     获取内部模型的参数
    /// </summary>
    public IEnumerable<IParameter> Parameters()
    {
        return _inner.Parameters();
    }
}

/// <summary>
///     分段检查点 —— 将模型分为 N 段，每段独立检查点。
///     相比整个模型一个检查点，分段检查点在反向传播时只需重算当前段，
///     减少不必要的重复计算，在内存节省和计算开销之间取得更好平衡。
/// </summary>
public sealed class SegmentedCheckpoint : ITrainableModel
{
    private readonly ITrainableModel[] _segments;

    /// <summary>
    ///     创建分段检查点
    /// </summary>
    /// <param name="segments">模型段列表，按前向顺序排列</param>
    public SegmentedCheckpoint(params ITrainableModel[] segments)
    {
        if (segments is null || segments.Length == 0) throw new ArgumentException("至少需要一个模型段", nameof(segments));

        _segments = segments;
    }

    /// <summary>
    ///     前向传播（无自动微分，逐段透传）
    /// </summary>
    public ArrayND forward(ArrayND input)
    {
        var current = input;
        foreach (var segment in _segments) current = segment.forward(current);

        return current;
    }

    /// <summary>
    ///     前向传播（带分段检查点 —— 每段独立检查点，反向时只重算当前段）
    /// </summary>
    public ArrayND forward(ArrayND input, AutogradContext ctx)
    {
        var current = input;

        foreach (var segment in _segments)
        {
            var segmentCheckpoint = new Checkpoint(segment);
            current = segmentCheckpoint.forward(current, ctx);
        }

        return current;
    }

    /// <summary>
    ///     获取所有段的参数
    /// </summary>
    public IEnumerable<IParameter> Parameters()
    {
        foreach (var segment in _segments)
        foreach (var param in segment.Parameters())
            yield return param;
    }
}

/// <summary>
///     自动分段检查点策略 —— 根据模型层数自动决定分段数量。
///     经验法则：每 4-6 层一个检查点段，在内存和计算之间取得平衡。
/// </summary>
public static class AutoCheckpoint
{
    /// <summary>
    ///     将顺序模型自动分段为检查点模型
    /// </summary>
    /// <param name="layers">模型层列表</param>
    /// <param name="layersPerSegment">每段包含的层数（默认 4）</param>
    /// <returns>分段检查点模型</returns>
    public static SegmentedCheckpoint FromLayers(IList<ITrainableModel> layers, int layersPerSegment = 4)
    {
        if (layers is null || layers.Count == 0) throw new ArgumentException("至少需要一个模型层", nameof(layers));

        if (layersPerSegment <= 0) throw new ArgumentOutOfRangeException(nameof(layersPerSegment), "每段层数必须大于 0");

        var segments = new List<ITrainableModel>();

        for (var i = 0; i < layers.Count; i += layersPerSegment)
        {
            var count = System.Math.Min(layersPerSegment, layers.Count - i);
            if (count == 1)
            {
                segments.Add(layers[i]);
            }
            else
            {
                var segmentLayers = new ITrainableModel[count];
                for (var j = 0; j < count; j++) segmentLayers[j] = layers[i + j];

                segments.Add(new Sequential(segmentLayers));
            }
        }

        return new SegmentedCheckpoint([.. segments]);
    }
}