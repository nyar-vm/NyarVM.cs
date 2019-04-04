using Std.DL.Training;

namespace Std.DL.Flux;

/// <summary>
///     混合精度训练 —— FP16 前向 + FP32 主权重
///     维护 FP32 主权重（master weights），前向传播使用 FP16 副本
///     梯度在 FP32 下更新主权重，再转换为 FP16 副本
///     优点：减少显存占用、加速计算、保持训练稳定性
/// </summary>
public sealed class MixedPrecisionTrainer
{
    private readonly Func<ITrainableModel, ArrayND, AutogradContext, ArrayND>? _forwardFn;
    private readonly List<TrainingLogEntry> _log;
    private readonly int _logInterval;
    private readonly Func<ArrayND, ArrayND, AutogradContext, (ArrayND loss, ArrayND? aux)> _lossFn;
    private readonly ILRScheduler? _lrScheduler;

    private readonly Dictionary<int, ArrayND> _masterWeights;
    private readonly float _maxGradNorm;
    private readonly ITrainableModel _model;
    private readonly IOptimizer _optimizer;

    /// <summary>
    ///     创建混合精度训练器
    /// </summary>
    /// <param name="model">模型</param>
    /// <param name="optimizer">优化器</param>
    /// <param name="lrScheduler">学习率调度器</param>
    /// <param name="maxGradNorm">梯度裁剪阈值</param>
    /// <param name="logInterval">日志间隔</param>
    /// <param name="forwardFn">自定义前向函数</param>
    /// <param name="lossFn">损失函数</param>
    public MixedPrecisionTrainer(
        ITrainableModel model,
        IOptimizer optimizer,
        ILRScheduler? lrScheduler = null,
        float maxGradNorm = 1.0f,
        int logInterval = 10,
        Func<ITrainableModel, ArrayND, AutogradContext, ArrayND>? forwardFn = null,
        Func<ArrayND, ArrayND, AutogradContext, (ArrayND loss, ArrayND? aux)>? lossFn = null)
    {
        _model = model;
        _optimizer = optimizer;
        _lrScheduler = lrScheduler;
        _maxGradNorm = maxGradNorm;
        _logInterval = logInterval;
        _forwardFn = forwardFn;
        _lossFn = lossFn ?? ((logits, targets, ctx) =>
        {
            var (l, _) = Losses.SoftmaxCrossEntropy(logits, targets, ctx);
            return (l, null);
        });

        _masterWeights = new Dictionary<int, ArrayND>();
        GlobalStep = 0;
        Epoch = 0;
        _log = [];

        InitializeMasterWeights();
    }

    /// <summary>
    ///     全局步数
    /// </summary>
    public int GlobalStep { get; private set; }

    /// <summary>
    ///     当前 epoch
    /// </summary>
    public int Epoch { get; private set; }

    /// <summary>
    ///     训练日志
    /// </summary>
    public IReadOnlyList<TrainingLogEntry> Log => _log;

    /// <summary>
    ///     获取主权重（FP32）
    /// </summary>
    /// <returns>主权重字典</returns>
    public IReadOnlyDictionary<int, ArrayND> MasterWeights => _masterWeights;

    /// <summary>
    ///     执行一个混合精度训练步骤
    ///     1. 将 FP32 主权重转换为 FP16 副本
    ///     2. FP16 前向传播
    ///     3. FP16 反向传播（梯度为 FP32）
    ///     4. FP32 梯度裁剪
    ///     5. FP32 更新主权重
    ///     6. 将更新后的 FP32 主权重转换为 FP16 副本
    /// </summary>
    /// <param name="inputs">输入</param>
    /// <param name="targets">目标</param>
    /// <returns>本步损失值</returns>
    public float TrainStep(ArrayND inputs, ArrayND targets)
    {
        CopyMasterToModel();

        var ctx = new AutogradContext();
        ctx.StartRecording();

        ArrayND logits;
        if (_forwardFn != null)
            logits = _forwardFn(_model, inputs, ctx);
        else if (_model is GPTModel gptModel)
            logits = gptModel.ForwardForTraining(inputs, ctx);
        else if (_model is WeightTiedGPTModel wtModel)
            logits = wtModel.ForwardForTraining(inputs, ctx);
        else
            logits = _model.forward(inputs, ctx);

        var (lossTensor, _) = _lossFn(logits, targets, ctx);
        ctx.Backward(lossTensor);

        var loss = lossTensor.AsSpan()[0];

        if (_maxGradNorm > 0)
        {
            var allParams = _model.Parameters().ToList();
            GradientClipping.ClipGradNorm(allParams, _maxGradNorm);
        }

        var paramList = _model.Parameters().ToList();
        _optimizer.Step(paramList);
        _optimizer.ZeroGrad(paramList);

        UpdateMasterWeights();

        _lrScheduler?.Step();
        GlobalStep++;

        if (GlobalStep % _logInterval == 0 || GlobalStep <= 1)
        {
            var lr = _lrScheduler?.LearningRate ?? 0;
            _log.Add(new TrainingLogEntry(GlobalStep, Epoch, loss, lr));
        }

        return loss;
    }

    /// <summary>
    ///     训练一个 epoch
    /// </summary>
    /// <param name="dataInputs">训练输入</param>
    /// <param name="dataLabels">训练标签</param>
    /// <param name="batchSize">批次大小</param>
    /// <returns>epoch 平均损失</returns>
    public float TrainEpoch(ArrayND dataInputs, ArrayND dataLabels, int batchSize)
    {
        var totalSamples = dataInputs.Shape[0];
        var totalLoss = 0.0f;
        var numBatches = 0;

        for (var start = 0; start < totalSamples; start += batchSize)
        {
            var end = System.Math.Min(start + batchSize, totalSamples);
            var actualBatchSize = end - start;

            var batchInputs = dataInputs.SliceRows(start, actualBatchSize);
            var batchLabels = dataLabels.SliceRows(start, actualBatchSize);

            var loss = TrainStep(batchInputs, batchLabels);
            totalLoss += loss;
            numBatches++;
        }

        Epoch++;
        return numBatches > 0 ? totalLoss / numBatches : 0.0f;
    }

