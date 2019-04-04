using System.Text.Json;
using Core.Data.Cache;

namespace Std.Data.Cache;

/// <summary>
///     基于 JSON 的缓存序列化器实现，使用简单的类型标记前缀进行序列化和反序列化。
/// </summary>
public sealed class JsonCacheSerializer : ICacheSerializer
{
    /// <summary>
    ///     序列化值为字节数组。
    /// </summary>
    /// <typeparam name="T">值类型。</typeparam>
    /// <param name="value">要序列化的值。</param>
    /// <returns>序列化后的字节数组。</returns>
    public byte[] serialize<T>(T value)
    {
        if (value is null) return Encoding.UTF8.GetBytes("null");

        var json = JsonSerializer.Serialize(value);
        return Encoding.UTF8.GetBytes(json);
    }

    /// <summary>
    ///     反序列化字节数组为值。
    /// </summary>
    /// <typeparam name="T">值类型。</typeparam>
    /// <param name="data">字节数组。</param>
    /// <returns>反序列化后的值。</returns>
    public T deserialize<T>(byte[] data)
    {
        var json = Encoding.UTF8.GetString(data);

        if (json == "null") return default!;

        return JsonSerializer.Deserialize<T>(json)!;
    }
}