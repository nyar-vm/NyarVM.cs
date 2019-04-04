namespace Std.Video.Extensions;

/// <summary>
///     视频视觉算子接口，提供可扩展的时域视觉操作能力。
/// </summary>
public interface IVideoVisionOperator
{
    /// <summary>
    ///     获取算子标识键。
    /// </summary>
    string key { get; }

    /// <summary>
    ///     判断当前算子是否能执行指定的视觉操作。
    /// </summary>
    /// <param name="operation">视觉操作名称。</param>
    /// <returns>是否能执行。</returns>
    bool can_execute(string operation);

    /// <summary>
    ///     执行视觉操作。
    /// </summary>
    /// <param name="operation">视觉操作名称。</param>
    /// <param name="input">输入数据。</param>
    /// <returns>操作结果。</returns>
    object execute(string operation, object input);
}