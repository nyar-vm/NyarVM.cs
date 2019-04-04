using Std.Data.Text.Syntax;
using Std.Data.Text.Valkyrie.Parser;

namespace Std.Data.Text.Valkyrie.AST;

/// <summary>
///     AST 节点基类，所有 AST 节点的抽象根类型
/// </summary>
/// <para>继承体系：</para>
/// <list type="bullet">
///     <item>
///         声明类：<see cref="DeclareMicro" />、<see cref="DeclareClass" />、<see cref="DeclareStructure" />、
///         <see cref="DeclareEnums" /> 等
///     </item>
///     <item>
///         语句类：<see cref="IfStatement" />、<see cref="LoopStatement" />、<see cref="WhileStatement" />、
///         <see cref="UntilStatement" />、
///         <see cref="ReturnStatement" /> 等
///     </item>
///     <item>表达式类：<see cref="TermLiteralNode" />、<see cref="BinaryExpr" />、<see cref="AnonymousMicro" /> 等</item>
///     <item>
///         模式类：<see cref="PatternLiteralNumberNode" />、<see cref="DeclarationPattern" />、<see cref="PatternNode" />、
///         <see cref="PatternLiteralWildcardNode" />
///     </item>
/// </list>
public abstract record ValkyrieNode
{
    /// <summary>
    ///     受保护构造函数，用于派生类在构造函数中设置 <see cref="span" />
    /// </summary>
    protected ValkyrieNode()
    {
    }

    /// <summary>
    ///     受保护构造函数，用于派生类在构造函数中设置 <see cref="span" />
    /// </summary>
    protected ValkyrieNode(TextSpan span)
    {
        this.span = span;
    }

    /// <summary>
    ///     节点类型标识，默认通过类型名称自动映射到 <see cref="ValkyrieNodeType" /> 枚举
    /// </summary>
    public virtual ValkyrieNodeType type => Enum.TryParse<ValkyrieNodeType>(GetType().Name, out var result)
        ? result
        : ValkyrieNodeType.unknown;

    /// <summary>
    ///     源代码位置范围（偏移量 + 长度），用于错误报告和 IDE 定位
    /// </summary>
    public TextSpan span { get; init; }
}
