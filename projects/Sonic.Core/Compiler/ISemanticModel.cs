namespace Core.Compiler;

/// <summary>
///     语义模型接口，提供语义分析的结果访问
/// </summary>
public interface ISemanticModel
{
    /// <summary>
    ///     获取符号表
    /// </summary>
    ISymbolTable symbols { get; }
}