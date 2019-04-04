namespace Std.DL.Engram;

/// <summary>印迹分支（模型 + 微调状态）</summary>
public sealed class EngramBranch
{
    /// <summary>分支标识</summary>
    public string Id { get; init; } = "";

    /// <summary>分支名称</summary>
    public string Name { get; init; } = "";

    /// <summary>父分支标识</summary>
    public string ParentId { get; init; } = "";

    /// <summary>创建时间</summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>分支头部数据</summary>
    public EngramData? Head { get; init; }
}