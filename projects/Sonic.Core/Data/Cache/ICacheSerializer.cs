namespace Core.Data.Cache;

/// <summary>
///     缓存序列化器接口
/// </summary>
public interface ICacheSerializer
{
    /// <summary>
    ///     序列化值为字节数组
    /// </summary>
    /// <typeparam name="T">值类型</typeparam>
    /// <param name="value">要序列化的值</param>
    /// <returns>序列化后的字节数组</returns>
    byte[] serialize<T>(T value);

    /// <summary>
    ///     反序列化字节数组为值
    /// </summary>
    /// <typeparam name="T">值类型</typeparam>
    /// <param name="data">字节数组</param>
    /// <returns>反序列化后的值</returns>
    T deserialize<T>(byte[] data);
}