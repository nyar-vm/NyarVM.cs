using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Database;

/// <summary>
///     类型安全文档集合接口
/// </summary>
/// <typeparam name="TDocument">文档类型</typeparam>
public interface ICollection<TDocument> where TDocument : class
{
    /// <summary>
    ///     集合名称
    /// </summary>
    string Name { get; }

    /// <summary>
    ///     异步插入文档
    /// </summary>
    ValueTask<ReadOnlyMemory<byte>> InsertAsync(TDocument document, CancellationToken cancellationToken = default);

    /// <summary>
    ///     异步查找文档
    /// </summary>
    ValueTask<TDocument?> FindAsync(ReadOnlyMemory<byte> key, CancellationToken cancellationToken = default);

    /// <summary>
    ///     异步更新文档
    /// </summary>
    ValueTask<bool> UpdateAsync(ReadOnlyMemory<byte> key, TDocument document,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     异步删除文档
    /// </summary>
    ValueTask<bool> DeleteAsync(ReadOnlyMemory<byte> key, CancellationToken cancellationToken = default);

    /// <summary>
    ///     异步查询文档
    /// </summary>
    ValueTask<IReadOnlyList<TDocument>> QueryAsync(Func<TDocument, bool> predicate,
        CancellationToken cancellationToken = default);
}