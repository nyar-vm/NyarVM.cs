namespace Nyar.Language.Valkyrie.Compiler.Pipeline;

/// <summary>
///     Staging 缓存条目，按 "文件哈希 + CanonicalTriple + feature set" 缓存。
/// </summary>
public sealed record StageCacheEntry
{
    /// <summary>
    ///     特化后的 tokenstream 的序列化形式
    /// </summary>
    public byte[] staged_token_data { get; init; } = [];

    /// <summary>
    ///     文件内容哈希
    /// </summary>
    public string content_hash { get; init; } = string.Empty;

    /// <summary>
    ///     目标 CanonicalTriple
    /// </summary>
    public string canonical_triple { get; init; } = string.Empty;

    /// <summary>
    ///     缓存创建时间
    /// </summary>
    public DateTimeOffset created_at { get; init; }
}