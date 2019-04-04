using System.Collections.Immutable;

namespace Nyar.VM.TextVM.Compiler;

/// <summary>
/// Brzozowski 导数（Derivative）运算。
/// 对给定的 AST 节点 r 和字符 c，计算 D_c(r) —— r 在消费 c 后的剩余语言。
/// 导数天然支持布尔正则：D_c(A &amp; B) = D_c(A) &amp; D_c(B)，D_c(!A) = !D_c(A)。
/// </summary>
public static class Derivative
{
    /// <summary>
    /// 用于 AST 节点结构相等比较的比较器，忽略引用差异。
    /// </summary>
    public static IEqualityComparer<AstNode?> EqualityComparer { get; } = new AstNodeStructuralEqualityComparer();

    /// <summary>
    /// 计算节点 <paramref name="node"/> 在字符 <paramref name="c"/> 下的导数。
    /// 返回 null 表示无匹配（空集），返回 <see cref="LiteralNode"/> 表示匹配后的剩余语言。
    /// </summary>
    public static AstNode? Derive(AstNode node, Char c)
    {
        return node switch
        {
            LiteralNode literal => DeriveLiteral(literal, c),
            AnyNode => new LiteralNode(String.Empty),
            CharClassNode charClass => DeriveCharClass(charClass, c),
            ConcatNode concat => DeriveConcat(concat, c),
            AltNode alt => CombineAlt(Derive(alt.Left, c), Derive(alt.Right, c)),
            StarNode star => CreateConcat(Derive(star.Inner, c), new StarNode(star.Inner)),
            PlusNode plus => CreateConcat(Derive(plus.Inner, c), new StarNode(plus.Inner)),
            OptionalNode opt => CombineAlt(Derive(opt.Inner, c), new LiteralNode(String.Empty)),
            IntersectNode inter => CombineIntersect(Derive(inter.Left, c), Derive(inter.Right, c)),
            ComplementNode comp => DeriveComplement(comp, c),
            DifferenceNode diff => CombineIntersect(
                Derive(diff.Left, c),
                new ComplementNode(Derive(diff.Right, c) ?? new StarNode(new AnyNode()))),
            CaptureNode cap => DeriveCapture(cap, c),
            BackrefNode => node,
            AnchorNode => node,
            _ => throw new ArgumentException($"未知的 AST 节点类型：{node.GetType()}", nameof(node))
        };
    }

    /// <summary>
    /// 判断节点是否可匹配空串（nullable 谓词）。
    /// 若 <paramref name="node"/> 可匹配空串则返回 true，否则 false。
    /// </summary>
    public static Boolean IsNullable(AstNode? node)
    {
        return node switch
        {
            null => false,
            LiteralNode literal => literal.Value.Length == 0,
            AnyNode => false,
            CharClassNode => false,
            ConcatNode concat => IsNullableConcat(concat),
            AltNode alt => IsNullable(alt.Left) || IsNullable(alt.Right),
            StarNode => true,
            PlusNode plus => IsNullable(plus.Inner),
            OptionalNode => true,
            IntersectNode inter => IsNullable(inter.Left) && IsNullable(inter.Right),
            ComplementNode comp => !IsNullable(comp.Inner),
            DifferenceNode diff => IsNullable(diff.Left) && !IsNullable(diff.Right),
            CaptureNode cap => IsNullable(cap.Inner),
            BackrefNode => false,
            AnchorNode => true,
            _ => throw new ArgumentException($"未知的 AST 节点类型：{node.GetType()}", nameof(node))
        };
    }

    /// <summary>
    /// 规范化 AST 节点，移除冗余结构（如空字面量、空替代等）。
    /// 返回 null 表示空集或 epsilon（接受状态）。
    /// </summary>
    public static AstNode? Normalize(AstNode? node)
    {
        if (node is null)
        {
            return null;
        }

        return node switch
        {
            LiteralNode literal => NormalizeLiteral(literal),
            CharClassNode => node,
            AnyNode => node,
            ConcatNode concat => NormalizeConcat(concat),
            AltNode alt => NormalizeAlt(alt),
            StarNode star => NormalizeStar(star),
            PlusNode plus => NormalizePlus(plus),
            OptionalNode opt => NormalizeOptional(opt),
            IntersectNode inter => NormalizeIntersect(inter),
            ComplementNode comp => NormalizeComplement(comp),
            DifferenceNode diff => NormalizeDifference(diff),
            CaptureNode cap => NormalizeCapture(cap),
            BackrefNode => node,
            AnchorNode => node,
            _ => throw new ArgumentException($"未知的 AST 节点类型：{node.GetType()}", nameof(node))
        };
    }

