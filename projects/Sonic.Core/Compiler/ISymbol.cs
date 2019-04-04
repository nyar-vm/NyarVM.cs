namespace Core.Compiler;

/// <summary>
///     符号接口，表示程序中的一个命名实体
/// </summary>
public interface ISymbol
{
    /// <summary>
    ///     获取符号名称
    /// </summary>
    string name { get; }

    /// <summary>
    ///     获取符号类别
    /// </summary>
    SymbolKind kind { get; }
}