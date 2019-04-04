namespace Hermes.YYDB.Query;

public sealed class CacheQuery<T> where T : class
{
    private readonly ICacheBackend _backend;
    private TimeSpan? _expiry;
    private string? _key;
    private Func<T, string>? _keySelector;
    private T? _value;

    public CacheQuery(ICacheBackend backend)
    {
        _backend = backend;
    }

    #region 批量

    public async Task InvalidatePatternAsync(string pattern)
    {
        await _backend.InvalidatePatternAsync(pattern);
    }

    #endregion

    private string ResolveKey(T value)
    {
        if (_key != null) return _key;

        if (_keySelector != null) return _keySelector(value);

        var type = typeof(T);
        var idProp = type.GetProperty("Id") ?? type.GetProperty("ID") ?? type.GetProperty("Key");
        if (idProp != null)
        {
            var idValue = idProp.GetValue(value);
            if (idValue != null) return $"{type.Name}:{idValue}";
        }

        throw new InvalidOperationException("无法自动推断缓存键，请使用 Key() 或 Key(selector) 指定");
    }

    #region 键选择

    public CacheQuery<T> Key(string key)
    {
        _key = key;
        return this;
    }

    public CacheQuery<T> Key(Func<T, string> keySelector)
    {
        _keySelector = keySelector;
        return this;
    }

    #endregion

    #region 读取

    public async Task<T?> GetAsync()
    {
        if (_key == null) throw new InvalidOperationException("必须指定缓存键");

        return await _backend.GetAsync<T>(_key);
    }

    public async Task<T> GetOrSetAsync(Func<Task<T>> factory, TimeSpan? expiry = null)
    {
        if (_key == null) throw new InvalidOperationException("必须指定缓存键");

        var cached = await _backend.GetAsync<T>(_key);
        if (cached != null) return cached;

        var value = await factory();
        await _backend.SetAsync(_key, value, expiry ?? _expiry);
        return value;
    }

    #endregion

    #region 写入

    public CacheQuery<T> Value(T value)
    {
        _value = value;
        return this;
    }

    public CacheQuery<T> Expire(TimeSpan expiry)
    {
        _expiry = expiry;
        return this;
    }

    public CacheQuery<T> ExpireMinutes(int minutes)
    {
        _expiry = TimeSpan.FromMinutes(minutes);
        return this;
    }

    public CacheQuery<T> ExpireHours(int hours)
    {
        _expiry = TimeSpan.FromHours(hours);
        return this;
    }

    public async Task SetAsync()
    {
        if (_value == null) throw new InvalidOperationException("必须指定缓存值");

        var key = ResolveKey(_value);
        await _backend.SetAsync(key, _value, _expiry);
    }

    public async Task SetAsync(T value, TimeSpan? expiry = null)
    {
        var key = ResolveKey(value);
        await _backend.SetAsync(key, value, expiry ?? _expiry);
    }

    #endregion

    #region 删除

    public async Task<bool> DeleteAsync()
    {
        if (_key == null) throw new InvalidOperationException("必须指定缓存键");

        return await _backend.DeleteAsync(_key);
    }

    public async Task<bool> ExistsAsync()
    {
        if (_key == null) throw new InvalidOperationException("必须指定缓存键");

        return await _backend.ExistsAsync(_key);
    }

    #endregion
}

public interface ICacheBackend
{
    Task<T?> GetAsync<T>(string key);
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null);
    Task<bool> DeleteAsync(string key);
    Task<bool> ExistsAsync(string key);
    Task InvalidatePatternAsync(string pattern);
}