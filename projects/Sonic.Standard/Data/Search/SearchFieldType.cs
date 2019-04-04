namespace Std.Data.Search;

/// <summary>
///     搜索字段类型枚举，定义索引字段的分类。
/// </summary>
public enum SearchFieldType
{
    /// <summary>
    ///     全文索引字段，支持分词搜索。
    /// </summary>
    full_text,

    /// <summary>
    ///     关键词字段，支持精确匹配。
    /// </summary>
    keyword
}