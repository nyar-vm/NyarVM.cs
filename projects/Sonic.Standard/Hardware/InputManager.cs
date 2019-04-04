using System.Threading.Channels;
using Core.Hardware;

namespace Std.Hardware;

/// <summary>
///     输入管理器，实现 IInputSource 接口，提供异步输入事件流
/// </summary>
public sealed class InputManager : IInputSource
{
    /// <summary>
    ///     事件通道
    /// </summary>
    private readonly Channel<IInputEvent> _channel;

    /// <summary>
    ///     初始化输入管理器
    /// </summary>
    /// <param name="capabilities">输入能力</param>
    public InputManager(InputCapability capabilities = InputCapability.keying | InputCapability.pointing)
    {
        this.capabilities = capabilities;
        _channel = Channel.CreateUnbounded<IInputEvent>();
    }

    /// <summary>
    ///     输入能力
    /// </summary>
    public InputCapability capabilities { get; }

    /// <summary>
    ///     输入事件异步枚举
    /// </summary>
    public IAsyncEnumerable<IInputEvent> events => get_events();

    /// <summary>
    ///     分发输入事件
    /// </summary>
    /// <param name="evt">输入事件</param>
    public void dispatch(IInputEvent evt)
    {
        _channel.Writer.TryWrite(evt);
    }

    /// <summary>
    ///     异步获取输入事件流
    /// </summary>
    private async IAsyncEnumerable<IInputEvent> get_events([EnumeratorCancellation] CancellationToken ct = default)
    {
        await foreach (var evt in _channel.Reader.ReadAllAsync(ct)) yield return evt;
    }
}