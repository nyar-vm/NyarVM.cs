using Core.Media;

namespace Std.Video.Graph;

/// <summary>
///     视频滤镜图类，实现 <see cref="IFilterGraph" /> 接口，提供滤镜节点的连接与处理能力。
/// </summary>
public sealed class VideoFilterGraph : IFilterGraph
{
    private readonly Dictionary<string, VideoFilterGraph> _input_ports = new();
    private readonly Dictionary<string, VideoFilterGraph> _output_ports = new();

    /// <summary>
    ///     获取命名输入端口字典。
    /// </summary>
    public IReadOnlyDictionary<string, VideoFilterGraph> input_ports => _input_ports;

    /// <summary>
    ///     获取命名输出端口字典。
    /// </summary>
    public IReadOnlyDictionary<string, VideoFilterGraph> output_ports => _output_ports;

    /// <summary>
    ///     将源滤镜图的输出端口连接到当前滤镜图的输入端口。
    /// </summary>
    /// <param name="source">源滤镜图。</param>
    /// <param name="outputName">源滤镜图的输出端口名称。</param>
    /// <param name="inputName">当前滤镜图的输入端口名称。</param>
    public void connect(IFilterGraph source, string outputName, string inputName)
    {
        if (source is VideoFilterGraph sourceGraph)
        {
            _input_ports[inputName] = sourceGraph;
            sourceGraph._output_ports[outputName] = this;
        }
    }

    /// <summary>
    ///     执行滤镜图处理。
    /// </summary>
    public void process()
    {
        throw new NotImplementedException();
    }
}