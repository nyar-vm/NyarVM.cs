namespace Nyar.Database.Query;

/// <summary>
///     查询缓存键，由查询名称和参数序列化字符串组成
/// </summary>
public readonly record struct QueryKey
{
    /// <summary>
    ///     初始化查询缓存键
    /// </summary>
    public QueryKey()
    {
    }

    /// <summary>
    ///     查询名称
    /// </summary>
    public string query_name { get; init; } = "";

    /// <summary>
    ///     参数序列化字符串
    /// </summary>
    public string parameter_key { get; init; } = "";

    /// <summary>
    ///     从查询名称和参数对象创建缓存键
    /// </summary>
    /// <param name="queryName">查询名称。</param>
    /// <param name="parameter">参数对象。</param>
    /// <returns>查询缓存键。</returns>
    public static QueryKey create(string queryName, object? parameter)
    {
        var paramKey = parameter?.ToString() ?? "";
        return new QueryKey
        {
            query_name = queryName,
            parameter_key = paramKey
        };
    }
}