namespace Core.Database;

/// <summary>
///     索引选项
/// </summary>
public sealed class IndexOptions
{
    /// <summary>
    ///     是否为唯一索引
    /// </summary>
    public bool Unique { get; init; }
}