namespace Core.Hardware;

/// <summary>
///     输入事件类型枚举
/// </summary>
public enum InputEventType
{
    /// <summary>
    ///     按键按下
    /// </summary>
    key_down,

    /// <summary>
    ///     按键释放
    /// </summary>
    key_up,

    /// <summary>
    ///     指针移动
    /// </summary>
    pointer_move,

    /// <summary>
    ///     指针按下
    /// </summary>
    pointer_down,

    /// <summary>
    ///     指针释放
    /// </summary>
    pointer_up,

    /// <summary>
    ///     触控开始
    /// </summary>
    touch_start,

    /// <summary>
    ///     触控移动
    /// </summary>
    touch_move,

    /// <summary>
    ///     触控结束
    /// </summary>
    touch_end,

    /// <summary>
    ///     运动事件
    /// </summary>
    motion
}