namespace Std.DL.Diagnostics;

/// <summary>张量谱系信息</summary>
public sealed class TensorLineage
{
    /// <summary>张量标识</summary>
    public string TensorId { get; init; } = "";

    /// <summary>父张量标识列表</summary>
    public string[] ParentIds { get; init; } = [];

    /// <summary>操作名称</summary>
    public string OperationName { get; init; } = "";

    /// <summary>创建时间</summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>张量形状</summary>
    public int[] Shape { get; init; } = [];
}