namespace Hermes.YYDB.Query;

/// <summary>
///     全文搜索查询
/// </summary>
public sealed class FullTextSearchQuery : QueryExpression
{
    public FullTextSearchQuery(
        string targetTypeName,
        IReadOnlyList<string> searchFields,
        string searchTerm,
        string language = "simple",
        FullTextSearchMode mode = FullTextSearchMode.Plain,
        QueryPredicate? predicate = null,
        QueryOrdering? ordering = null,
        QueryPagination? pagination = null,
        bool orderByRank = true)
        : base(targetTypeName)
    {
        SearchFields = searchFields;
        SearchTerm = searchTerm;
        Language = language;
        Mode = mode;
        Predicate = predicate;
        Ordering = ordering;
        Pagination = pagination;
        OrderByRank = orderByRank;
    }

    public override string QueryKind => "fulltext_search";

    /// <summary>
    ///     搜索字段列表
    /// </summary>
    public IReadOnlyList<string> SearchFields { get; }

    /// <summary>
    ///     搜索关键词
    /// </summary>
    public string SearchTerm { get; }

    /// <summary>
    ///     搜索语言配置（PostgreSQL ts_vector 配置名称，如 'simple'、'english'、'chinese'）
    /// </summary>
    public string Language { get; }

    /// <summary>
    ///     搜索模式
    /// </summary>
    public FullTextSearchMode Mode { get; }

    /// <summary>
    ///     额外过滤条件
    /// </summary>
    public QueryPredicate? Predicate { get; }

    /// <summary>
    ///     排序规则
    /// </summary>
    public QueryOrdering? Ordering { get; }

    /// <summary>
    ///     分页
    /// </summary>
    public QueryPagination? Pagination { get; }

    /// <summary>
    ///     是否按相关性排序（PostgreSQL ts_rank）
    /// </summary>
    public bool OrderByRank { get; }
}

/// <summary>
///     全文搜索模式
/// </summary>
public enum FullTextSearchMode
{
    /// <summary>
    ///     PLAIN TO_TSQUERY — 简单关键词匹配
    /// </summary>
    Plain,

    /// <summary>
    ///     PHRASE TO_TSQUERY — 短语匹配（PostgreSQL 11+）
    /// </summary>
    Phrase,

    /// <summary>
    ///     WEBSEARCH TO_TSQUERY — Web 搜索语法（PostgreSQL 11+）
    /// </summary>
    WebSearch,

    /// <summary>
    ///     PLAINTO_TSQUERY — 自动 AND 连接
    /// </summary>
    PlainAnd
}