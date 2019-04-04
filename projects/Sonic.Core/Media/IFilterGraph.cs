namespace Core.Media;

/// <summary>
///     滤镜图接口，用于连接滤镜节点构建处理管线。
/// </summary>
public interface IFilterGraph
{
    /// <summary>
    ///     将源滤镜图的输出端口连接到当前滤镜图的输入端口。
    /// </summary>
    /// <param name="source">源滤镜图。</param>
    /// <param name="outputName">源滤镜图的输出端口名称。</param>
    /// <param name="inputName">当前滤镜图的输入端口名称。</param>
    void connect(IFilterGraph source, string outputName, string inputName);
}