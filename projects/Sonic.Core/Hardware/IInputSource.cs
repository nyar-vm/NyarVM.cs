using System.Collections.Generic;

namespace Core.Hardware;

/// <summary>
///     输入源接口，提供异步事件流和输入能力信息
/// </summary>
public interface IInputSource
{
    /// <summary>
    ///     输入事件异步枚举
    /// </summary>
    IAsyncEnumerable<IInputEvent> events { get; }

    /// <summary>
    ///     输入能力
    /// </summary>
    InputCapability capabilities { get; }
}