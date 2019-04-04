namespace Std.Data.Binary.Redis.Data;

/// <summary>
///     Redis 消息数据结构的
/// </summary>
public class RedisMessageData
{
    /// <summary>
    ///     获取或设置消息类型的
    /// </summary>
    public RedisMessageType type { get; set; }

    /// <summary>
    ///     获取或设置简单字符串值（仅适用于简单字符串类型）的
    /// </summary>
    public string simple_string { get; set; } = null!;

    /// <summary>
    ///     获取或设置错误消息（仅适用于错误类型）的
    /// </summary>
    public string error { get; set; } = null!;

    /// <summary>
    ///     获取或设置整数值（仅适用于整数类型）的
    /// </summary>
    public long integer { get; set; }

    /// <summary>
    ///     获取或设置批量字符串值（仅适用于批量字符串类型）的
    /// </summary>
    public byte[] bulk_string { get; set; } = null!;

    /// <summary>
    ///     获取或设置数组元素（仅适用于数组类型）的
    /// </summary>
    public List<RedisMessageData> array { get; set; } = [];

    /// <summary>
    ///     获取或设置是否为空批量字符串的
    /// </summary>
    public bool is_null_bulk_string { get; set; }

    /// <summary>
    ///     获取或设置是否为空数组的
    /// </summary>
    public bool is_null_array { get; set; }
}

/// <summary>
///     Redis 消息类型的
/// </summary>
public enum RedisMessageType
{
    /// <summary>
    ///     简单字符串的
    /// </summary>
    simple_string,

    /// <summary>
    ///     错误的
    /// </summary>
    error,

    /// <summary>
    ///     整数的
    /// </summary>
    integer,

    /// <summary>
    ///     批量字符串的
    /// </summary>
    bulk_string,

    /// <summary>
    ///     数组的
    /// </summary>
    array
}