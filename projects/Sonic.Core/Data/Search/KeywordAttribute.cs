using System;

namespace Core.Data.Search;

/// <summary>
///     标记属性为关键词字段
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class KeywordAttribute : Attribute
{
}