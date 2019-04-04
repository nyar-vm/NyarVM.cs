namespace Std.Database.Core;

/// <summary>
///     预定义键模式，提供类型安全的键创建方法
/// </summary>
internal static class DatabaseKeyPatterns
{
    /// <summary>
    ///     用户键
    /// </summary>
    public static DatabaseKey user(string userId)
    {
        return $"user:{userId}";
    }

    /// <summary>
    ///     用户资料键
    /// </summary>
    public static DatabaseKey user_profile(string userId)
    {
        return $"user:{userId}:profile";
    }

    /// <summary>
    ///     用户设置键
    /// </summary>
    public static DatabaseKey user_settings(string userId)
    {
        return $"user:{userId}:settings";
    }

    /// <summary>
    ///     订单键
    /// </summary>
    public static DatabaseKey order(string orderId)
    {
        return $"order:{orderId}";
    }

    /// <summary>
    ///     用户订单键
    /// </summary>
    public static DatabaseKey user_order(string userId, string orderId)
    {
        return $"order:{userId}:{orderId}";
    }

    /// <summary>
    ///     会话键
    /// </summary>
    public static DatabaseKey session(string sessionId)
    {
        return $"session:{sessionId}";
    }

    /// <summary>
    ///     配置键
    /// </summary>
    public static DatabaseKey config(string configName)
    {
        return $"config:{configName}";
    }

    /// <summary>
    ///     计数器键
    /// </summary>
    public static DatabaseKey counter(string counterName)
    {
        return $"counter:{counterName}";
    }

    /// <summary>
    ///     锁键
    /// </summary>
    public static DatabaseKey @lock(string resourceName)
    {
        return $"lock:{resourceName}";
    }

    /// <summary>
    ///     集合键前缀
    /// </summary>
    public static DatabaseKey collection(string collectionName)
    {
        return $"collection:{collectionName}";
    }

    /// <summary>
    ///     集合文档键
    /// </summary>
    public static DatabaseKey collection_doc(string collectionName, string docId)
    {
        return $"collection:{collectionName}:{docId}";
    }

    /// <summary>
    ///     索引键前缀
    /// </summary>
    public static DatabaseKey index(string indexName)
    {
        return $"index:{indexName}";
    }

    /// <summary>
    ///     元数据键
    /// </summary>
    public static DatabaseKey meta(string key)
    {
        return $"meta:{key}";
    }

    /// <summary>
    ///     序列键
    /// </summary>
    public static DatabaseKey sequence(string sequenceName)
    {
        return $"seq:{sequenceName}";
    }
}