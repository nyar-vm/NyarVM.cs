namespace Nyar.Database.Index;

/// <summary>
///     符号索引记录
/// </summary>
public sealed record SymbolRecord
{
    /// <summary>
    ///     符号唯一标识
    /// </summary>
    public SymbolId id { get; init; }

    /// <summary>
    ///     符号名称
    /// </summary>
    public string name { get; init; } = "";

    /// <summary>
    ///     符号种类
    /// </summary>
    public SymbolKind kind { get; init; }

    /// <summary>
    ///     符号所在文件的 URI
    /// </summary>
    public string file_uri { get; init; } = "";

    /// <summary>
    ///     符号定义的位置
    /// </summary>
    public Loc location { get; init; }

    /// <summary>
    ///     符号的额外元数据（JSON 格式）
    /// </summary>
    public string? metadata { get; init; }

    /// <summary>
    ///     符号的可访问性
    /// </summary>
    public SymbolAccessibility accessibility { get; init; }
}