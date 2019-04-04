using Std.DL.Flux;

namespace Std.DL.Execution;

/// <summary>
///     计算图中的单个操作 —— 编译后的执行单元
/// </summary>
public sealed class GraphOperation
{
    /// <summary>
    ///     操作名称（唯一标识）
    /// </summary>
    public string Name { get; init; } = "";

    /// <summary>
    ///     操作类型（"Dense" / "Conv2D" / "ReLU" / "Sigmoid" / "Tanh" / "Softmax" / "GELU" / "SiLU" / "MaxPool2D" / "Flatten" /
    ///     "Dropout" / "BatchNorm"）
    /// </summary>
    public string OpType { get; init; } = "";

    /// <summary>
    ///     操作配置（如 fanIn / fanOut / channels / kernelSize 等）
    /// </summary>
    public Dictionary<string, object> Config { get; init; } = new();

    /// <summary>
    ///     输入张量的来源名称列表
    /// </summary>
    public string[] InputNames { get; init; } = [];

    /// <summary>
    ///     操作所需的参数张量（Weight / Bias / Gamma / Beta 等）
    /// </summary>
    public ArrayND[] Parameters { get; init; } = [];
}