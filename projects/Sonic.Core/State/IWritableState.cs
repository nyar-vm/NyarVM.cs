namespace Core.State;

/// <summary>
///     IWritableState 接口
/// </summary>
public interface IWritableState<T>
{
    /// <summary>
    ///     获取或设置当前值
    /// </summary>
    T value { get; set; }
}