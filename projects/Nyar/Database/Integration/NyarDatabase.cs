using Core.Database;
using Std.Database.Core;
using LightDB = Std.Database.LightDb;

namespace Nyar.Database.Integration;

public sealed class NyarDatabase : IAsyncDisposable
{
    #region 字段

    private bool _disposed;

    #endregion

    #region 构造函数

    // TODO: 待实现 - NyarDatabaseOptions 配置类型已移除，构造函数依赖项不完整
    // public NyarDatabase(global::Nyar.Database.Integration.NyarDatabaseOptions? options = null)
    // {
    //     Options = options ?? NyarDatabaseOptions.Default;
    //     Inner = new LightDB(Options.ToLightOptions());
    // }

    public NyarDatabase()
    {
        options = null;
        _inner = new LightDB();
    }

    #endregion

    #region IAsyncDisposable

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;

        _disposed = true;

        await _inner.DisposeAsync();
    }

    #endregion

    #region 事务与快照

    // TODO: LightDb 的 BeginTransaction 不支持 async，需要适配
    // public async ValueTask<ITransaction> BeginTransactionAsync(
    //     IsolationLevel isolationLevel = IsolationLevel.Snapshot,
    //     CancellationToken ct = default)
    // {
    //     return await Inner.BeginTransaction(isolationLevel, ct);
    // }

    public ISnapshot create_snapshot()
    {
        return _inner.CreateSnapshot();
    }

    #endregion

    #region 游标

    // TODO: LightDb 不直接暴露 Seek 方法，需要通过 CreateCursor 适配
    // public ICursor Seek(string key)
    // {
    //     return Inner.Seek(key);
    // }

    #endregion

    #region 集合

    // TODO: LightDb 不直接暴露 GetCollection<T> 方法
    // public Sonic.Standard.Database.ICollection<TDocument> GetCollection<TDocument>(string collectionName)
    //     where TDocument : class
    // {
    //     return Inner.GetCollection<TDocument>(collectionName);
    // }

    #endregion

    #region 检查点

    // TODO: LightDb 的 Checkpoint 为 internal，需要改用其他方式
    // public async ValueTask CheckpointAsync(CancellationToken ct = default)
    // {
    //     await Inner.Checkpoint(ct);
    // }

    #endregion

    #region 属性

    // TODO: NyarDatabaseOptions 类型已移除
    public object? options { get; }

    public DatabaseStatistics statistics => _inner.statistics;

    internal LightDB _inner { get; }

    #endregion

    #region 键值操作

    // TODO: LightDb 不直接暴露泛型 Get/Put/Delete/Contains 方法，需要通过 IDatabase 接口适配
    // public async ValueTask<TValue?> GetAsync<TValue>(string key, CancellationToken ct = default)
    // {
    //     return await Inner.Get<TValue>(key, ct);
    // }
    //
    // public async ValueTask PutAsync<TValue>(string key, TValue value, CancellationToken ct = default)
    // {
    //     await Inner.Put(key, value, ct);
    // }
    //
    // public async ValueTask<bool> DeleteAsync(string key, CancellationToken ct = default)
    // {
    //     return await Inner.Delete(key, ct);
    // }
    //
    // public async ValueTask<bool> ExistsAsync(string key, CancellationToken ct = default)
    // {
    //     return await Inner.Contains(key, ct);
    // }

    #endregion
}