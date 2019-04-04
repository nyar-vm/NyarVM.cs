namespace Std.DL.Flux;

/// <summary>可组合异步流式源接口</summary>
public interface IFluxable<out T>
{
    /// <summary>转为异步流</summary>
    IAsyncEnumerable<T> StreamAsync(CancellationToken cancellationToken = default);
}