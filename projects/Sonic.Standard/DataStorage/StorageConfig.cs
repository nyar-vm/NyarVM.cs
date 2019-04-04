namespace Std.DataStorage;

/// <summary>
///     存储后端配置，描述数据类型的持久化目�?///
/// </summary>
public sealed class StorageConfig
{
    /// <summary>
    ///     初始化存储配�?    ///
    /// </summary>
    /// <param name="backend">存储后端类型标识</param>
    /// <param name="tableName">目标表名或集合名，可�?/param>
    public StorageConfig(string backend, string? tableName = null)
    {
        this.backend = backend;
        table_name = tableName;
    }

    /// <summary>
    ///     存储后端类型标识，例�?"relational"�?kv"�?file"
    /// </summary>
    public string backend { get; }

    /// <summary>
    ///     目标表名或集合名，为 null 时由后端自动推断
    /// </summary>
    public string? table_name { get; }
}