namespace Nyar.Language.Valkyrie.Compiler.Pipeline;

/// <summary>
///     Token 缓存条目，按文件哈希缓存原始 tokenstream。
/// </summary>
public sealed record TokenCacheEntry
{
    /// <summary>
    ///     原始 tokenstream 的序列化形式
    /// </summary>
    public byte[] token_data { get; init; } = [];

    /// <summary>
    ///     文件内容哈希
    /// </summary>
    public string content_hash { get; init; } = string.Empty;

    /// <summary>
    ///     缓存创建时间
    /// </summary>
    public DateTimeOffset created_at { get; init; }
}