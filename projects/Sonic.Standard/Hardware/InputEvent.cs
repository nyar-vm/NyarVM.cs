using Core.Hardware;

namespace Std.Hardware;

/// <summary>
///     输入事件实现，描述一次输入事件的基本信息
/// </summary>
public sealed class InputEvent : IInputEvent
{
    /// <summary>
    ///     初始化输入事件
    /// </summary>
    /// <param name="timestamp">事件时间戳</param>
    /// <param name="deviceId">设备标识</param>
    /// <param name="type">事件类型</param>
    public InputEvent(long timestamp, string deviceId, InputEventType type)
    {
        this.timestamp = timestamp;
        device_id = deviceId;
        this.type = type;
    }

    /// <summary>
    ///     事件时间戳
    /// </summary>
    public long timestamp { get; }

    /// <summary>
    ///     产生事件的设备标识
    /// </summary>
    public string device_id { get; }

    /// <summary>
    ///     事件类型
    /// </summary>
    public InputEventType type { get; }
}