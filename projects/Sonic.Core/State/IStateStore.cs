namespace Core.State;

/// <summary>
///     IStateStore 接口
/// </summary>
public interface IStateStore
{
    /// <summary>
    ///     获取指定键的可读状态
    /// </summary>
    IReadableState<T> get_state<T>(string key);

    /// <summary>
    ///     获取指定键的可写状态
    /// </summary>
    IWritableState<T> get_writable_state<T>(string key);
}