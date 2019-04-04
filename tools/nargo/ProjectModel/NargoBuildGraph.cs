namespace Nargo.Cli.ProjectModel;

/// <summary>
///     构建节点类型
/// </summary>
public enum NargoBuildNodeKind
{
    /// <summary>
    ///     语言处理节点（JS / TS / CSS / SCSS / Tailwind）
    /// </summary>
    Language,

    /// <summary>
    ///     资源处理节点（图片、字体等静态资源）
    /// </summary>
    Asset,

    /// <summary>
    ///     样式处理节点
    /// </summary>
    Style,

    /// <summary>
    ///     SSR 节点
    /// </summary>
    Ssr,

    /// <summary>
    ///     浏览器产物节点
    /// </summary>
    BrowserOutput,

    /// <summary>
    ///     Node 产物节点
    /// </summary>
    NodeOutput,

    /// <summary>
    ///     发布装配节点
    /// </summary>
    Publish,
}

/// <summary>
///     Nargo 构建图节点
/// </summary>
public sealed class NargoBuildNode
{
    /// <summary>
    ///     节点标识
    /// </summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>
    ///     节点类型
    /// </summary>
    public NargoBuildNodeKind Kind { get; init; } = NargoBuildNodeKind.Language;

    /// <summary>
    ///     节点标签（如 "typescript"、"css"、"ssr" 等）
    /// </summary>
    public string Label { get; init; } = string.Empty;

    /// <summary>
    ///     输入文件路径列表
    /// </summary>
    public List<string> Inputs { get; init; } = [];

    /// <summary>
    ///     输出文件路径列表
    /// </summary>
    public List<string> Outputs { get; init; } = [];

    /// <summary>
    ///     依赖的上游节点标识列表
    /// </summary>
    public List<string> Dependencies { get; init; } = [];
}

/// <summary>
///     Nargo 构建图：表达完整的构建依赖关系与处理流程
/// </summary>
public sealed class NargoBuildGraph
{
    /// <summary>
    ///     构建节点列表
    /// </summary>
    public List<NargoBuildNode> Nodes { get; init; } = [];

    /// <summary>
    ///     查找指定标识的构建节点
    /// </summary>
    /// <param name="nodeId">节点标识</param>
    /// <returns>匹配的构建节点，未找到返回 null</returns>
    public NargoBuildNode? find_node(string nodeId)
    {
        return Nodes.FirstOrDefault(n => n.Id == nodeId);
    }
}