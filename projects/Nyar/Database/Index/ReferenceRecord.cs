namespace Nyar.Database.Index;

/// <summary>
///     引用索引记录
/// </summary>
public sealed record ReferenceRecord
{
    /// <summary>
    ///     被引用符号的唯一标识
    /// </summary>
    public SymbolId symbol_id { get; init; }

    /// <summary>
    ///     引用所在的文件 URI
    /// </summary>
    public string file_uri { get; init; } = "";

    /// <summary>
    ///     引用的位置
    /// </summary>
    public Loc location { get; init; }

    /// <summary>
    ///     引用种类
    /// </summary>
    public ReferenceKind kind { get; init; }
}