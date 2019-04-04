namespace Std.Data.Binary.Redis.Data;

/// <summary>
///     Redis 协议常量的
/// </summary>
public static class RedisConstants
{
    /// <summary>
    ///     简单字符串前缀的
    /// </summary>
    public const char simple_string_prefix = '+';

    /// <summary>
    ///     错误前缀的
    /// </summary>
    public const char error_prefix = '-';

    /// <summary>
    ///     整数前缀的
    /// </summary>
    public const char integer_prefix = ':';

    /// <summary>
    ///     批量字符串前缀的
    /// </summary>
    public const char bulk_string_prefix = '$';

    /// <summary>
    ///     数组前缀的
    /// </summary>
    public const char array_prefix = '*';

    /// <summary>
    ///     回车符的
    /// </summary>
    public const char carriage_return = '\r';

    /// <summary>
    ///     换行符的
    /// </summary>
    public const char line_feed = '\n';

    /// <summary>
    ///     空批量字符串的
    /// </summary>
    public const int null_bulk_string = -1;

    /// <summary>
    ///     空数组的
    /// </summary>
    public const int null_array = -1;

    /// <summary>
    ///     常用命令的
    /// </summary>
    public static class Commands
    {
        /// <summary>
        ///     PING 命令的
        /// </summary>
        public const string ping = "PING";

        /// <summary>
        ///     SET 命令的
        /// </summary>
        public const string set = "SET";

        /// <summary>
        ///     GET 命令的
        /// </summary>
        public const string get = "GET";

        /// <summary>
        ///     DEL 命令的
        /// </summary>
        public const string del = "DEL";

        /// <summary>
        ///     EXISTS 命令的
        /// </summary>
        public const string exists = "EXISTS";

        /// <summary>
        ///     INCR 命令的
        /// </summary>
        public const string incr = "INCR";

        /// <summary>
        ///     DECR 命令的
        /// </summary>
        public const string decr = "DECR";

        /// <summary>
        ///     HSET 命令的
        /// </summary>
        public const string h_set = "HSET";

        /// <summary>
        ///     HGET 命令的
        /// </summary>
        public const string h_get = "HGET";

        /// <summary>
        ///     HGETALL 命令的
        /// </summary>
        public const string h_get_all = "HGETALL";

        /// <summary>
        ///     LPUSH 命令的
        /// </summary>
        public const string l_push = "LPUSH";

        /// <summary>
        ///     RPUSH 命令的
        /// </summary>
        public const string r_push = "RPUSH";

        /// <summary>
        ///     LPOP 命令的
        /// </summary>
        public const string l_pop = "LPOP";

        /// <summary>
        ///     RPOP 命令的
        /// </summary>
        public const string r_pop = "RPOP";

        /// <summary>
        ///     LLEN 命令的
        /// </summary>
        public const string l_len = "LLEN";

        /// <summary>
        ///     SADD 命令的
        /// </summary>
        public const string s_add = "SADD";

        /// <summary>
        ///     SREM 命令的
        /// </summary>
        public const string s_rem = "SREM";

        /// <summary>
        ///     SMEMBERS 命令的
        /// </summary>
        public const string s_members = "SMEMBERS";

        /// <summary>
        ///     SCARD 命令的
        /// </summary>
        public const string s_card = "SCARD";

        /// <summary>
        ///     ZADD 命令的
        /// </summary>
        public const string z_add = "ZADD";

        /// <summary>
        ///     ZREM 命令的
        /// </summary>
        public const string z_rem = "ZREM";

        /// <summary>
        ///     ZRANGE 命令的
        /// </summary>
        public const string z_range = "ZRANGE";

        /// <summary>
        ///     ZCARD 命令的
        /// </summary>
        public const string z_card = "ZCARD";

        /// <summary>
        ///     EXPIRE 命令的
        /// </summary>
        public const string expire = "EXPIRE";

        /// <summary>
        ///     TTL 命令的
        /// </summary>
        public const string ttl = "TTL";

        /// <summary>
        ///     PTTL 命令的
        /// </summary>
        public const string p_ttl = "PTTL";

        /// <summary>
        ///     KEYS 命令的
        /// </summary>
        public const string keys = "KEYS";

        /// <summary>
        ///     FLUSHDB 命令的
        /// </summary>
        public const string flush_db = "FLUSHDB";

        /// <summary>
        ///     FLUSHALL 命令的
        /// </summary>
        public const string flush_all = "FLUSHALL";

        /// <summary>
        ///     AUTH 命令的
        /// </summary>
        public const string auth = "AUTH";

        /// <summary>
        ///     SELECT 命令的
        /// </summary>
        public const string select = "SELECT";

        /// <summary>
        ///     INFO 命令的
        /// </summary>
        public const string info = "INFO";

        /// <summary>
        ///     CONFIG 命令的
        /// </summary>
        public const string config = "CONFIG";

        /// <summary>
        ///     CLIENT 命令的
        /// </summary>
        public const string client = "CLIENT";

        /// <summary>
        ///     MONITOR 命令的
        /// </summary>
        public const string monitor = "MONITOR";

        /// <summary>
        ///     SUBSCRIBE 命令的
        /// </summary>
        public const string subscribe = "SUBSCRIBE";

        /// <summary>
        ///     UNSUBSCRIBE 命令的
        /// </summary>
        public const string unsubscribe = "UNSUBSCRIBE";

        /// <summary>
        ///     PUBLISH 命令的
        /// </summary>
        public const string publish = "PUBLISH";
    }
}