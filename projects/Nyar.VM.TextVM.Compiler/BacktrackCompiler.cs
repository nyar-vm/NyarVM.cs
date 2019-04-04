using System.Collections.Immutable;

using Nyar.VM.TextVM;

namespace Nyar.VM.TextVM.Compiler;

/// <summary>
/// 回溯虚拟机指令编译器。将 <see cref="AstNode"/> 编译为 <see cref="Inst"/> 指令序列，
/// 供 BacktrackExecutor 执行。支持捕获组和反向引用。
/// </summary>
public static class BacktrackCompiler
{
    /// <summary>
    /// 编译 AST 节点为回溯虚拟机指令序列。
    /// </summary>
    /// <param name="node">要编译的 AST 根节点。</param>
    /// <returns>包含指令数组和捕获组数量的元组。</returns>
    public static (Inst[] Program, int CaptureCount) Compile(AstNode node)
    {
        List<Inst> instructions = [];
        int captureCount = 0;
        CompileNode(node, instructions, ref captureCount);
        return ([.. instructions], captureCount);
    }

    /// <summary>
    /// 递归编译 AST 节点。
    /// </summary>
    private static void CompileNode(AstNode node, List<Inst> insts, ref int captureCount)
    {
        switch (node)
        {
            case LiteralNode lit:
                foreach (char c in lit.Value)
                {
                    insts.Add(Inst.CreateChar(c));
                }
                break;

            case AnyNode:
                insts.Add(Inst.Any);
                break;

            case CharClassNode cc:
            {
                // 转换区间类型：从编译器的 CharRange 转换为运行时的 CharRange
                ImmutableArray<Nyar.VM.TextVM.CharRange> ranges =
                [
                    .. cc.Ranges
                        .Select(r => new Nyar.VM.TextVM.CharRange(r.Lo, r.Hi))
                ];
                insts.Add(Inst.CreateCharClass(ranges, cc.Negated));
                break;
            }

            case ConcatNode concat:
                foreach (var child in concat.Children)
                {
                    CompileNode(child, insts, ref captureCount);
                }
                break;

            case AltNode alt:
            {
                // Split(L1, L2): L1 = left, L2 = right
                int splitPos = insts.Count;
                insts.Add(default); // placeholder
                int leftStart = insts.Count;
                CompileNode(alt.Left, insts, ref captureCount);
                int jumpPos = insts.Count;
                insts.Add(default); // placeholder for Jump
                int rightStart = insts.Count;
                CompileNode(alt.Right, insts, ref captureCount);
                int endPos = insts.Count;

                insts[splitPos] = Inst.CreateSplit(leftStart, rightStart);
                insts[jumpPos] = Inst.CreateJump(endPos);
                break;
            }

            case StarNode star:
            {
                int loopStart = insts.Count;
                insts.Add(default); // Split placeholder
                int bodyStart = insts.Count;
                CompileNode(star.Inner, insts, ref captureCount);
                insts.Add(Inst.CreateJump(loopStart));
                int afterLoop = insts.Count;
                insts[loopStart] = Inst.CreateSplit(bodyStart, afterLoop);
                break;
            }

            case PlusNode plus:
            {
                CompileNode(plus.Inner, insts, ref captureCount);
                int plusLoopStart = insts.Count;
                insts.Add(default); // Split placeholder
                int plusBodyStart = insts.Count;
                CompileNode(plus.Inner, insts, ref captureCount);
                insts.Add(Inst.CreateJump(plusLoopStart));
                int plusAfterLoop = insts.Count;
                insts[plusLoopStart] = Inst.CreateSplit(plusBodyStart, plusAfterLoop);
                break;
            }

            case OptionalNode opt:
            {
                int optSplit = insts.Count;
                insts.Add(default);
                int optBody = insts.Count;
                CompileNode(opt.Inner, insts, ref captureCount);
                int optEnd = insts.Count;
                insts[optSplit] = Inst.CreateSplit(optBody, optEnd);
                break;
            }

            case CaptureNode cap:
            {
                int groupId = cap.GroupId;
                captureCount = Math.Max(captureCount, groupId + 1);
                insts.Add(Inst.CreateSave(groupId * 2));
                CompileNode(cap.Inner, insts, ref captureCount);
                insts.Add(Inst.CreateSave(groupId * 2 + 1));
                break;
            }

            case BackrefNode br:
                insts.Add(Inst.CreateBackref(br.GroupId));
                break;

            case AnchorNode an:
            {
                int anchorKind = an.Kind switch
                {
                    AnchorKind.Start => 0,
                    AnchorKind.End => 1,
                    AnchorKind.WordBoundary => 2,
                    _ => 0,
                };
                insts.Add(Inst.CreateAnchor(anchorKind));
                break;
            }

            case IntersectNode inter:
                // 编译左子节点；语义上由布尔折叠在编译期处理
                CompileNode(inter.Left, insts, ref captureCount);
                break;

            case ComplementNode comp:
                // 编译内部子节点；补集语义由布尔折叠在编译期处理
                CompileNode(comp.Inner, insts, ref captureCount);
                break;

            case DifferenceNode diff:
                CompileNode(diff.Left, insts, ref captureCount);
                break;
        }
    }
}
