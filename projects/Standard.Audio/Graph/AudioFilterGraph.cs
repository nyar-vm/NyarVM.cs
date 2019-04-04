using Core.Media;

namespace Sonic.Audio.Graph;

/// <summary>
///     音频滤镜图类，实现 <see cref="IFilterGraph" /> 接口，用于连接滤镜节点构建音频处理管线。
/// </summary>
public sealed class AudioFilterGraph : IFilterGraph
{
    /// <summary>
    ///     连接字典，记录输出端口到输入端口的连接关系。
    /// </summary>
    private readonly Dictionary<(string outputName, string inputName), AudioFilterGraph> _connections = new();

    /// <summary>
    ///     命名输入端口字典。
    /// </summary>
    public Dictionary<string, AudioFilterGraph> inputs { get; } = new();

    /// <summary>
    ///     命名输出端口字典。
    /// </summary>
    public Dictionary<string, AudioFilterGraph> outputs { get; } = new();

    /// <summary>
    ///     将源滤镜图的输出端口连接到当前滤镜图的输入端口。
    /// </summary>
    /// <param name="source">源滤镜图。</param>
    /// <param name="outputName">源滤镜图的输出端口名称。</param>
    /// <param name="inputName">当前滤镜图的输入端口名称。</param>
    public void connect(IFilterGraph source, string outputName, string inputName)
    {
        if (source is AudioFilterGraph src)
        {
            _connections[(outputName, inputName)] = src;
            src.outputs[outputName] = this;
            inputs[inputName] = src;
        }
    }

    /// <summary>
    ///     执行滤镜图处理管线。
    /// </summary>
    /// <param name="input">输入音频帧。</param>
    /// <returns>处理后的音频帧。</returns>
    public AudioFrame<float> process(AudioFrame<float> input)
    {
        throw new NotImplementedException();
    }
}