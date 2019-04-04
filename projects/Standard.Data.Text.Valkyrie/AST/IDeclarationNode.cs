using Std.Data.Text.Syntax;
using Std.Data.Text.Valkyrie.AST.Declaration;

namespace Std.Data.Text.Valkyrie.AST;

/// <summary>
///     声明节点接口，统一访问 Name、Attributes、Span
/// </summary>
public interface IDeclarationNode
{
    Annotations annotations { get; }

    /// <summary>
    ///     文档注释
    /// </summary>
    string document_text => annotations.document_text();

    /// <summary>
    ///     属性列表
    /// </summary>
    IReadOnlyList<AttributeItem> attributes => annotations.attributes();

    /// <summary>
    ///     修饰符列表
    /// </summary>
    IReadOnlyList<IdentifierNode> modifiers => annotations.modifiers;

    /// <summary>
    ///     声明名称
    /// </summary>
    IdentifierNode? name { get; }

    /// <summary>
    ///     源代码位置范围
    /// </summary>
    TextSpan span { get; }
}