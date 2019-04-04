namespace Core.State.Observable;

/// <summary>
///     IObservableOperator 接口
/// </summary>
public interface IObservableOperator<TIn, TOut>
{
    /// <summary>
    ///     对数据源应用算子
    /// </summary>
    IObservableSource<TOut> apply(IObservableSource<TIn> source);
}