using System.Collections.Immutable;

namespace Nyar.VM.TextVM.Compiler;

/// <summary>
/// 编译期布尔代数常量折叠。
/// 将 <see cref="IntersectNode"/>、<see cref="ComplementNode"/> 和 <see cref="DifferenceNode"/>
/// 中两个操作数均为 <see cref="CharClassNode"/> 的布尔运算在编译期求值，
/// 重写为等价的 <see cref="CharClassNode"/>，从而消除运行时布尔逻辑指令。
/// </summary>
public static class BooleanFolder
{
    /// <summary>
    /// 对 AST 进行编译期布尔代数常量折叠。
    /// </summary>
    /// <param name="node">要折叠的 AST 根节点。</param>
    /// <param name="warnings">折叠过程中产生的编译警告列表。</param>
    /// <returns>折叠后的 AST 根节点。</returns>
    public static AstNode Fold(AstNode node, out IReadOnlyList<CompileWarning> warnings)
    {
        List<CompileWarning> warningList = [];
        AstNode result = FoldCore(node, warningList);
        warnings = warningList.AsReadOnly();
        return result;
    }

    /// <summary>
    /// 递归折叠 AST。
    /// </summary>
    private static AstNode FoldCore(AstNode node, List<CompileWarning> warnings)
    {
        switch (node)
        {
            case ConcatNode concat:
            {
                ImmutableArray<AstNode> children = concat.Children;
                ImmutableArray<AstNode>.Builder builder = ImmutableArray.CreateBuilder<AstNode>(children.Length);
                Boolean anyChanged = false;

                for (Int32 idx = 0; idx < children.Length; idx++)
                {
                    AstNode folded = FoldCore(children[idx], warnings);
                    builder.Add(folded);

                    if (folded != children[idx])
                    {
                        anyChanged = true;
                    }
                }

                if (anyChanged)
                {
                    return new ConcatNode(builder.MoveToImmutable())
                    {
                        Position = concat.Position,
                        Length = concat.Length,
                    };
                }

                return concat;
            }

            case AltNode alt:
            {
                AstNode left = FoldCore(alt.Left, warnings);
                AstNode right = FoldCore(alt.Right, warnings);

                if (left != alt.Left || right != alt.Right)
                {
                    return new AltNode(left, right)
                    {
                        Position = alt.Position,
                        Length = alt.Length,
                    };
                }

                return alt;
            }

            case StarNode star:
            {
                AstNode inner = FoldCore(star.Inner, warnings);

                if (inner != star.Inner)
                {
                    return new StarNode(inner)
                    {
                        Position = star.Position,
                        Length = star.Length,
                    };
                }

                return star;
            }

            case PlusNode plus:
            {
                AstNode inner = FoldCore(plus.Inner, warnings);

                if (inner != plus.Inner)
                {
                    return new PlusNode(inner)
                    {
                        Position = plus.Position,
                        Length = plus.Length,
                    };
                }

                return plus;
            }

            case OptionalNode optional:
            {
                AstNode inner = FoldCore(optional.Inner, warnings);

                if (inner != optional.Inner)
                {
                    return new OptionalNode(inner)
                    {
                        Position = optional.Position,
                        Length = optional.Length,
                    };
                }

                return optional;
            }

            case CaptureNode capture:
            {
                AstNode inner = FoldCore(capture.Inner, warnings);

                if (inner != capture.Inner)
                {
                    return new CaptureNode(inner, capture.GroupId)
                    {
                        Position = capture.Position,
                        Length = capture.Length,
                    };
                }

                return capture;
            }

            case IntersectNode intersect:
            {
                AstNode left = FoldCore(intersect.Left, warnings);
                AstNode right = FoldCore(intersect.Right, warnings);

                if (left is CharClassNode leftCc && right is CharClassNode rightCc)
                {
                    return FoldIntersection(leftCc, rightCc);
                }

                if (left != intersect.Left || right != intersect.Right)
                {
                    return new IntersectNode(left, right)
                    {
                        Position = intersect.Position,
                        Length = intersect.Length,
                    };
                }

                return intersect;
            }

            case ComplementNode complement:
            {
                DetectCaptureInComplement(complement, warnings);
                AstNode inner = FoldCore(complement.Inner, warnings);

                if (inner is CharClassNode innerCc)
                {
                    return FoldComplement(innerCc);
                }

                if (inner != complement.Inner)
                {
                    return new ComplementNode(inner)
                    {
                        Position = complement.Position,
                        Length = complement.Length,
                    };
                }

                return complement;
            }

            case DifferenceNode difference:
            {
                AstNode left = FoldCore(difference.Left, warnings);
                AstNode right = FoldCore(difference.Right, warnings);

                if (left is CharClassNode leftCc && right is CharClassNode rightCc)
                {
                    return FoldDifference(leftCc, rightCc);
                }

                if (left != difference.Left || right != difference.Right)
                {
                    return new DifferenceNode(left, right)
                    {
                        Position = difference.Position,
                        Length = difference.Length,
                    };
                }

                return difference;
            }

            default:
                return node;
        }
    }

