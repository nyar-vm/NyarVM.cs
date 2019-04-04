namespace Std.DL.Execution;

/// <summary>
///     编译后的计算图 IR —— 线性操作序列
/// </summary>
public sealed class CompiledGraph
{
    /// <summary>
    ///     计算图标识
    /// </summary>
    public string Id { get; init; } = "";

    /// <summary>
    ///     线性操作序列（拓扑排序后）
    /// </summary>
    public IReadOnlyList<GraphOperation> Operations { get; init; } = [];

    /// <summary>
    ///     输入名称列表
    /// </summary>
    public IReadOnlyList<string> InputNames { get; init; } = [];

    /// <summary>
    ///     输出操作名称列表（最终输出）
    /// </summary>
    public IReadOnlyList<string> OutputNames { get; init; } = [];

    /// <summary>
    ///     计算图字节数据（兼容旧接口）
    /// </summary>
    public byte[] Data { get; init; } = [];
}