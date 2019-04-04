using Core.Data.Search;

namespace Std.Data.Search;

/// <summary>
///     基于内存的搜索索引实现，使用简单的分词匹配进行全文搜索。
/// </summary>
/// <typeparam name="T">文档类型。</typeparam>
public sealed class InMemorySearchIndex<T> : ISearchIndex<T>
{
    private readonly IFieldAnalyzer _analyzer;
    private readonly List<T> _documents = [];
    private readonly List<SearchFieldDefinition> _full_text_field_names = [];

    /// <summary>
    ///     初始化内存搜索索引的新实例。
    /// </summary>
    /// <param name="analyzer">字段分词器。</param>
    public InMemorySearchIndex(IFieldAnalyzer analyzer)
    {
        _analyzer = analyzer;
    }

    /// <summary>
    ///     异步索引文档。
    /// </summary>
    /// <param name="document">要索引的文档。</param>
    public Task index(T document)
    {
        _documents.Add(document);
        return Task.CompletedTask;
    }

    /// <summary>
    ///     异步搜索文档。
    /// </summary>
    /// <param name="query">搜索查询。</param>
    /// <returns>搜索结果列表。</returns>
    public Task<IReadOnlyList<T>> search(string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return Task.FromResult<IReadOnlyList<T>>([.. _documents]);

        var queryTokens = _analyzer.tokenize(query.ToLowerInvariant());
        var results = new List<T>();

        foreach (var document in _documents)
            if (matches_document(document, queryTokens))
                results.Add(document);

        return Task.FromResult<IReadOnlyList<T>>(results);
    }

    /// <summary>
    ///     注册全文索引字段。
    /// </summary>
    /// <param name="fieldName">字段名称。</param>
    /// <param name="analyzerName">分词器名称。</param>
    public void register_full_text_field(string fieldName, string? analyzerName = null)
    {
        _full_text_field_names.Add(new SearchFieldDefinition(fieldName, analyzerName));
    }

    /// <summary>
    ///     异步移除文档索引。
    /// </summary>
    /// <param name="document">要移除的文档。</param>
    public Task remove(T document)
    {
        _documents.Remove(document);
        return Task.CompletedTask;
    }

    /// <summary>
    ///     判断文档是否匹配查询词元。
    /// </summary>
    private bool matches_document(T document, string[] queryTokens)
    {
        var type = typeof(T);

        foreach (var fieldDef in _full_text_field_names)
        {
            var prop = type.GetProperty(fieldDef.field_name);

            var value = prop?.GetValue(document);

            if (value is not string text) continue;

            var fieldTokens = _analyzer.tokenize(text.ToLowerInvariant());

            foreach (var queryToken in queryTokens)
            foreach (var fieldToken in fieldTokens)
                if (fieldToken.Contains(queryToken))
                    return true;
        }

        return false;
    }

    private readonly struct SearchFieldDefinition
    {
        public readonly string field_name;
        public readonly string? analyzer_name;

        public SearchFieldDefinition(string fieldName, string? analyzerName)
        {
            field_name = fieldName;
            analyzer_name = analyzerName;
        }
    }
}