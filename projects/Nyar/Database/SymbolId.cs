namespace Nyar.Database;

/// <summary>
///     符号唯一标识，由文件 URI、符号名称和符号种类组成的复合键
/// </summary>
public readonly record struct SymbolId
{
    /// <summary>
    ///     初始化符号唯一标识
    /// </summary>
    public SymbolId()
    {
    }

    /// <summary>
    ///     符号所在文件的 URI
    /// </summary>
    public string file_uri { get; init; } = "";

    /// <summary>
    ///     符号名称
    /// </summary>
    public string name { get; init; } = "";

    /// <summary>
    ///     符号种类名称
    /// </summary>
    public string kind_name { get; init; } = "";

    /// <summary>
    ///     从文件 URI、名称和符号种类创建符号唯一标识
    /// </summary>
    /// <param name="fileUri">符号所在文件的 URI。</param>
    /// <param name="name">符号名称。</param>
    /// <param name="kind">符号种类。</param>
    /// <returns>符号唯一标识。</returns>
    public static SymbolId create(string fileUri, string name, SymbolKind kind)
    {
        return new SymbolId
        {
            file_uri = fileUri,
            name = name,
            kind_name = kind.ToString()
        };
    }
}