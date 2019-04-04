using Std.DL.Training;

namespace Std.DL.Flux;

/// <summary>
///     Sequential 容器 —— 链式组合多个层，前向传播依次调用
///     支持 ITrainableModel（标准输入输出）的层
/// </summary>
public sealed class Sequential : ITrainableModel
{
    private readonly List<ITrainableModel> _layers;

    /// <summary>
    ///     创建 Sequential 容器
    /// </summary>
    /// <param name="layers">按顺序排列的层列表</param>
    public Sequential(params ITrainableModel[] layers)
    {
        _layers = [.. layers];
    }

    /// <summary>
    ///     层数
    /// </summary>
    public int Count => _layers.Count;

    /// <summary>
    ///     获取第 index 层
    /// </summary>
    public ITrainableModel this[int index] => _layers[index];

    /// <summary>
    ///     前向传播（无自动微分）
    /// </summary>
    public ArrayND forward(ArrayND input)
    {
        var x = input;
        foreach (var layer in _layers) x = layer.forward(x);
        return x;
    }

    /// <summary>
    ///     前向传播（带自动微分记录）
    /// </summary>
    public ArrayND forward(ArrayND input, AutogradContext ctx)
    {
        var x = input;
        foreach (var layer in _layers) x = layer.forward(x, ctx);
        return x;
    }

    /// <summary>
    ///     汇总所有层的可训练参数
    /// </summary>
    public IEnumerable<IParameter> Parameters()
    {
        return _layers.SelectMany(l => l.Parameters());
    }

    /// <summary>
    ///     追加一层到末尾
    /// </summary>
    public void Add(ITrainableModel layer)
    {
        _layers.Add(layer);
    }
}