using System.Collections.Immutable;

namespace Nyar.VM.TextVM.Compiler;

/// <summary>
/// 扫描 AST 并确定复杂度等级的分类器。
/// </summary>
public static class ComplexityClassifier
{
    /// <summary>
    /// 对指定 AST 节点进行复杂度分类。
    /// </summary>
    /// <param name="node">要分析的 AST 根节点。</param>
    /// <returns>复杂度等级。</returns>
    public static Complexity Classify(AstNode node)
    {
        if (node is LiteralNode)
        {
            return Complexity.Literal;
        }

        if (ContainsBackref(node))
        {
            return Complexity.ContextFree;
        }

        return Complexity.Regular;
    }

    /// <summary>
    /// 递归检查 AST 中是否包含反向引用节点。
    /// </summary>
    private static Boolean ContainsBackref(AstNode node)
    {
        if (node is BackrefNode)
        {
            return true;
        }

        return node switch
        {
            ConcatNode n => AnyChildContainsBackref(n.Children),
            AltNode n => ContainsBackref(n.Left) || ContainsBackref(n.Right),
            StarNode n => ContainsBackref(n.Inner),
            PlusNode n => ContainsBackref(n.Inner),
            OptionalNode n => ContainsBackref(n.Inner),
            IntersectNode n => ContainsBackref(n.Left) || ContainsBackref(n.Right),
            ComplementNode n => ContainsBackref(n.Inner),
            DifferenceNode n => ContainsBackref(n.Left) || ContainsBackref(n.Right),
            CaptureNode n => ContainsBackref(n.Inner),
            _ => false,
        };
    }

    /// <summary>
    /// 检查子节点数组中是否包含反向引用。
    /// </summary>
    private static Boolean AnyChildContainsBackref(ImmutableArray<AstNode>.Builder nodes)
    {
        foreach (AstNode child in nodes)
        {
            if (ContainsBackref(child))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 检查子节点数组中是否包含反向引用。
    /// </summary>
    private static Boolean AnyChildContainsBackref(ImmutableArray<AstNode> nodes)
    {
        foreach (AstNode child in nodes)
        {
            if (ContainsBackref(child))
            {
                return true;
            }
        }

        return false;
    }
}
