using Nyar.Dialect.Core.Nodes;
using Nyar.EGraph;
using Nyar.IR.Intent;
using Range = Nyar.Dialect.Core.Nodes.Range;

namespace Nyar.Dialect.Core.Analysis;

/// <summary>
///     Oa 类型分析与效应推断
/// </summary>
public sealed class IKunTypeAnalysis : IAnalysis<AlgebraNode>
{
    /// <summary>
    ///     分析数据类型
    /// </summary>
    public Type data_type => typeof(IKunClassInfo);

    /// <summary>
    ///     从 EGraph 节点创建分析数据
    /// </summary>
    public object make(EGraph<AlgebraNode> egraph, AlgebraNode enode)
    {
        var type = InferType(egraph, enode);
        var effects = InferEffects(egraph, enode);
        return new IKunClassInfo(type, effects);
    }

    /// <summary>
    ///     合并两个分析数据
    /// </summary>
    public bool merge(ref object to, object from)
    {
        var toInfo = (IKunClassInfo)to;
        var fromInfo = (IKunClassInfo)from;

        var mergedType = toInfo.Type.kind == TypeKind.unknown
            ? fromInfo.Type
            : fromInfo.Type.kind == TypeKind.unknown
                ? toInfo.Type
                : toInfo.Type;

        var mergedEffects = toInfo.Effects.union(fromInfo.Effects);

        if (toInfo.Type == mergedType && toInfo.Effects == mergedEffects) return false;

        to = new IKunClassInfo(mergedType, mergedEffects);
        return true;
    }

    /// <summary>
    ///     创建默认分析数据
    /// </summary>
    public object make_default()
    {
        return IKunClassInfo.Unknown;
    }

    /// <summary>
    ///     检查两个分析数据是否兼容
    /// </summary>
    public bool is_compatible(object data1, object data2)
    {
        var info1 = (IKunClassInfo)data1;
        var info2 = (IKunClassInfo)data2;

        if (info1.Type.kind == TypeKind.unknown || info2.Type.kind == TypeKind.unknown) return true;

        return info1.Type.kind == info2.Type.kind;
    }

    private static TypeAnnotation InferType(EGraph<AlgebraNode> egraph, AlgebraNode enode)
    {
        switch (enode)
        {
            case Literal<object?>:
                return TypeAnnotation.unit;
            case Literal<long>:
                return TypeAnnotation.integer(64);
            case Literal<double>:
                return TypeAnnotation.@float(64);
            case Literal<bool>:
                return TypeAnnotation.boolean;
            case Add add:
                return InferTypeFromChild(egraph, add.left);
            case Sub sub:
                return InferTypeFromChild(egraph, sub.left);
            case Mul mul:
                return InferTypeFromChild(egraph, mul.left);
            case Div div:
                return InferTypeFromChild(egraph, div.left);
            case Rem rem:
                return InferTypeFromChild(egraph, rem.left);
            case Neg neg:
                return InferTypeFromChild(egraph, neg.operand);
            case Not:
                return TypeAnnotation.boolean;
            case Cmp:
                return TypeAnnotation.boolean;
            case Alloc:
            case Free:
            case Load:
            case Store:
            case Call:
            case Perform:
            case Handle:
                return InferTypeFromEffects(egraph, enode);

            // 声明与符号
            case VarDecl:
                return TypeAnnotation.unit;
            case Sym:
                return TypeAnnotation.unknown;
            case TypeRef:
            case TypeParam:
            case TypeParamConstraint:
            case UnionType:
            case IntersectType:
            case FuncType:
            case ListType:
            case ArrType:
            case TypeAlias:
            case Attrib:
            case Import:
            case Export:
            case Mod:
                return TypeAnnotation.unit;

            // 函数与闭包
            case Lambda:
                return new TypeAnnotation(TypeKind.function, "fn");
            case Apply apply:
                return InferTypeFromChild(egraph, apply.function);
            case Closure closure:
                return InferTypeFromChild(egraph, closure.function);

            // 控制流扩展
            case Choice choice:
                return InferTypeFromChild(egraph, choice.then);
            case Trap trap:
                return InferTypeFromChild(egraph, trap.value);
            case Compose compose:
                return InferTypeFromChild(egraph, compose.second);
            case StateUp:
            case Lifecycle:
            case Meta:
            case Resume:
                return TypeAnnotation.unit;

            // 数据结构
            case ArrayLit:
                return new TypeAnnotation(TypeKind.pointer, "array");
            case Range range:
                return InferTypeFromChild(egraph, range.start);
            case Pair:
                return new TypeAnnotation(TypeKind.tuple, "record");
            case Table:
                return new TypeAnnotation(TypeKind.tuple, "record");
            case GetOrdinalIdx getOrdinalIdx:
                return InferTypeFromChild(egraph, getOrdinalIdx.obj);
            case SetOrdinalIdx setOrdinalIdx:
                return InferTypeFromChild(egraph, setOrdinalIdx.obj);
            case GetOffsetIdx getOffsetIdx:
                return InferTypeFromChild(egraph, getOffsetIdx.obj);
            case SetOffsetIdx setOffsetIdx:
                return InferTypeFromChild(egraph, setOffsetIdx.obj);

            // 对象模型
            case Inherit:
            case ClassDef:
                return TypeAnnotation.unit;

            // 模式匹配
            case Match match:
                return InferTypeFromChild(egraph, match.value);
            case MatchArm matchArm:
                return InferTypeFromChild(egraph, matchArm.body);
            case Catch:
            case CatchArm:
                return TypeAnnotation.unknown;
            case LitPattern:
            case VarPattern:
            case CtorPattern:
            case ObjPatField:
            case ObjPattern:
            case WildPattern:
            case OrPattern:
            case GuardPattern:
            case TypeTestPattern:
                return new TypeAnnotation(TypeKind.unknown, "pattern");

            // 特性组
            case AttrGroup:
                return TypeAnnotation.unit;

            // 类型转换
            case Cast cast:
                return InferTypeFromChild(egraph, cast.target_type);

            // 循环降级
            case LoopFilter loopFilter:
                return InferTypeFromChild(egraph, loopFilter.data);

            default:
                return TypeAnnotation.unknown;
        }
    }

