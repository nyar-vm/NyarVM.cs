namespace Nyar.VM.TextVM.Compiler;

/// <summary>
/// 基于启发式方法估计 DFA 状态数量，无需实际构建 DFA。
/// </summary>
public static class StateExplosionEstimator
{
    /// <summary>
    /// 估计指定 AST 节点对应的 DFA 状态数量等级。
    /// </summary>
    /// <param name="node">要分析的 AST 根节点。</param>
    /// <returns>DFA 状态数量估计等级。</returns>
    public static DfaEstimate Estimate(AstNode node)
    {
        Int32 nodeCount = CountNodes(node);
        Int32 maxNesting = MaxNestingDepth(node);

        if (nodeCount < 10 && maxNesting <= 2)
        {
            return DfaEstimate.Small;
        }

        if (nodeCount < 50)
        {
            return DfaEstimate.Medium;
        }

        return DfaEstimate.Large;
    }

    /// <summary>
    /// 递归统计 AST 中的节点总数。
    /// </summary>
    private static Int32 CountNodes(AstNode node)
    {
        Int32 count = 1;

        switch (node)
        {
            case ConcatNode concatNode:
                foreach (AstNode child in concatNode.Children)
                {
                    count += CountNodes(child);
                }
                break;

            case AltNode altNode:
                count += CountNodes(altNode.Left);
                count += CountNodes(altNode.Right);
                break;

            case StarNode starNode:
                count += CountNodes(starNode.Inner);
                break;

            case PlusNode plusNode:
                count += CountNodes(plusNode.Inner);
                break;

            case OptionalNode optionalNode:
                count += CountNodes(optionalNode.Inner);
                break;

            case IntersectNode intersectNode:
                count += CountNodes(intersectNode.Left);
                count += CountNodes(intersectNode.Right);
                break;

            case ComplementNode complementNode:
                count += CountNodes(complementNode.Inner);
                break;

            case DifferenceNode differenceNode:
                count += CountNodes(differenceNode.Left);
                count += CountNodes(differenceNode.Right);
                break;

            case CaptureNode captureNode:
                count += CountNodes(captureNode.Inner);
                break;

            case LiteralNode:
            case CharClassNode:
            case BackrefNode:
            case AnchorNode:
            case AnyNode:
                break;
        }

        return count;
    }

    /// <summary>
    /// 递归计算 AST 中的最大嵌套深度。
    /// </summary>
    private static Int32 MaxNestingDepth(AstNode node)
    {
        Int32 maxChildDepth = 0;

        switch (node)
        {
            case ConcatNode concatNode:
                foreach (AstNode child in concatNode.Children)
                {
                    Int32 d = MaxNestingDepth(child);
                    if (d > maxChildDepth)
                    {
                        maxChildDepth = d;
                    }
                }
                break;

            case AltNode altNode:
                maxChildDepth = Math.Max(MaxNestingDepth(altNode.Left), MaxNestingDepth(altNode.Right));
                break;

            case StarNode starNode:
                maxChildDepth = MaxNestingDepth(starNode.Inner);
                break;

            case PlusNode plusNode:
                maxChildDepth = MaxNestingDepth(plusNode.Inner);
                break;

            case OptionalNode optionalNode:
                maxChildDepth = MaxNestingDepth(optionalNode.Inner);
                break;

            case IntersectNode intersectNode:
                maxChildDepth = Math.Max(MaxNestingDepth(intersectNode.Left), MaxNestingDepth(intersectNode.Right));
                break;

            case ComplementNode complementNode:
                maxChildDepth = MaxNestingDepth(complementNode.Inner);
                break;

            case DifferenceNode differenceNode:
                maxChildDepth = Math.Max(MaxNestingDepth(differenceNode.Left), MaxNestingDepth(differenceNode.Right));
                break;

            case CaptureNode captureNode:
                maxChildDepth = MaxNestingDepth(captureNode.Inner);
                break;

            case LiteralNode:
            case CharClassNode:
            case BackrefNode:
            case AnchorNode:
            case AnyNode:
                break;
        }

        return maxChildDepth + 1;
    }
}
