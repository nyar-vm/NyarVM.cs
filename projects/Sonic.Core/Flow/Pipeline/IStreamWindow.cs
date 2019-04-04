using System.Collections.Generic;

namespace Core.Flow.Pipeline;

/// <summary>
///     IStreamWindow 接口
/// </summary>
public interface IStreamWindow<T>
{
    /// <summary>
    ///     收集窗口内数据
    /// </summary>
    IReadOnlyList<T> Collect();
}