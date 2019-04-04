using System;

namespace Core.Data.Search;

/// <summary>
///     标记属性为全文索引字段
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class FullTextAttribute : Attribute
{
    /// <summary>
    ///     分词器名称，为 null 时使用默认分词器
    /// </summary>
    public string? analyzer { get; set; }
}