using System.Collections.Generic;

namespace Core.Compiler;

/// <summary>
///     语法节点接口，表示语法树中的一个节点
/// </summary>
public interface ISyntaxNode
{
    /// <summary>
    ///     获取该节点的子节点只读列表
    /// </summary>
    IReadOnlyList<ISyntaxNode> children { get; }

    /// <summary>
    ///     获取该节点的语法类别
    /// </summary>
    SyntaxKind kind { get; }
}