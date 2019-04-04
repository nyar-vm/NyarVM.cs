using System;

namespace Core.Chrono;

/// <summary>
///     IClock 接口
/// </summary>
public interface IClock
{
    /// <summary>
    ///     获取当前时间
    /// </summary>
    DateTimeOffset now { get; }
}