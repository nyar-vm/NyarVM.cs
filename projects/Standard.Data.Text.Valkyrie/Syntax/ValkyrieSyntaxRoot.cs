using Std.Data.Text.Syntax;

namespace Std.Data.Text.Valkyrie.Syntax;

/// <summary>
///     Valkyrie 语法树根节点，对应 CompilationUnit
/// </summary>
public sealed class ValkyrieSyntaxRoot : SyntaxRoot
{
    private readonly SyntaxTree _syntax_tree;

    /// <summary>
    ///     初始化 Valkyrie 语法根节点
    /// </summary>
    public ValkyrieSyntaxRoot(GreenNode green, SyntaxTree tree, int offset)
        : base(green, tree, offset, "Valkyrie")
    {
        child_count = green.child_count;
        _syntax_tree = tree;
    }

    /// <summary>
    ///     子节点数量
    /// </summary>
    public int child_count { get; }

    /// <summary>
    ///     所有顶层声明节点
    /// </summary>
    public IReadOnlyList<ValkyrieDeclarationSyntax> declarations
    {
        get
        {
            var decls = new List<ValkyrieDeclarationSyntax>();
            for (var i = 0; i < child_count; i++)
                try
                {
                    var child = child_node<ValkyrieDeclarationSyntax>(i);
                    decls.Add(child);
                }
                catch
                {
                    // 子节点不是声明类型，跳过
                }

            return decls;
        }
    }

    /// <summary>
    ///     获取指定索引的子节点
    /// </summary>
    public SyntaxNode get_child(int index)
    {
        return child_node<SyntaxNode>(index);
    }

    public override VisitRecursionMode accept(SyntaxVisitor visitor)
    {
        return visitor.visit_default(this);
    }
}