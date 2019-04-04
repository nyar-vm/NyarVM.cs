using Std.DL.Training;

namespace Std.DL.Flux;

/// <summary>
///     量化感知训练（QAT）模型包装器 —— 在模型中插入伪量化节点
///     对 Dense 层的权重和激活值分别插入 FakeQuantize 节点
///     训练时模拟量化误差，使模型适应低精度推理
/// </summary>
public sealed class QATModelWrapper : ITrainableModel
{
    private readonly int _activationBits;
    private readonly ITrainableModel _model;
    private readonly int _weightBits;

    /// <summary>
    ///     创建量化感知训练模型包装器
    /// </summary>
    /// <param name="model">待包装的可训练模型</param>
    /// <param name="weightBits">权重量化位数，默认 8</param>
    /// <param name="activationBits">激活值量化位数，默认 8</param>
    public QATModelWrapper(ITrainableModel model, int weightBits = 8, int activationBits = 8)
    {
        _model = model;
        _weightBits = weightBits;
        _activationBits = activationBits;
        _weightFakeQuantizers = [];
        _activationFakeQuantizers = [];
        _denseLayerIndices = [];

        InitializeFakeQuantizers();
    }

    /// <summary>
    ///     前向传播（无自动微分）：在每个 Dense 层前后插入伪量化
    /// </summary>
    /// <param name="input">输入张量</param>
    /// <returns>输出张量</returns>
    public ArrayND forward(ArrayND input)
    {
        if (_model is Sequential sequential) return ForwardSequential(sequential, input, null);

        var actFq = _activationFakeQuantizers.Count > 0 ? _activationFakeQuantizers[0] : null;
        var x = actFq is not null ? actFq.Forward(input) : input;
        x = _model.forward(x);

        if (_activationFakeQuantizers.Count > 1) x = _activationFakeQuantizers[^1].Forward(x);

        return x;
    }

    /// <summary>
    ///     前向传播（带自动微分记录）：在每个 Dense 层前后插入伪量化
    /// </summary>
    /// <param name="input">输入张量</param>
    /// <param name="ctx">自动微分上下文</param>
    /// <returns>输出张量</returns>
    public ArrayND forward(ArrayND input, AutogradContext ctx)
    {
        if (_model is Sequential sequential) return ForwardSequential(sequential, input, ctx);

        var actFq = _activationFakeQuantizers.Count > 0 ? _activationFakeQuantizers[0] : null;
        var x = actFq is not null ? actFq.Forward(input, ctx) : input;
        x = _model.forward(x, ctx);

        if (_activationFakeQuantizers.Count > 1) x = _activationFakeQuantizers[^1].Forward(x, ctx);

        return x;
    }

    /// <summary>
    ///     获取所有可训练参数（委托给被包装模型）
    /// </summary>
    /// <returns>可训练参数枚举</returns>
    public IEnumerable<IParameter> Parameters()
    {
        return _model.Parameters();
    }

    /// <summary>
    ///     运行校准过程：使用样本输入更新所有伪量化节点的量化范围
    /// </summary>
    /// <param name="sampleInput">校准用样本输入</param>
    public void Calibrate(ArrayND sampleInput)
    {
        if (_model is not Sequential sequential)
        {
            if (_activationFakeQuantizers.Count > 0) _activationFakeQuantizers[0].UpdateRange(sampleInput);

            var output = _model.forward(sampleInput);

            if (_activationFakeQuantizers.Count > 1) _activationFakeQuantizers[^1].UpdateRange(output);

            foreach (var wFq in _weightFakeQuantizers)
            {
                var paramsList = _model.Parameters().ToList();
                foreach (var param in paramsList) wFq.UpdateRange(param.Value);
            }

            return;
        }

        CalibrateSequential(sequential, sampleInput);
    }

    #region 伪量化节点集合

    private readonly List<FakeQuantize> _weightFakeQuantizers;
    private readonly List<FakeQuantize> _activationFakeQuantizers;
    private readonly List<int> _denseLayerIndices;

    #endregion

    #region 私有方法

    /// <summary>
    ///     初始化伪量化节点：为每个 Dense 层创建权重和激活值的伪量化器
    /// </summary>
    private void InitializeFakeQuantizers()
    {
        if (_model is Sequential sequential)
        {
            for (var i = 0; i < sequential.Count; i++)
                if (sequential[i] is Dense)
                {
                    _denseLayerIndices.Add(i);
                    _weightFakeQuantizers.Add(new FakeQuantize(_weightBits));
                    _activationFakeQuantizers.Add(new FakeQuantize(_activationBits));
                }

            if (_denseLayerIndices.Count > 0) _activationFakeQuantizers.Add(new FakeQuantize(_activationBits));
        }
        else
        {
            _weightFakeQuantizers.Add(new FakeQuantize(_weightBits));
            _activationFakeQuantizers.Add(new FakeQuantize(_activationBits));
            _activationFakeQuantizers.Add(new FakeQuantize(_activationBits));
        }
    }

    /// <summary>
    ///     Sequential 模型的前向传播：在 Dense 层前后插入伪量化
    /// </summary>
    private ArrayND ForwardSequential(Sequential sequential, ArrayND input, AutogradContext? ctx)
    {
        var x = input;
        var denseIdx = 0;

        for (var i = 0; i < sequential.Count; i++)
            if (sequential[i] is Dense)
            {
                if (denseIdx < _activationFakeQuantizers.Count - 1)
                    x = ctx is not null
                        ? _activationFakeQuantizers[denseIdx].Forward(x, ctx)
                        : _activationFakeQuantizers[denseIdx].Forward(x);

                x = ctx is not null
                    ? sequential[i].forward(x, ctx)
                    : sequential[i].forward(x);

                denseIdx++;
            }
            else
            {
                x = ctx is not null
                    ? sequential[i].forward(x, ctx)
                    : sequential[i].forward(x);
            }

        if (_activationFakeQuantizers.Count > 0 && denseIdx < _activationFakeQuantizers.Count)
            x = ctx is not null
                ? _activationFakeQuantizers[^1].Forward(x, ctx)
                : _activationFakeQuantizers[^1].Forward(x);

        return x;
    }

    /// <summary>
    ///     Sequential 模型的校准：逐层前向传播并更新量化范围
    /// </summary>
    private void CalibrateSequential(Sequential sequential, ArrayND sampleInput)
    {
        var x = sampleInput;
        var denseIdx = 0;

        for (var i = 0; i < sequential.Count; i++)
            if (sequential[i] is Dense dense)
            {
                if (denseIdx < _activationFakeQuantizers.Count - 1)
                {
                    _activationFakeQuantizers[denseIdx].UpdateRange(x);
                    x = _activationFakeQuantizers[denseIdx].Forward(x);
                }

                if (denseIdx < _weightFakeQuantizers.Count) _weightFakeQuantizers[denseIdx].UpdateRange(dense.Weight);

                x = sequential[i].forward(x);
                denseIdx++;
            }
            else
            {
                x = sequential[i].forward(x);
            }

        if (_activationFakeQuantizers.Count > 0) _activationFakeQuantizers[^1].UpdateRange(x);
    }

    #endregion
}