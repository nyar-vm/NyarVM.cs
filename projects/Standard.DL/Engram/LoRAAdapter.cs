using Std.DL.Flux;

namespace Std.DL.Engram;

/// <summary>LoRA 增量塑形单元</summary>
public sealed class LoRAAdapter
{
    /// <summary>LoRA 配置</summary>
    public LoRAConfig Config { get; init; } = new();

    /// <summary>A 矩阵</summary>
    public float[] A { get; init; } = [];

    /// <summary>B 矩阵</summary>
    public float[] B { get; init; } = [];

    /// <summary>应用 LoRA 变换</summary>
    public ArrayND Apply(ArrayND input)
    {
        return input;
    }
}