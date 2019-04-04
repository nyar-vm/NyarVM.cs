namespace Std.Data.Search;

/// <summary>
///     搜索字段定义，描述索引字段的名称、类型和分词器配置。
/// </summary>
public sealed class SearchFieldDefinition
{
    /// <summary>
    ///     初始化搜索字段定义。
    /// </summary>
    /// <param name="name">字段名称。</param>
    /// <param name="fieldType">字段类型。</param>
    /// <param name="analyzer">分词器名称。</param>
    public SearchFieldDefinition(string name, SearchFieldType fieldType, string? analyzer = null)
    {
        this.name = name;
        field_type = fieldType;
        this.analyzer = analyzer;
    }

    /// <summary>
    ///     字段名称。
    /// </summary>
    public string name { get; }

    /// <summary>
    ///     字段类型。
    /// </summary>
    public SearchFieldType field_type { get; }

    /// <summary>
    ///     分词器名称，为 null 时使用默认分词器。
    /// </summary>
    public string? analyzer { get; }
}