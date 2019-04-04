namespace Core.Compiler;

/// <summary>
///     符号表接口，提供按名称查找符号的能力
/// </summary>
public interface ISymbolTable
{
    /// <summary>
    ///     根据名称查找符号
    /// </summary>
    /// <param name="name">符号名称</param>
    /// <returns>找到的符号，若未找到则返回 null</returns>
    ISymbol? lookup(string name);
}