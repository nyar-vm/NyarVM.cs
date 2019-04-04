using Std.Database.Core;

namespace Std.Database;

/// <summary>
///     LightDB 文档集合实现
/// </summary>
/// <typeparam name="TDocument">文档类型</typeparam>
public sealed class LightCollection<TDocument> : global::Core.Database.ICollection<TDocument> where TDocument : class
{
    #region 构造函数

    /// <summary>
    ///     创建文档集合
    /// </summary>
    /// <param name="name">集合名称</param>
    /// <param name="database">数据库实例</param>
    internal LightCollection(string name, LightDb database)
    {
        Name = name;
        _database = database;
        _prefix = DatabaseKeyPatterns.collection(name);
    }

    #endregion

    #region 字段

    private readonly LightDb _database;
    private readonly DatabaseKey _prefix;

    #endregion

    #region ICollection<TDocument> 实现

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public async ValueTask<ReadOnlyMemory<byte>> InsertAsync(TDocument document,
        CancellationToken cancellationToken = default)
    {
        var docId = Guid.NewGuid().ToString("N");
        var key = DatabaseKeyPatterns.collection_doc(Name, docId);
        await _database.put(key, document, cancellationToken);
        return key.bytes;
    }

    /// <inheritdoc />
    public async ValueTask<TDocument?> FindAsync(ReadOnlyMemory<byte> key,
        CancellationToken cancellationToken = default)
    {
        var dbKey = new DatabaseKey(key.ToArray());
        return await _database.get<TDocument>(dbKey, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<bool> UpdateAsync(ReadOnlyMemory<byte> key, TDocument document,
        CancellationToken cancellationToken = default)
    {
        var dbKey = new DatabaseKey(key.ToArray());
        if (!await _database.contains(dbKey, cancellationToken)) return false;

        await _database.put(dbKey, document, cancellationToken);
        return true;
    }

    /// <inheritdoc />
    public async ValueTask<bool> DeleteAsync(ReadOnlyMemory<byte> key,
        CancellationToken cancellationToken = default)
    {
        var dbKey = new DatabaseKey(key.ToArray());
        return await _database.delete(dbKey, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<IReadOnlyList<TDocument>> QueryAsync(Func<TDocument, bool> predicate,
        CancellationToken cancellationToken = default)
    {
        var results = new List<TDocument>();
        using var cursor = _database.seek(_prefix);
        var entries = await cursor.get_prefix(_prefix, int.MaxValue, cancellationToken);

        foreach (var entry in entries)
        {
            var document = entry.value.to_object<TDocument>();
            if (document is not null && predicate(document)) results.Add(document);
        }

        return results;
    }

    #endregion
}