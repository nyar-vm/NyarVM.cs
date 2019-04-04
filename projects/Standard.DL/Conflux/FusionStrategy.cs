namespace Std.DL.Conflux;

/// <summary>预置融合策略枚举</summary>
public enum FusionStrategy
{
    /// <summary>沿轴拼接</summary>
    Concatenate,

    /// <summary>加权求和</summary>
    Additive,

    /// <summary>逐元素相乘</summary>
    Multiplicative,

    /// <summary>注意力池化</summary>
    AttentionPooling,

    /// <summary>均值池化</summary>
    MeanPooling,

    /// <summary>最大池化</summary>
    MaxPooling
}