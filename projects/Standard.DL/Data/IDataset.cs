using Std.DL.Flux;

namespace Std.DL.Data;

/// <summary>
///     数据集抽象接口 —— 定义训练/评估数据的访问方式
/// </summary>
public interface IDataset
{
    /// <summary>样本总数</summary>
    int Count { get; }

    /// <summary>单个样本输入维度</summary>
    int InputSize { get; }

    /// <summary>类别数量（输出维度）</summary>
    int OutputSize { get; }

    /// <summary>
    ///     获取指定索引的样本批次
    /// </summary>
    /// <param name="indices">样本索引数组</param>
    /// <returns>(输入, 标签 [batch, 1])</returns>
    (ArrayND Inputs, ArrayND Labels) GetBatch(int[] indices);
}

/// <summary>
///     内存数据集 —— 将全部数据加载到内存中，支持快速索引访问
/// </summary>
public sealed class InMemoryDataset : IDataset
{
    private readonly ArrayND _inputs;
    private readonly ArrayND _labels;

    /// <summary>
    ///     创建内存数据集
    /// </summary>
    /// <param name="inputs">全部输入 [N, inputSize]</param>
    /// <param name="labels">全部标签 [N, 1]（索引编码）</param>
    /// <param name="outputSize">类别数</param>
    public InMemoryDataset(ArrayND inputs, ArrayND labels, int outputSize)
    {
        if (inputs.Shape[0] != labels.Shape[0])
            throw new ArgumentException(
                $"输入样本数 {inputs.Shape[0]} 与标签数 {labels.Shape[0]} 不一致");

        _inputs = inputs;
        _labels = labels;
        Count = inputs.Shape[0];
        InputSize = inputs.Shape[1];
        OutputSize = outputSize;
    }

    /// <summary>样本总数</summary>
    public int Count { get; }

    /// <summary>输入维度</summary>
    public int InputSize { get; }

    /// <summary>类别数</summary>
    public int OutputSize { get; }

    /// <summary>
    ///     获取指定索引的批量样本
    /// </summary>
    public (ArrayND Inputs, ArrayND Labels) GetBatch(int[] indices)
    {
        var batchSize = indices.Length;
        var flatInputs = new float[batchSize * InputSize];
        var flatLabels = new float[batchSize];

        var spanInputs = _inputs.AsSpan();
        var spanLabels = _labels.AsSpan();

        for (var i = 0; i < batchSize; i++)
        {
            var idx = indices[i];
            var srcInputStart = idx * InputSize;
            var dstInputStart = i * InputSize;

            for (var j = 0; j < InputSize; j++) flatInputs[dstInputStart + j] = spanInputs[srcInputStart + j];

            flatLabels[i] = spanLabels[idx];
        }

        return (ArrayND.FromArray(flatInputs, batchSize, InputSize),
            ArrayND.FromArray(flatLabels, batchSize, 1));
    }
}

/// <summary>
///     数据批次结构
/// </summary>
public sealed class DataBatch
{
    /// <summary>批次输入 [batchSize, inputSize]</summary>
    public ArrayND Inputs { get; init; } = ArrayND.Zeros(1, 1);

    /// <summary>批次标签 [batchSize, 1]</summary>
    public ArrayND Labels { get; init; } = ArrayND.Zeros(1, 1);

    /// <summary>批次索引</summary>
    public int BatchIndex { get; init; }

    /// <summary>样本总数</summary>
    public int TotalSamples { get; init; }
}