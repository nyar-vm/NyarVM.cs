namespace Hermes.Plugin.Sql.Execution;

/// <summary>
///     事务作用域 — 提供 RAII 风格的事务管理，通过 using 语句自动提交或回滚。
/// </summary>
public sealed class TransactionScope : IAsyncDisposable
{
    private readonly ISqlExecutor _executor;
    private bool _committed;
    private bool _disposed;

    private TransactionScope(ISqlExecutor executor)
    {
        _executor = executor;
    }

    /// <summary>
    ///     释放资源，如果未提交则回滚。
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;

        _disposed = true;

        if (!_committed) await _executor.RollbackAsync();
    }

    /// <summary>
    ///     创建事务作用域并开始事务。
    /// </summary>
    /// <param name="executor">SQL 执行器</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>事务作用域实例</returns>
    public static async Task<TransactionScope> BeginAsync(ISqlExecutor executor, CancellationToken ct = default)
    {
        await executor.BeginTransactionAsync(ct);
        return new TransactionScope(executor);
    }

    /// <summary>
    ///     提交事务。
    /// </summary>
    public async Task CommitAsync(CancellationToken ct = default)
    {
        await _executor.CommitAsync(ct);
        _committed = true;
    }
}