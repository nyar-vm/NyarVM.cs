using System.Collections.Immutable;
using System.Text;
using Nyar.VM.TextVM;

namespace Nyar.VM.TextVM.Compiler;

/// <summary>
/// 静态分析编排器，协调所有四个分析阶段并生成 <see cref="PatternAttr"/>。
/// </summary>
public static class StaticAnalyzer
{
    /// <summary>
    /// 对指定的 AST 节点执行完整的静态分析。
    /// </summary>
    /// <param name="node">要分析的 AST 根节点。</param>
    /// <param name="encoding">文本编码类型。</param>
    /// <returns>静态分析聚合结果。</returns>
    public static PatternAttr Analyze(AstNode node, TextEncoding encoding)
    {
        Complexity complexity = ComplexityClassifier.Classify(node);
        ImmutableArray<Byte[]> prefixes = PrefixExtractor.Extract(node, encoding);
        Boolean isAsciiOnly = EncodingAnalyzer.IsAsciiOnly(node);
        Boolean isBmpOnly = EncodingAnalyzer.IsBmpOnly(node);
        DfaEstimate stateEstimate = StateExplosionEstimator.Estimate(node);
        Int32 minByteLen = ComputeMinByteLen(node, encoding);

        List<CompileWarning> warnings = [];

        CollectWarnings(node, complexity, warnings);

        return new PatternAttr
        {
            Complexity = complexity,
            PrefixLiterals = prefixes,
            IsAsciiOnly = isAsciiOnly,
            IsBmpOnly = isBmpOnly,
            MinByteLen = minByteLen,
            StateEstimate = stateEstimate,
            Warnings = [.. warnings],
        };
    }

    /// <summary>
    /// 计算模式的最小匹配字节长度。
    /// </summary>
    private static Int32 ComputeMinByteLen(AstNode node, TextEncoding encoding)
    {
        Int32 charLen = ComputeMinCharLen(node);

        if (charLen <= 0)
        {
            return 0;
        }

        return encoding switch
        {
            TextEncoding.Ascii => charLen,
            TextEncoding.Utf8 => charLen,
            TextEncoding.Utf16Le => charLen * 2,
            TextEncoding.Utf16Be => charLen * 2,
            _ => charLen,
        };
    }

    /// <summary>
    /// 递归计算最小字符长度。
    /// </summary>
    private static Int32 ComputeMinCharLen(AstNode node)
    {
        switch (node)
        {
            case LiteralNode literalNode:
                return literalNode.Value.Length;

            case CharClassNode:
                return 1;

            case AnyNode:
                return 1;

            case BackrefNode:
                return 0;

            case AnchorNode:
                return 0;

            case ConcatNode concatNode:
            {
                Int32 total = 0;
                foreach (AstNode child in concatNode.Children)
                {
                    total += ComputeMinCharLen(child);
                }
                return total;
            }

            case AltNode altNode:
            {
                Int32 left = ComputeMinCharLen(altNode.Left);
                Int32 right = ComputeMinCharLen(altNode.Right);
                return Math.Min(left, right);
            }

            case StarNode:
                return 0;

            case PlusNode plusNode:
                return ComputeMinCharLen(plusNode.Inner);

            case OptionalNode:
                return 0;

            case IntersectNode intersectNode:
            {
                Int32 left = ComputeMinCharLen(intersectNode.Left);
                Int32 right = ComputeMinCharLen(intersectNode.Right);
                return Math.Max(left, right);
            }

            case ComplementNode:
                return 0;

            case DifferenceNode differenceNode:
            {
                Int32 left = ComputeMinCharLen(differenceNode.Left);
                Int32 right = ComputeMinCharLen(differenceNode.Right);
                return Math.Min(left, right);
            }

            case CaptureNode captureNode:
                return ComputeMinCharLen(captureNode.Inner);

            default:
                return 0;
        }
    }

    /// <summary>
    /// 收集编译警告。
    /// </summary>
    private static void CollectWarnings(AstNode node, Complexity complexity, List<CompileWarning> warnings)
    {
        if (complexity == Complexity.ContextFree)
        {
            warnings.Add(new CompileWarning(
                "CFG001",
                "模式包含反向引用，需要上下文无关匹配引擎，无法使用 DFA 优化",
                -1));
        }

        if (node is AnyNode)
        {
            warnings.Add(new CompileWarning(
                "ANY001",
                "使用通配符 '.' 可能导致回溯灾难，建议尽量使用具体字符类替代",
                node.Position));
        }

        CollectNestedStarWarnings(node, warnings);
    }

    /// <summary>
    /// 递归收集嵌套的量词警告。
    /// </summary>
    private static void CollectNestedStarWarnings(AstNode node, List<CompileWarning> warnings)
    {
        switch (node)
        {
            case StarNode starNode:
                if (starNode.Inner is StarNode or PlusNode or OptionalNode)
                {
                    warnings.Add(new CompileWarning(
                        "NEST001",
                        "嵌套量词可能导致指数级回溯",
                        starNode.Position));
                }
                break;

            case PlusNode plusNode:
                if (plusNode.Inner is StarNode or PlusNode or OptionalNode)
                {
                    warnings.Add(new CompileWarning(
                        "NEST001",
                        "嵌套量词可能导致指数级回溯",
                        plusNode.Position));
                }
                break;

            case OptionalNode optionalNode:
                if (optionalNode.Inner is StarNode or PlusNode or OptionalNode)
                {
                    warnings.Add(new CompileWarning(
                        "NEST001",
                        "嵌套量词可能导致指数级回溯",
                        optionalNode.Position));
                }
                break;
        }

        // 递归子节点
        switch (node)
        {
            case ConcatNode concatNode:
                foreach (AstNode child in concatNode.Children)
                {
                    CollectNestedStarWarnings(child, warnings);
                }
                break;

            case AltNode altNode:
                CollectNestedStarWarnings(altNode.Left, warnings);
                CollectNestedStarWarnings(altNode.Right, warnings);
                break;

            case StarNode starNode:
                CollectNestedStarWarnings(starNode.Inner, warnings);
                break;

            case PlusNode plusNode:
                CollectNestedStarWarnings(plusNode.Inner, warnings);
                break;

            case OptionalNode optionalNode:
                CollectNestedStarWarnings(optionalNode.Inner, warnings);
                break;

            case IntersectNode intersectNode:
                CollectNestedStarWarnings(intersectNode.Left, warnings);
                CollectNestedStarWarnings(intersectNode.Right, warnings);
                break;

            case ComplementNode complementNode:
                CollectNestedStarWarnings(complementNode.Inner, warnings);
                break;

            case DifferenceNode differenceNode:
                CollectNestedStarWarnings(differenceNode.Left, warnings);
                CollectNestedStarWarnings(differenceNode.Right, warnings);
                break;

            case CaptureNode captureNode:
                CollectNestedStarWarnings(captureNode.Inner, warnings);
                break;
        }
    }
}
