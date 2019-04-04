namespace Nyar.Language.Valkyrie.Compiler.Pipeline;

/// <summary>
///     IR 缓存条目，按模块级缓存 HIR/MIR/LIR。
/// </summary>
public sealed record IrCacheEntry
{
    /// <summary>
    ///     IR 类型（"HIR" / "MIR" / "LIR"）
    /// </summary>
    public string ir_kind { get; init; } = string.Empty;

    /// <summary>
    ///     IR 的序列化形式
    /// </summary>
    public byte[] ir_data { get; init; } = [];

    /// <summary>
    ///     IR 哈希
    /// </summary>
    public string ir_hash { get; init; } = string.Empty;

    /// <summary>
    ///     目标 CanonicalTriple
    /// </summary>
    public string canonical_triple { get; init; } = string.Empty;

    /// <summary>
    ///     缓存创建时间
    /// </summary>
    public DateTimeOffset created_at { get; init; }
}