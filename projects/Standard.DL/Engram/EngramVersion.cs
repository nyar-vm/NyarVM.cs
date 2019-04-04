namespace Std.DL.Engram;

/// <summary>印迹版本记录</summary>
public sealed class EngramVersion
{
    /// <summary>版本标识</summary>
    public string Id { get; init; } = "";

    /// <summary>所属分支标识</summary>
    public string BranchId { get; init; } = "";

    /// <summary>父版本标识</summary>
    public string ParentVersionId { get; init; } = "";

    /// <summary>创建时间</summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>版本消息</summary>
    public string Message { get; init; } = "";
}