    // ---- 布尔代数折叠核心逻辑 ----

    /// <summary>
    /// 对两个字符类的交集进行常量折叠。
    /// </summary>
    private static CharClassNode FoldIntersection(CharClassNode left, CharClassNode right)
    {
        if (!left.Negated && !right.Negated)
        {
            // A & B
            return new CharClassNode(IntersectRanges(left.Ranges, right.Ranges), false);
        }

        if (left.Negated && right.Negated)
        {
            // !A & !B = !(A | B)
            return new CharClassNode(UnionRanges(left.Ranges, right.Ranges), true);
        }

        if (left.Negated && !right.Negated)
        {
            // !A & B = B \ A
            return new CharClassNode(SubtractRanges(right.Ranges, left.Ranges), false);
        }

        // !left.Negated && right.Negated
        // A & !B = A \ B
        return new CharClassNode(SubtractRanges(left.Ranges, right.Ranges), false);
    }

    /// <summary>
    /// 对字符类的补集进行常量折叠。
    /// </summary>
    private static CharClassNode FoldComplement(CharClassNode inner)
    {
        return new CharClassNode(inner.Ranges, !inner.Negated);
    }

    /// <summary>
    /// 对两个字符类的差集进行常量折叠。
    /// </summary>
    private static CharClassNode FoldDifference(CharClassNode left, CharClassNode right)
    {
        if (!left.Negated && !right.Negated)
        {
            // A - B = A \ B
            return new CharClassNode(SubtractRanges(left.Ranges, right.Ranges), false);
        }

        if (left.Negated && !right.Negated)
        {
            // !A - B = !A & !B = !(A | B)
            return new CharClassNode(UnionRanges(left.Ranges, right.Ranges), true);
        }

        if (!left.Negated && right.Negated)
        {
            // A - !B = A & B
            return new CharClassNode(IntersectRanges(left.Ranges, right.Ranges), false);
        }

        // both negated
        // !A - !B = !A & B = B \ A
        return new CharClassNode(SubtractRanges(right.Ranges, left.Ranges), false);
    }

    // ---- 补集中包含捕获组的检测 ----

    /// <summary>
    /// 检查补集节点中是否包含捕获组，并发出相应警告。
    /// </summary>
    private static void DetectCaptureInComplement(ComplementNode complement, List<CompileWarning> warnings)
    {
        if (ContainsCapture(complement.Inner))
        {
            warnings.Add(new CompileWarning(
                "CAPTURE_IN_COMPLEMENT",
                "补集节点中包含捕获组，运行时会忽略捕获操作。",
                complement.Position));
        }
    }

    /// <summary>
    /// 检查节点树中是否包含 <see cref="CaptureNode"/>。
    /// </summary>
    private static Boolean ContainsCapture(AstNode node)
    {
        switch (node)
        {
            case CaptureNode:
                return true;

            case ConcatNode concat:
                foreach (AstNode child in concat.Children)
                {
                    if (ContainsCapture(child))
                    {
                        return true;
                    }
                }

                return false;

            case AltNode alt:
                return ContainsCapture(alt.Left) || ContainsCapture(alt.Right);

            case StarNode star:
                return ContainsCapture(star.Inner);

            case PlusNode plus:
                return ContainsCapture(plus.Inner);

            case OptionalNode optional:
                return ContainsCapture(optional.Inner);

            case ComplementNode complement:
                return ContainsCapture(complement.Inner);

            case IntersectNode intersect:
                return ContainsCapture(intersect.Left) || ContainsCapture(intersect.Right);

            case DifferenceNode difference:
                return ContainsCapture(difference.Left) || ContainsCapture(difference.Right);

            default:
                return false;
        }
    }

