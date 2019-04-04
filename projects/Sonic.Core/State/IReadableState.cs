namespace Core.State;

/// <summary>
///     IReadableState 接口
/// </summary>
public interface IReadableState<T>
{
    /// <summary>
    ///     获取当前值
    /// </summary>
    T value { get; }
}