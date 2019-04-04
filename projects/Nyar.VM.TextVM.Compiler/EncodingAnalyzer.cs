using System.Collections.Immutable;

namespace Nyar.VM.TextVM.Compiler;

/// <summary>
/// 分析模式是否可以在不进行完整 Unicode 解码的情况下处理。
/// </summary>
public static class EncodingAnalyzer
{
    /// <summary>
    /// 判断 AST 中所有字面量字符是否均为 ASCII 字符（&lt; 128），
    /// 且所有字符类范围均在 ASCII 范围内。
    /// </summary>
    /// <param name="node">要分析的 AST 根节点。</param>
    /// <returns>如果整个模式只包含 ASCII 字符，则为 true。</returns>
    public static Boolean IsAsciiOnly(AstNode node)
    {
        return CheckAscii(node) == AsciiStatus.OnlyAscii;
    }

    /// <summary>
    /// 判断 AST 中所有字符范围是否均在 BMP（U+0000-U+FFFF）内。
    /// </summary>
    /// <param name="node">要分析的 AST 根节点。</param>
    /// <returns>如果整个模式只包含 BMP 字符，则为 true。</returns>
    public static Boolean IsBmpOnly(AstNode node)
    {
        return CheckBmp(node) == BmpStatus.OnlyBmp;
    }

    /// <summary>
    /// ASCII 状态。
    /// </summary>
    private enum AsciiStatus
    {
        /// <summary>
        /// 仅包含 ASCII 字符。
        /// </summary>
        OnlyAscii,

        /// <summary>
        /// 包含非 ASCII 字符。
        /// </summary>
        ContainsNonAscii,
    }

    /// <summary>
    /// BMP 状态。
    /// </summary>
    private enum BmpStatus
    {
        /// <summary>
        /// 仅包含 BMP 字符。
        /// </summary>
        OnlyBmp,

        /// <summary>
        /// 包含增补平面字符。
        /// </summary>
        ContainsSupplementary,
    }

    /// <summary>
    /// 递归检查 ASCII 状态。
    /// </summary>
    private static AsciiStatus CheckAscii(AstNode node)
    {
        switch (node)
        {
            case LiteralNode literalNode:
                foreach (Char c in literalNode.Value)
                {
                    if (c >= 128)
                    {
                        return AsciiStatus.ContainsNonAscii;
                    }
                }
                return AsciiStatus.OnlyAscii;

            case CharClassNode charClassNode:
                foreach (CharRange range in charClassNode.Ranges)
                {
                    if (range.Hi >= 128)
                    {
                        return AsciiStatus.ContainsNonAscii;
                    }
                }
                return AsciiStatus.OnlyAscii;

            case AnyNode:
                return AsciiStatus.ContainsNonAscii;

            case ConcatNode concatNode:
                return CheckAsciiConcat(concatNode.Children);

            case AltNode altNode:
                return CombineAscii(CheckAscii(altNode.Left), CheckAscii(altNode.Right));

            case StarNode starNode:
                return CheckAscii(starNode.Inner);

            case PlusNode plusNode:
                return CheckAscii(plusNode.Inner);

            case OptionalNode optionalNode:
                return CheckAscii(optionalNode.Inner);

            case IntersectNode intersectNode:
                return CombineAscii(CheckAscii(intersectNode.Left), CheckAscii(intersectNode.Right));

            case ComplementNode complementNode:
                return CheckAscii(complementNode.Inner);

            case DifferenceNode differenceNode:
                return CombineAscii(CheckAscii(differenceNode.Left), CheckAscii(differenceNode.Right));

            case CaptureNode captureNode:
                return CheckAscii(captureNode.Inner);

            case BackrefNode:
            case AnchorNode:
                return AsciiStatus.OnlyAscii;

            default:
                return AsciiStatus.OnlyAscii;
        }
    }

    /// <summary>
    /// 合并两个 ASCII 状态。
    /// </summary>
    private static AsciiStatus CombineAscii(AsciiStatus left, AsciiStatus right)
    {
        if (left == AsciiStatus.ContainsNonAscii || right == AsciiStatus.ContainsNonAscii)
        {
            return AsciiStatus.ContainsNonAscii;
        }

        return AsciiStatus.OnlyAscii;
    }

    /// <summary>
    /// 检查子节点数组的 ASCII 状态。
    /// </summary>
    private static AsciiStatus CheckAsciiConcat(ImmutableArray<AstNode> children)
    {
        foreach (AstNode child in children)
        {
            if (CheckAscii(child) == AsciiStatus.ContainsNonAscii)
            {
                return AsciiStatus.ContainsNonAscii;
            }
        }

        return AsciiStatus.OnlyAscii;
    }

    /// <summary>
    /// 递归检查 BMP 状态。
    /// </summary>
    private static BmpStatus CheckBmp(AstNode node)
    {
        switch (node)
        {
            case LiteralNode literalNode:
                foreach (Char c in literalNode.Value)
                {
                    if (c > 0xFFFF)
                    {
                        return BmpStatus.ContainsSupplementary;
                    }
                }
                return BmpStatus.OnlyBmp;

            case CharClassNode charClassNode:
                foreach (CharRange range in charClassNode.Ranges)
                {
                    if (range.Hi > 0xFFFF)
                    {
                        return BmpStatus.ContainsSupplementary;
                    }
                }
                return BmpStatus.OnlyBmp;

            case AnyNode:
                return BmpStatus.ContainsSupplementary;

            case ConcatNode concatNode:
                return CheckBmpConcat(concatNode.Children);

            case AltNode altNode:
                return CombineBmp(CheckBmp(altNode.Left), CheckBmp(altNode.Right));

            case StarNode starNode:
                return CheckBmp(starNode.Inner);

            case PlusNode plusNode:
                return CheckBmp(plusNode.Inner);

            case OptionalNode optionalNode:
                return CheckBmp(optionalNode.Inner);

            case IntersectNode intersectNode:
                return CombineBmp(CheckBmp(intersectNode.Left), CheckBmp(intersectNode.Right));

            case ComplementNode complementNode:
                return CheckBmp(complementNode.Inner);

            case DifferenceNode differenceNode:
                return CombineBmp(CheckBmp(differenceNode.Left), CheckBmp(differenceNode.Right));

            case CaptureNode captureNode:
                return CheckBmp(captureNode.Inner);

            case BackrefNode:
            case AnchorNode:
                return BmpStatus.OnlyBmp;

            default:
                return BmpStatus.OnlyBmp;
        }
    }

    /// <summary>
    /// 合并两个 BMP 状态。
    /// </summary>
    private static BmpStatus CombineBmp(BmpStatus left, BmpStatus right)
    {
        if (left == BmpStatus.ContainsSupplementary || right == BmpStatus.ContainsSupplementary)
        {
            return BmpStatus.ContainsSupplementary;
        }

        return BmpStatus.OnlyBmp;
    }

    /// <summary>
    /// 检查子节点数组的 BMP 状态。
    /// </summary>
    private static BmpStatus CheckBmpConcat(ImmutableArray<AstNode> children)
    {
        foreach (AstNode child in children)
        {
            if (CheckBmp(child) == BmpStatus.ContainsSupplementary)
            {
                return BmpStatus.ContainsSupplementary;
            }
        }

        return BmpStatus.OnlyBmp;
    }
}