    /// <summary>
    /// 计算 <see cref="LiteralNode"/> 的导数。
    /// </summary>
    private static AstNode? DeriveLiteral(LiteralNode node, Char c)
    {
        String value = node.Value;

        if (value.Length == 0)
        {
            return null;
        }

        return value[0] == c
            ? new LiteralNode(value[1..])
            : null;
    }

    /// <summary>
    /// 计算 <see cref="CharClassNode"/> 的导数。
    /// </summary>
    private static AstNode? DeriveCharClass(CharClassNode node, Char c)
    {
        Boolean matches = false;

        foreach (CharRange range in node.Ranges)
        {
            if (c >= range.Lo && c <= range.Hi)
            {
                matches = true;
                break;
            }
        }

        Boolean isMatch = matches ^ node.Negated;

        return isMatch ? new LiteralNode(String.Empty) : null;
    }

    /// <summary>
    /// 计算 <see cref="ConcatNode"/> 的导数。
    /// D_c(a·b·c...) = D_c(a)·(b·c...) | IsNullable(a) ? D_c(b·c...) : ∅
    /// </summary>
    private static AstNode? DeriveConcat(ConcatNode node, Char c)
    {
        ImmutableArray<AstNode> children = node.Children;

        if (children.Length == 0)
        {
            return null;
        }

        if (children.Length == 1)
        {
            return Derive(children[0], c);
        }

        AstNode first = children[0];
        ImmutableArray<AstNode> rest = children.RemoveAt(0);

        AstNode? derivedFirst = Derive(first, c);
        AstNode? leftPart = derivedFirst is null
            ? null
            : new ConcatNode([derivedFirst, .. rest]);

        AstNode? rightPart = null;

        if (IsNullable(first))
        {
            rightPart = DeriveConcat(new ConcatNode(rest), c);
        }

        return CombineAlt(leftPart, rightPart);
    }

    /// <summary>
    /// 计算 <see cref="ComplementNode"/> 的导数。
    /// D_c(!a) = !D_c(a)，当 D_c(a) 为 ∅ 时 !∅ = .*。
    /// </summary>
    private static AstNode DeriveComplement(ComplementNode node, Char c)
    {
        AstNode? innerDerived = Derive(node.Inner, c);

        if (innerDerived is null)
        {
            return new StarNode(new AnyNode());
        }

        return new ComplementNode(innerDerived);
    }

    /// <summary>
    /// 计算 <see cref="CaptureNode"/> 的导数。
    /// D_c(Capture(id, inner)) = Capture(id, D_c(inner))。
    /// </summary>
    private static AstNode? DeriveCapture(CaptureNode node, Char c)
    {
        AstNode? innerDerived = Derive(node.Inner, c);

        if (innerDerived is null)
        {
            return null;
        }

        return new CaptureNode(innerDerived, node.GroupId);
    }

    /// <summary>
    /// 判断 <see cref="ConcatNode"/> 是否可匹配空串。
    /// </summary>
    private static Boolean IsNullableConcat(ConcatNode node)
    {
        foreach (AstNode child in node.Children)
        {
            if (!IsNullable(child))
            {
                return false;
            }
        }

        return node.Children.Length > 0;
    }

    /// <summary>
    /// 规范化 <see cref="LiteralNode"/>。
    /// 空字面量视为 epsilon，返回 null。
    /// </summary>
    private static AstNode? NormalizeLiteral(LiteralNode node)
    {
        // 注意：LiteralNode("") 表示 ε（空字符串），是有效的接受状态，
        // 不应转换为 null。null 仅表示空集 ∅。
        return node;
    }

    /// <summary>
    /// 规范化 <see cref="ConcatNode"/>。
    /// </summary>
    private static AstNode? NormalizeConcat(ConcatNode node)
    {
        // 递归规范化所有子节点
        List<AstNode> normalized = new List<AstNode>(node.Children.Length);

        foreach (AstNode child in node.Children)
        {
            AstNode? normChild = Normalize(child);

            if (normChild is null)
            {
                // 空集 ∅: Concat 中出现 ∅ => 整个连接为空集
                return null;
            }

            // 跳过 ε（空字符串），因为 ε 是连接的幺元：ε·X = X, X·ε = X
            if (normChild is LiteralNode lit && lit.Value.Length == 0)
            {
                continue;
            }

            // 展平嵌套的连接节点
            if (normChild is ConcatNode innerConcat)
            {
                normalized.AddRange(innerConcat.Children);
            }
            else
            {
                normalized.Add(normChild);
            }
        }

        if (normalized.Count == 0)
        {
            // 所有子节点都是 ε，结果为 ε
            return new LiteralNode("");
        }

        if (normalized.Count == 1)
        {
            return normalized[0];
        }

        return new ConcatNode([.. normalized]);
    }

