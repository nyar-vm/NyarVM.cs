namespace Nyar.Database.Query;

/// <summary>
///     查询接口，所有增量查询必须实现此接口
/// </summary>
public interface IQuery
{
    /// <summary>
    ///     查询名称，用于缓存键和日志
    /// </summary>
    string name { get; }
}

/// <summary>
///     强类型查询接口，接受输入参数并返回结果
/// </summary>
/// <typeparam name="TInput">输入参数类型</typeparam>
/// <typeparam name="TOutput">输出结果类型</typeparam>
public interface IQuery<TInput, TOutput> : IQuery
{
    /// <summary>
    ///     执行查询计算
    /// </summary>
    /// <param name="input">输入参数。</param>
    /// <param name="context">查询执行上下文，用于调用其他查询。</param>
    /// <returns>查询结果。</returns>
    TOutput execute(TInput input, QueryContext context);

    /// <summary>
    ///     计算输入参数的哈希值，用于判断缓存是否有效
    /// </summary>
    /// <param name="input">输入参数。</param>
    /// <returns>哈希值。</returns>
    int compute_input_hash(TInput input);
}