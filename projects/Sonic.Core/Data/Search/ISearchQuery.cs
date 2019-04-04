namespace Core.Data.Search;

/// <summary>
///     搜索查询接口
/// </summary>
public interface ISearchQuery
{
    /// <summary>
    ///     添加过滤条件
    /// </summary>
    /// <param name="field">字段名</param>
    /// <param name="value">过滤值</param>
    /// <returns>当前查询实例</returns>
    ISearchQuery with_filter(string field, object value);

    /// <summary>
    ///     添加排序条件
    /// </summary>
    /// <param name="field">字段名</param>
    /// <param name="descending">是否降序，默认为 false</param>
    /// <returns>当前查询实例</returns>
    ISearchQuery with_sort(string field, bool descending = false);

    /// <summary>
    ///     设置分页参数
    /// </summary>
    /// <param name="offset">偏移量</param>
    /// <param name="limit">每页数量</param>
    /// <returns>当前查询实例</returns>
    ISearchQuery with_paging(int offset, int limit);
}