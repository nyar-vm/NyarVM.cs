using Std.DL.Flux;

namespace Std.DL.Data;

/// <summary>
///     数据变换接口 —— 定义对张量的变换操作
/// </summary>
public interface ITransform
{
    /// <summary>
    ///     对输入张量应用变换
    /// </summary>
    /// <param name="input">输入张量</param>
    /// <returns>变换后的张量</returns>
    ArrayND Apply(ArrayND input);
}

/// <summary>
///     组合变换 —— 将多个变换按顺序串联执行
/// </summary>
public sealed class Compose : ITransform
{
    private readonly ITransform[] _transforms;

    /// <summary>
    ///     创建组合变换
    /// </summary>
    /// <param name="transforms">按顺序执行的变换数组</param>
    public Compose(params ITransform[] transforms)
    {
        _transforms = transforms;
    }

    /// <summary>
    ///     变换数量
    /// </summary>
    public int Count => _transforms.Length;

    /// <summary>
    ///     依次应用所有变换
    /// </summary>
    /// <param name="input">输入张量</param>
    /// <returns>经过所有变换后的张量</returns>
    public ArrayND Apply(ArrayND input)
    {
        var x = input;
        foreach (var transform in _transforms) x = transform.Apply(x);

        return x;
    }
}

/// <summary>
///     映射变换 —— 对张量的每个元素应用指定函数
/// </summary>
public sealed class MapTransform : ITransform
{
    private readonly Func<float, float> _mapFn;

    /// <summary>
    ///     创建映射变换
    /// </summary>
    /// <param name="mapFn">逐元素映射函数</param>
    public MapTransform(Func<float, float> mapFn)
    {
        _mapFn = mapFn;
    }

    /// <summary>
    ///     对每个元素应用映射函数
    /// </summary>
    /// <param name="input">输入张量</param>
    /// <returns>映射后的张量</returns>
    public ArrayND Apply(ArrayND input)
    {
        var result = ArrayND.Zeros(input.Shape);
        var spanInput = input.AsSpan();
        var spanResult = result.AsWriteSpan();

        for (var i = 0; i < spanInput.Length; i++) spanResult[i] = _mapFn(spanInput[i]);

        return result;
    }
}

/// <summary>
///     标准化变换 —— 按均值和标准差进行标准化
///     output = (input - mean) / std
/// </summary>
public sealed class NormalizeTransform : ITransform
{
    /// <summary>
    ///     创建标准化变换
    /// </summary>
    /// <param name="mean">均值</param>
    /// <param name="std">标准差</param>
    public NormalizeTransform(float mean, float std)
    {
        if (std < 1e-10f) throw new ArgumentOutOfRangeException(nameof(std), "标准差必须大于零");

        Mean = mean;
        Std = std;
    }

    /// <summary>
    ///     均值
    /// </summary>
    public float Mean { get; }

    /// <summary>
    ///     标准差
    /// </summary>
    public float Std { get; }

    /// <summary>
    ///     对张量进行标准化：(input - mean) / std
    /// </summary>
    /// <param name="input">输入张量</param>
    /// <returns>标准化后的张量</returns>
    public ArrayND Apply(ArrayND input)
    {
        var result = ArrayND.Zeros(input.Shape);
        var spanInput = input.AsSpan();
        var spanResult = result.AsWriteSpan();

        for (var i = 0; i < spanInput.Length; i++) spanResult[i] = (spanInput[i] - Mean) / Std;

        return result;
    }
}

/// <summary>
///     展平变换 —— 将张量重塑为一维
/// </summary>
public sealed class FlattenTransform : ITransform
{
    /// <summary>
    ///     将输入张量重塑为一维
    /// </summary>
    /// <param name="input">输入张量</param>
    /// <returns>一维张量</returns>
    public ArrayND Apply(ArrayND input)
    {
        return input.Reshape(input.Size);
    }
}