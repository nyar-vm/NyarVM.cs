namespace Std.DL.Engram;

/// <summary>LoRA 配置（rank、alpha、target）</summary>
public sealed class LoRAConfig
{
    /// <summary>低秩维度</summary>
    public int Rank { get; init; } = 8;

    /// <summary>缩放因子</summary>
    public float Alpha { get; init; } = 8.0f;

    /// <summary>目标层</summary>
    public string Target { get; init; } = "";

    /// <summary>是否启用</summary>
    public bool Enabled { get; init; } = true;
}