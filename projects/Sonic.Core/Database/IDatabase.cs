using System;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Database;

/// <summary>
///     核心数据库接口，定义键值存储的基本操作
/// </summary>
public interface IDatabase : IDisposable, IAsyncDisposable
{
    /// <summary>
    ///     数据库名称
    /// </summary>
    string name { get; }

    /// <summary>
    ///     索引管理器
    /// </summary>
    IIndexManager IndexManager { get; }

    /// <summary>
    ///     异步获取键值
    /// </summary>
    /// <param name="key">键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>值，若不存在则返回 null</returns>
    Task<ReadOnlyMemory<byte>?> GetAsync(ReadOnlyMemory<byte> key, CancellationToken cancellationToken = default);

    /// <summary>
    ///     异步存储键值
    /// </summary>
    /// <param name="key">键</param>
    /// <param name="value">值</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task PutAsync(ReadOnlyMemory<byte> key, ReadOnlyMemory<byte> value, CancellationToken cancellationToken = default);

    /// <summary>
    ///     异步删除键
    /// </summary>
    /// <param name="key">键</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task DeleteAsync(ReadOnlyMemory<byte> key, CancellationToken cancellationToken = default);

    /// <summary>
    ///     异步判断键是否存在
    /// </summary>
    /// <param name="key">键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否存在</returns>
    Task<bool> ContainsKeyAsync(ReadOnlyMemory<byte> key, CancellationToken cancellationToken = default);

    /// <summary>
    ///     开启事务
    /// </summary>
    /// <param name="isolationLevel">隔离级别</param>
    /// <returns>事务实例</returns>
    ITransaction BeginTransaction(IsolationLevel isolationLevel = IsolationLevel.Snapshot);

    /// <summary>
    ///     创建快照
    /// </summary>
    /// <returns>快照实例</returns>
    ISnapshot CreateSnapshot();

    /// <summary>
    ///     创建游标
    /// </summary>
    /// <returns>游标实例</returns>
    ICursor CreateCursor();
}