    /// <summary>
    ///     从 eclass 的子 Id 推断类型
    /// </summary>
    private static TypeAnnotation InferTypeFromChild(EGraph<AlgebraNode> egraph, Id childId)
    {
        var childClass = egraph.get_class(childId);
        if (childClass?.data is IKunClassInfo childInfo && childInfo.Type.kind != TypeKind.unknown)
            return childInfo.Type;

        return TypeAnnotation.unknown;
    }

    /// <summary>
    ///     从效应操作的子节点推断类型（对于无覆盖 child_ids 的效应节点，使用 child_ids 回退到子节点推断）
    /// </summary>
    private static TypeAnnotation InferTypeFromEffects(EGraph<AlgebraNode> egraph, AlgebraNode enode)
    {
        var children = enode.child_ids();
        if (children.Count > 0) return InferTypeFromChild(egraph, children[0]);

        return TypeAnnotation.unknown;
    }

    private static EffectSet InferEffects(EGraph<AlgebraNode> egraph, AlgebraNode enode)
    {
        return enode switch
        {
            Alloc => EffectSet.from(EffectKind.allocate),
            Free => EffectSet.from(EffectKind.free),
            Load => EffectSet.from(EffectKind.read),
            Store => EffectSet.from(EffectKind.write),
            Perform => EffectSet.from(EffectKind.perform),
            Handle => EffectSet.from(EffectKind.handle),
            Call => InferCallEffects(egraph, enode),
            _ => EffectSet.pure
        };
    }

    private static EffectSet InferCallEffects(EGraph<AlgebraNode> egraph, AlgebraNode enode)
    {
        // 提取 Call 节点的函数 Id（优先属性访问，回退 child_ids）
        Id? funcId = enode switch
        {
            Call call => call.function,
            _ => null
        };

        if (funcId is null)
        {
            var children = enode.child_ids();
            if (children.Count < 1) return EffectSet.from(EffectKind.io);

            funcId = children[0];
        }

        var funcClass = egraph.get_class(funcId.Value);
        if (funcClass?.data is IKunClassInfo funcInfo) return funcInfo.Effects;

        return EffectSet.from(EffectKind.io);
    }
}
