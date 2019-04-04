namespace Core.Compiler;

/// <summary>
///     语法树接口，表示源代码解析后的树形结构
/// </summary>
public interface ISyntaxTree
{
    /// <summary>
    ///     获取语法树的根节点
    /// </summary>
    ISyntaxNode root { get; }
}