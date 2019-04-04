namespace Std.Flow.Pipeline;

/// <summary>
///     流式管道接口，连接数据源、处理器和输出端。
///     源代码生成器会为标记了 <c>[StreamProcessor]</c>、<c>[StreamSource]</c>、<c>[StreamSink]</c> 的类自动实现此接口。
/// </summary>
/// <typeparam name="T">数据元素类型。</typeparam>
public interface IPipeline<T>
{
    /// <summary>
    ///     处理输入数据流并返回输出数据流。
    /// </summary>
    /// <param name="source">输入数据流。</param>
    /// <returns>输出数据流。</returns>
    IAsyncEnumerable<T> process(IAsyncEnumerable<T> source, CancellationToken cancellationToken = default);
}