namespace Core.Hardware;

/// <summary>
///     输入事件接口，描述一次输入事件的基本信息
/// </summary>
public interface IInputEvent
{
    /// <summary>
    ///     事件时间戳
    /// </summary>
    long timestamp { get; }

    /// <summary>
    ///     产生事件的设备标识
    /// </summary>
    string device_id { get; }

    /// <summary>
    ///     事件类型
    /// </summary>
    InputEventType type { get; }
}