    /// <summary>
    /// 规范化 <see cref="AltNode"/>。
    /// </summary>
    private static AstNode? NormalizeAlt(AltNode node)
    {
        AstNode? leftNorm = Normalize(node.Left);
        AstNode? rightNorm = Normalize(node.Right);

        return CombineAlt(leftNorm, rightNorm);
    }

    /// <summary>
    /// 规范化 <see cref="StarNode"/>。
    /// </summary>
    private static AstNode? NormalizeStar(StarNode node)
    {
        AstNode? innerNorm = Normalize(node.Inner);

        if (innerNorm is null)
        {
            // 空集的 Kleene 星 = epsilon
            return null;
        }

        if (innerNorm is LiteralNode lit && lit.Value.Length == 0)
        {
            // ε 的 Kleene 星 = ε
            return new LiteralNode("");
        }

        if (innerNorm is StarNode)
        {
            // 星的星 = 星
            return node;
        }

        return new StarNode(innerNorm);
    }

    /// <summary>
    /// 规范化 <see cref="PlusNode"/>。
    /// </summary>
    private static AstNode? NormalizePlus(PlusNode node)
    {
        AstNode? innerNorm = Normalize(node.Inner);

        if (innerNorm is null)
        {
            return null;
        }

        if (innerNorm is LiteralNode lit && lit.Value.Length == 0)
        {
            // ε+ = ε
            return new LiteralNode("");
        }

        return new PlusNode(innerNorm);
    }

    /// <summary>
    /// 规范化 <see cref="OptionalNode"/>。
    /// </summary>
    private static AstNode? NormalizeOptional(OptionalNode node)
    {
        AstNode? innerNorm = Normalize(node.Inner);

        if (innerNorm is null)
        {
            return null;
        }

        if (innerNorm is LiteralNode lit && lit.Value.Length == 0)
        {
            // ε? = ε
            return new LiteralNode("");
        }

        return new OptionalNode(innerNorm);
    }

    /// <summary>
    /// 规范化 <see cref="IntersectNode"/>。
    /// </summary>
    private static AstNode? NormalizeIntersect(IntersectNode node)
    {
        AstNode? leftNorm = Normalize(node.Left);
        AstNode? rightNorm = Normalize(node.Right);

        return CombineIntersect(leftNorm, rightNorm);
    }

    /// <summary>
    /// 规范化 <see cref="ComplementNode"/>。
    /// </summary>
    private static AstNode? NormalizeComplement(ComplementNode node)
    {
        AstNode? innerNorm = Normalize(node.Inner);

        if (innerNorm is null)
        {
            // !∅ = U，用 .* 表示
            return new StarNode(new AnyNode());
        }

        if (innerNorm is ComplementNode innerComp)
        {
            // !!a = a
            return innerComp.Inner;
        }

        return new ComplementNode(innerNorm);
    }

    /// <summary>
    /// 规范化 <see cref="DifferenceNode"/>。
    /// </summary>
    private static AstNode? NormalizeDifference(DifferenceNode node)
    {
        AstNode? leftNorm = Normalize(node.Left);
        AstNode? rightNorm = Normalize(node.Right);

        if (leftNorm is null)
        {
            return null;
        }

        if (rightNorm is null)
        {
            return leftNorm;
        }

        return new DifferenceNode(leftNorm, rightNorm);
    }

    /// <summary>
    /// 规范化 <see cref="CaptureNode"/>。
    /// </summary>
    private static AstNode? NormalizeCapture(CaptureNode node)
    {
        AstNode? innerNorm = Normalize(node.Inner);

        if (innerNorm is null)
        {
            return null;
        }

        return new CaptureNode(innerNorm, node.GroupId);
    }

    /// <summary>
    /// 组合替代节点，自动处理 null（空集）情况。
    /// </summary>
    private static AstNode? CombineAlt(AstNode? left, AstNode? right)
    {
        if (left is null)
        {
            return right;
        }

        if (right is null)
        {
            return left;
        }

        return new AltNode(left, right);
    }

    /// <summary>
    /// 组合连接节点，自动处理 null（空集）情况。
    /// </summary>
    private static AstNode? CreateConcat(AstNode? left, AstNode right)
    {
        if (left is null)
        {
            return null;
        }

        return new ConcatNode([left, right]);
    }

    /// <summary>
    /// 组合交集节点，自动处理 null（空集）情况。
    /// </summary>
    private static AstNode? CombineIntersect(AstNode? left, AstNode? right)
    {
        if (left is null || right is null)
        {
            return null;
        }

        return new IntersectNode(left, right);
    }

