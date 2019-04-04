namespace Core.Flow.Pipeline;

/// <summary>
///     IPipeline 接口
/// </summary>
public interface IPipeline<TIn, TOut>
{
    /// <summary>
    ///     处理输入数据并返回输出
    /// </summary>
    TOut process(TIn input);
}