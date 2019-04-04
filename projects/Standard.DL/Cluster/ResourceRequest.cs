namespace Std.DL.Cluster;

/// <summary>资源申请描述</summary>
public sealed class ResourceRequest
{
    /// <summary>GPU 数量</summary>
    public int GpuCount { get; init; } = 1;

    /// <summary>CPU 核心数</summary>
    public int CpuCount { get; init; } = 4;

    /// <summary>内存大小（字节）</summary>
    public long MemoryBytes { get; init; } = 8L * 1024 * 1024 * 1024;

    /// <summary>标签</summary>
    public string[] Tags { get; init; } = [];
}