    /// <summary>
    /// AST 节点结构相等比较器。
    /// 由于 <see cref="ImmutableArray{T}"/> 在记录类型中使用时默认比较引用，此比较器提供基于内容的结构相等比较。
    /// </summary>
    private sealed class AstNodeStructuralEqualityComparer : IEqualityComparer<AstNode?>
    {
        /// <summary>
        /// 比较两个 AST 节点是否结构相等。
        /// </summary>
        public Boolean Equals(AstNode? x, AstNode? y)
        {
            if (ReferenceEquals(x, y))
            {
                return true;
            }

            if (x is null || y is null)
            {
                return false;
            }

            if (x.GetType() != y.GetType())
            {
                return false;
            }

            return x switch
            {
                LiteralNode lx => y is LiteralNode ly && lx.Value == ly.Value,
                CharClassNode ccx => y is CharClassNode ccy
                    && ccx.Negated == ccy.Negated
                    && RangesEqual(ccx.Ranges, ccy.Ranges),
                ConcatNode cnx => y is ConcatNode cny
                    && ChildrenEqual(cnx.Children, cny.Children),
                AltNode ax => y is AltNode ay
                    && Equals(ax.Left, ay.Left) && Equals(ax.Right, ay.Right),
                StarNode sx => y is StarNode sy && Equals(sx.Inner, sy.Inner),
                PlusNode px => y is PlusNode py && Equals(px.Inner, py.Inner),
                OptionalNode ox => y is OptionalNode oy && Equals(ox.Inner, oy.Inner),
                IntersectNode ix => y is IntersectNode iy
                    && Equals(ix.Left, iy.Left) && Equals(ix.Right, iy.Right),
                ComplementNode cx => y is ComplementNode cy && Equals(cx.Inner, cy.Inner),
                DifferenceNode dx => y is DifferenceNode dy
                    && Equals(dx.Left, dy.Left) && Equals(dx.Right, dy.Right),
                CaptureNode capx => y is CaptureNode capy
                    && capx.GroupId == capy.GroupId && Equals(capx.Inner, capy.Inner),
                BackrefNode brx => y is BackrefNode bry && brx.GroupId == bry.GroupId,
                AnchorNode anx => y is AnchorNode any && anx.Kind == any.Kind,
                AnyNode => y is AnyNode,
                _ => x.Equals(y)
            };
        }

        /// <summary>
        /// 计算 AST 节点的哈希码。
        /// </summary>
        public Int32 GetHashCode(AstNode? node)
        {
            if (node is null)
            {
                return 0;
            }

            HashCode hash = new HashCode();

            hash.Add(node.GetType());

            switch (node)
            {
                case LiteralNode literal:
                    hash.Add(literal.Value);
                    break;

                case CharClassNode charClass:
                    hash.Add(charClass.Negated);

                    foreach (CharRange range in charClass.Ranges)
                    {
                        hash.Add(range.Lo);
                        hash.Add(range.Hi);
                    }
                    break;

                case ConcatNode concat:
                    foreach (AstNode child in concat.Children)
                    {
                        hash.Add(GetHashCode(child));
                    }
                    break;

                case AltNode alt:
                    hash.Add(GetHashCode(alt.Left));
                    hash.Add(GetHashCode(alt.Right));
                    break;

                case StarNode star:
                    hash.Add(GetHashCode(star.Inner));
                    break;

                case PlusNode plus:
                    hash.Add(GetHashCode(plus.Inner));
                    break;

                case OptionalNode opt:
                    hash.Add(GetHashCode(opt.Inner));
                    break;

                case IntersectNode inter:
                    hash.Add(GetHashCode(inter.Left));
                    hash.Add(GetHashCode(inter.Right));
                    break;

                case ComplementNode comp:
                    hash.Add(GetHashCode(comp.Inner));
                    break;

                case DifferenceNode diff:
                    hash.Add(GetHashCode(diff.Left));
                    hash.Add(GetHashCode(diff.Right));
                    break;

                case CaptureNode cap:
                    hash.Add(cap.GroupId);
                    hash.Add(GetHashCode(cap.Inner));
                    break;

                case BackrefNode br:
                    hash.Add(br.GroupId);
                    break;

                case AnchorNode an:
                    hash.Add(an.Kind);
                    break;

                case AnyNode:
                    break;
            }

            return hash.ToHashCode();
        }

        /// <summary>
        /// 比较两个 <see cref="CharRange"/> 数组是否相等。
        /// </summary>
        private static Boolean RangesEqual(ImmutableArray<CharRange> a, ImmutableArray<CharRange> b)
        {
            if (a.Length != b.Length)
            {
                return false;
            }

            for (Int32 i = 0; i < a.Length; i++)
            {
                if (a[i].Lo != b[i].Lo || a[i].Hi != b[i].Hi)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 比较两个 AST 节点数组是否结构相等。
        /// </summary>
        private Boolean ChildrenEqual(ImmutableArray<AstNode> a, ImmutableArray<AstNode> b)
        {
            if (a.Length != b.Length)
            {
                return false;
            }

            for (Int32 i = 0; i < a.Length; i++)
            {
                if (!Equals(a[i], b[i]))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
