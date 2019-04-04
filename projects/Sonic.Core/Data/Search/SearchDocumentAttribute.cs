using System;

namespace Core.Data.Search;

/// <summary>
///     标记类为搜索文档
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class SearchDocumentAttribute : Attribute
{
    /// <summary>
    ///     索引名称，为 null 时使用类名
    /// </summary>
    public string? index_name { get; set; }
}