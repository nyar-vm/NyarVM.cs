using System.Text;

namespace Hermes.YYDB.Query;

public sealed class StorageQuery<T> where T : class
{
    private readonly IStorageBackend _backend;
    private string? _contentType;
    private Stream? _data;
    private string? _key;
    private Func<T, string>? _keySelector;
    private string? _prefix;

    public StorageQuery(IStorageBackend backend)
    {
        _backend = backend;
    }

    #region 删除

    public async Task<bool> DeleteAsync()
    {
        if (_key == null) throw new InvalidOperationException("必须指定对象键");

        return await _backend.DeleteAsync(_key);
    }

    #endregion

    #region 列举

    public async Task<List<StorageObjectInfo>> ListAsync()
    {
        var infos = await _backend.ListAsync(_prefix);
        return
        [
            .. infos.Select(i => new StorageObjectInfo
            {
                Key = i.Key,
                Size = i.Size,
                LastModified = i.LastModified,
                ETag = i.ETag
            })
        ];
    }

    #endregion

    #region 预签名

    public async Task<string> GetPresignedUrlAsync(TimeSpan expiry)
    {
        if (_key == null) throw new InvalidOperationException("必须指定对象键");

        return await _backend.GetPresignedUrlAsync(_key, expiry);
    }

    #endregion

    private static T? MapMetadata(StorageMetadata meta)
    {
        var type = typeof(T);
        var obj = Activator.CreateInstance<T>();

        foreach (var prop in type.GetProperties())
            if (prop.Name == "Key" && meta.Key != null)
                prop.SetValue(obj, Convert.ChangeType(meta.Key, prop.PropertyType));
            else if (prop.Name == "Size" && meta.Size.HasValue)
                prop.SetValue(obj, Convert.ChangeType(meta.Size.Value, prop.PropertyType));
            else if (prop.Name == "ContentType" && meta.ContentType != null)
                prop.SetValue(obj, Convert.ChangeType(meta.ContentType, prop.PropertyType));
            else if (prop.Name == "ETag" && meta.ETag != null)
                prop.SetValue(obj, Convert.ChangeType(meta.ETag, prop.PropertyType));
            else if (prop.Name == "LastModified" && meta.LastModified.HasValue)
                prop.SetValue(obj, Convert.ChangeType(meta.LastModified.Value, prop.PropertyType));

        return obj;
    }

    #region 键选择

    public StorageQuery<T> Key(string key)
    {
        _key = key;
        return this;
    }

    public StorageQuery<T> Key(Func<T, string> keySelector)
    {
        _keySelector = keySelector;
        return this;
    }

    public StorageQuery<T> Prefix(string prefix)
    {
        _prefix = prefix;
        return this;
    }

    #endregion

    #region 读取

    public async Task<Stream> GetAsync()
    {
        if (_key == null) throw new InvalidOperationException("必须指定对象键");

        return await _backend.GetAsync(_key);
    }

    public async Task<byte[]> GetBytesAsync()
    {
        using var stream = await GetAsync();
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms);
        return ms.ToArray();
    }

    public async Task<string> GetTextAsync()
    {
        var bytes = await GetBytesAsync();
        return Encoding.UTF8.GetString(bytes);
    }

    public async Task<T?> GetMetadataAsync()
    {
        if (_key == null) throw new InvalidOperationException("必须指定对象键");

        var meta = await _backend.GetMetadataAsync(_key);
        if (meta == null) return null;

        return MapMetadata(meta);
    }

    public async Task<bool> ExistsAsync()
    {
        if (_key == null) throw new InvalidOperationException("必须指定对象键");

        return await _backend.ExistsAsync(_key);
    }

    #endregion

    #region 写入

    public StorageQuery<T> Data(Stream data)
    {
        _data = data;
        return this;
    }

    public StorageQuery<T> Data(byte[] data)
    {
        _data = new MemoryStream(data);
        return this;
    }

    public StorageQuery<T> Data(string text)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        _data = new MemoryStream(bytes);
        _contentType = "text/plain; charset=utf-8";
        return this;
    }

    public StorageQuery<T> ContentType(string contentType)
    {
        _contentType = contentType;
        return this;
    }

    public async Task PutAsync()
    {
        if (_data == null) throw new InvalidOperationException("必须指定数据流");

        var key = _key ?? throw new InvalidOperationException("必须指定对象键");
        await _backend.PutAsync(key, _data, _contentType);
    }

    public async Task PutAsync(string key, Stream data, string? contentType = null)
    {
        await _backend.PutAsync(key, data, contentType);
    }

    public async Task PutAsync(string key, byte[] data, string? contentType = null)
    {
        using var stream = new MemoryStream(data);
        await _backend.PutAsync(key, stream, contentType);
    }

    #endregion
}

public sealed class StorageObjectInfo
{
    public string Key { get; init; } = "";
    public long Size { get; init; }
    public DateTimeOffset? LastModified { get; init; }
    public string? ETag { get; init; }
}

public sealed class StorageMetadata
{
    public string Key { get; init; } = "";
    public long? Size { get; init; }
    public string? ContentType { get; init; }
    public string? ETag { get; init; }
    public DateTimeOffset? LastModified { get; init; }
    public IReadOnlyDictionary<string, string> UserMetadata { get; init; } = new Dictionary<string, string>();
}

public interface IStorageBackend
{
    Task<StorageMetadata?> GetMetadataAsync(string key);
    Task<Stream> GetAsync(string key);
    Task PutAsync(string key, Stream data, string? contentType = null);
    Task<bool> DeleteAsync(string key);
    Task<bool> ExistsAsync(string key);
    Task<IReadOnlyList<StorageObjectInfo>> ListAsync(string? prefix = null);
    Task<string> GetPresignedUrlAsync(string key, TimeSpan expiry);
}