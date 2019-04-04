namespace Nyar.Language.Valkyrie.Compiler.Pipeline;

/// <summary>
///     语义缓存条目，按"已特化 AST 的哈希"缓存语义模型。
/// </summary>
public sealed record SemanticCacheEntry
{
    /// <summary>
    ///     语义模型的序列化形式
    /// </summary>
    public byte[] semantic_data { get; init; } = [];

    /// <summary>
    ///     AST 哈希
    /// </summary>
    public string ast_hash { get; init; } = string.Empty;

    /// <summary>
    ///     目标 CanonicalTriple
    /// </summary>
    public string canonical_triple { get; init; } = string.Empty;

    /// <summary>
    ///     缓存创建时间
    /// </summary>
    public DateTimeOffset created_at { get; init; }
}