namespace Std.DL.Engram;

/// <summary>印迹数据结构</summary>
public sealed class EngramData
{
    /// <summary>数据标识</summary>
    public string Id { get; init; } = "";

    /// <summary>所属分支标识</summary>
    public string BranchId { get; init; } = "";

    /// <summary>模型数据</summary>
    public byte[] ModelData { get; init; } = [];

    /// <summary>LoRA 配置</summary>
    public LoRAConfig? LoRAConfig { get; init; }

    /// <summary>元数据</summary>
    public Dictionary<string, string> Metadata { get; init; } = new();

    /// <summary>创建时间</summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}