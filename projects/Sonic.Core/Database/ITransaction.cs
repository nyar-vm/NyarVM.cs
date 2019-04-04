using System;
using System.Threading.Tasks;

namespace Core.Database;

/// <summary>
///     事务接口，提供原子性操作
/// </summary>
public interface ITransaction : IDisposable
{
    /// <summary>
    ///     事务隔离级别
    /// </summary>
    IsolationLevel IsolationLevel { get; }

    /// <summary>
    ///     事务内获取键值
    /// </summary>
    Task<ReadOnlyMemory<byte>?> GetAsync(ReadOnlyMemory<byte> key);

    /// <summary>
    ///     事务内存储键值
    /// </summary>
    Task PutAsync(ReadOnlyMemory<byte> key, ReadOnlyMemory<byte> value);

    /// <summary>
    ///     事务内删除键
    /// </summary>
    Task DeleteAsync(ReadOnlyMemory<byte> key);

    /// <summary>
    ///     提交事务
    /// </summary>
    Task CommitAsync();

    /// <summary>
    ///     回滚事务
    /// </summary>
    Task RollbackAsync();
}