    private void InitializeMasterWeights()
    {
        var idx = 0;
        foreach (var p in _model.Parameters())
        {
            var master = ArrayND.Zeros(p.Value.Shape);
            var spanSrc = p.Value.AsSpan();
            var spanDst = master.AsWriteSpan();
            for (var i = 0; i < spanSrc.Length; i++) spanDst[i] = spanSrc[i];
            _masterWeights[idx] = master;
            idx++;
        }
    }

    private void CopyMasterToModel()
    {
        var idx = 0;
        foreach (var p in _model.Parameters())
        {
            if (_masterWeights.TryGetValue(idx, out var master))
            {
                var spanSrc = master.AsSpan();
                var spanDst = p.Value.AsWriteSpan();
                for (var i = 0; i < spanSrc.Length && i < spanDst.Length; i++) spanDst[i] = SingleToHalf(spanSrc[i]);
            }

            idx++;
        }
    }

    private void UpdateMasterWeights()
    {
        var idx = 0;
        foreach (var p in _model.Parameters())
        {
            if (_masterWeights.TryGetValue(idx, out var master))
            {
                var spanModel = p.Value.AsSpan();
                var spanMaster = master.AsWriteSpan();
                for (var i = 0; i < spanMaster.Length && i < spanModel.Length; i++) spanMaster[i] = spanModel[i];
            }

            idx++;
        }
    }

    private static float SingleToHalf(float value)
    {
        if (float.IsNaN(value) || float.IsInfinity(value)) return value;

        if (value == 0) return 0;

        if (value > 65504.0f) return 65504.0f;

        if (value < -65504.0f) return -65504.0f;

        var absVal = MathF.Abs(value);
        if (absVal < 6.1e-5f) return 0;

        var exp = MathF.Floor(MathF.Log2(absVal));
        if (exp < -14) return 0;

        var mantissa = absVal / MathF.Pow(2, exp) - 1.0f;
        var halfMantissa = MathF.Round(mantissa * 1024) / 1024;
        return MathF.CopySign(MathF.Pow(2, exp) * (1.0f + halfMantissa), value);
    }
}

/// <summary>
///     梯度缩放器 —— 用于混合精度训练中的损失缩放（Loss Scaling）
///     防止 FP16 梯度下溢（underflow）
///     动态缩放：如果出现 inf/nan 梯度，缩小 scale；否则逐步增大
/// </summary>
public sealed class GradScaler
{
    private readonly float _backoffFactor;
    private readonly float _growthFactor;
    private readonly int _growthInterval;
    private int _growthTracker;

    /// <summary>
    ///     创建梯度缩放器
    /// </summary>
    /// <param name="initialScale">初始缩放因子</param>
    /// <param name="growthFactor">增长因子</param>
    /// <param name="backoffFactor">回退因子</param>
    /// <param name="growthInterval">增长间隔步数</param>
    public GradScaler(float initialScale = 65536.0f, float growthFactor = 2.0f, float backoffFactor = 0.5f,
        int growthInterval = 2000)
    {
        Scale = initialScale;
        _growthFactor = growthFactor;
        _backoffFactor = backoffFactor;
        _growthInterval = growthInterval;
        _growthTracker = 0;
    }

    /// <summary>
    ///     当前缩放因子
    /// </summary>
    public float Scale { get; private set; }

    /// <summary>
    ///     缩放损失值
    /// </summary>
    /// <param name="loss">原始损失</param>
    /// <returns>缩放后的损失</returns>
    public ArrayND ScaleLoss(ArrayND loss)
    {
        return loss * Scale;
    }

    /// <summary>
    ///     反缩放梯度
    /// </summary>
    /// <param name="parameters">模型参数</param>
    public void UnscaleGrad(IEnumerable<IParameter> parameters)
    {
        var invScale = 1.0f / Scale;
        foreach (var p in parameters)
        {
            if (p.Value.Grad == null) continue;

            var span = p.Value.Grad.AsWriteSpan();
            for (var i = 0; i < span.Length; i++) span[i] *= invScale;
        }
    }

    /// <summary>
    ///     检查梯度中是否有 inf/nan
    /// </summary>
    /// <param name="parameters">模型参数</param>
    /// <returns>是否有 inf/nan</returns>
    public bool HasInfOrNanGrad(IEnumerable<IParameter> parameters)
    {
        foreach (var p in parameters)
        {
            if (p.Value.Grad == null) continue;

            var span = p.Value.Grad.AsSpan();
            for (var i = 0; i < span.Length; i++)
                if (float.IsInfinity(span[i]) || float.IsNaN(span[i]))
                    return true;
        }

        return false;
    }

    /// <summary>
    ///     更新缩放因子（每步调用）
    /// </summary>
    /// <param name="hasInfGrad">是否有 inf 梯度</param>
    public void Update(bool hasInfGrad)
    {
        if (hasInfGrad)
        {
            Scale *= _backoffFactor;
            _growthTracker = 0;
        }
        else
        {
            _growthTracker++;
            if (_growthTracker >= _growthInterval)
            {
                Scale *= _growthFactor;
                _growthTracker = 0;
            }
        }
    }
}