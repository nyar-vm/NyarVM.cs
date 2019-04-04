namespace Std.DL.Engram;

/// <summary>印迹存储抽象（Get/Save/List/Import）</summary>
public interface IEngramStore
{
    /// <summary>获取印迹数据</summary>
    Task<EngramData?> GetAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>保存印迹数据</summary>
    Task<string> SaveAsync(EngramData data, CancellationToken cancellationToken = default);

    /// <summary>列出印迹版本</summary>
    IAsyncEnumerable<EngramVersion> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>导入印迹</summary>
    Task ImportAsync(string sourcePath, CancellationToken cancellationToken = default);
}