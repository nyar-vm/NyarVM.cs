namespace Core.Chrono;

/// <summary>
///     IDuration 接口
/// </summary>
public interface IDuration
{
    /// <summary>
    ///     总纳秒数
    /// </summary>
    long total_nanoseconds { get; }
}