using System;

namespace Core.State.Observable;

/// <summary>
///     IObservableSource 接口
/// </summary>
public interface IObservableSource<T>
{
    /// <summary>
    ///     订阅数据源
    /// </summary>
    IDisposable subscribe(Action<T> observer);
}