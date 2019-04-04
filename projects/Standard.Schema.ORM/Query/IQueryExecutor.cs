using Hermes.YYDB.Query;

namespace Hermes.ORM;

/// <summary>
///     查询执行器接口
/// </summary>
public interface IQueryExecutor
{
    /// <summary>
    ///     执行查询表达式并返回原始结果
    /// </summary>
    Task<OrmQueryResult> ExecuteAsync(QueryExpression query, CancellationToken ct = default);

    /// <summary>
    ///     执行高级查询表达式（CTE/窗口函数/全文搜索/UPSERT）并返回原始结果
    /// </summary>
    Task<OrmQueryResult> ExecuteRawAsync(QueryExpression query, CancellationToken ct = default)
    {
        return ExecuteAsync(query, ct);
    }

    /// <summary>
    ///     创建数据库查询构建器
    /// </summary>
    DatabaseQuery<T> Query<T>() where T : class
    {
        throw new NotSupportedException("未实现数据库查询");
    }

    /// <summary>
    ///     创建缓存查询构建器
    /// </summary>
    CacheQuery<T> Cache<T>() where T : class
    {
        throw new NotSupportedException("未实现缓存查询");
    }

    /// <summary>
    ///     创建存储查询构建器
    /// </summary>
    StorageQuery<T> Store<T>() where T : class
    {
        throw new NotSupportedException("未实现存储查询");
    }

    /// <summary>
    ///     创建消息流查询构建器
    /// </summary>
    StreamQuery<T> Stream<T>() where T : class
    {
        throw new NotSupportedException("未实现消息流查询");
    }
}