    // ---- 字符区间运算 ----

    /// <summary>
    /// 规范化字符区间集合：按起始位置升序排序，合并重叠和相邻的区间。
    /// </summary>
    private static ImmutableArray<CharRange> NormalizeRanges(ImmutableArray<CharRange> ranges)
    {
        if (ranges.Length <= 1)
        {
            return ranges;
        }

        CharRange[] sorted = [.. ranges.OrderBy(r => r.Lo)];
        List<CharRange> merged = [];
        CharRange current = sorted[0];

        for (Int32 idx = 1; idx < sorted.Length; idx++)
        {
            CharRange next = sorted[idx];

            if (next.Lo <= current.Hi + 1)
            {
                // 重叠或相邻，合并
                current = new CharRange(current.Lo, (Char)Math.Max(current.Hi, next.Hi));
            }
            else
            {
                merged.Add(current);
                current = next;
            }
        }

        merged.Add(current);
        return [.. merged];
    }

    /// <summary>
    /// 计算两组规范化区间的并集。
    /// </summary>
    private static ImmutableArray<CharRange> UnionRanges(ImmutableArray<CharRange> a, ImmutableArray<CharRange> b)
    {
        return NormalizeRanges(a.AddRange(b));
    }

    /// <summary>
    /// 计算两组规范化区间的交集。
    /// </summary>
    private static ImmutableArray<CharRange> IntersectRanges(ImmutableArray<CharRange> a, ImmutableArray<CharRange> b)
    {
        List<CharRange> result = [];
        Int32 i = 0;
        Int32 j = 0;

        while (i < a.Length && j < b.Length)
        {
            CharRange ra = a[i];
            CharRange rb = b[j];

            if (ra.Lo <= rb.Hi && rb.Lo <= ra.Hi)
            {
                result.Add(new CharRange(
                    (Char)Math.Max(ra.Lo, rb.Lo),
                    (Char)Math.Min(ra.Hi, rb.Hi)));
            }

            if (ra.Hi < rb.Hi)
            {
                i++;
            }
            else
            {
                j++;
            }
        }

        return [.. result];
    }

    /// <summary>
    /// 计算规范化区间集合在 BMP（0x0000–0xFFFF）范围内的补集。
    /// </summary>
    private static ImmutableArray<CharRange> ComplementRanges(
        ImmutableArray<CharRange> ranges,
        Char universeLo,
        Char universeHi)
    {
        if (ranges.Length == 0)
        {
            return [new CharRange(universeLo, universeHi)];
        }

        List<CharRange> result = [];
        Int32 currentLo = universeLo;
        Int32 universeHiInt = universeHi;

        foreach (CharRange range in ranges)
        {
            if (range.Lo > currentLo)
            {
                result.Add(new CharRange((Char)currentLo, (Char)(range.Lo - 1)));
            }

            currentLo = range.Hi + 1;

            if (currentLo > universeHiInt)
            {
                break;
            }
        }

        if (currentLo <= universeHiInt)
        {
            result.Add(new CharRange((Char)currentLo, universeHi));
        }

        return [.. result];
    }

    /// <summary>
    /// 计算两组规范化区间的差集（A \ B）。
    /// </summary>
    private static ImmutableArray<CharRange> SubtractRanges(
        ImmutableArray<CharRange> a,
        ImmutableArray<CharRange> b)
    {
        List<CharRange> result = [];
        Int32 i = 0;
        Int32 j = 0;

        while (i < a.Length)
        {
            CharRange ra = a[i];
            Int32 lo = ra.Lo;
            Int32 raHi = ra.Hi;

            // 跳过所有在 ra 之前的 b 区间
            while (j < b.Length && b[j].Hi < lo)
            {
                j++;
            }

            // 处理与 ra 重叠的 b 区间
            while (j < b.Length && b[j].Lo <= raHi)
            {
                CharRange rb = b[j];

                if (lo < rb.Lo)
                {
                    result.Add(new CharRange((Char)lo, (Char)(rb.Lo - 1)));
                }

                lo = rb.Hi + 1;

                if (rb.Hi >= raHi)
                {
                    lo = raHi + 1;
                    break;
                }

                j++;
            }

            if (lo <= raHi)
            {
                result.Add(new CharRange((Char)lo, (Char)raHi));
            }

            i++;
        }

        return [.. result];
    }
}
