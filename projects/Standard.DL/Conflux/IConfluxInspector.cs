using Std.DL.Flux;

namespace Std.DL.Conflux;

/// <summary>融合场内部状态只读探查</summary>
public interface IConfluxInspector
{
    /// <summary>输入数量</summary>
    int InputCount { get; }

    /// <summary>获取指定输入</summary>
    ArrayND GetInput(int index);

    /// <summary>获取融合结果</summary>
    ArrayND GetResult();

    /// <summary>获取融合权重</summary>
    float[] GetWeights();
}