using Nyar.Dialect.Core;
using Nyar.Dialect.Core.Nodes;
using Nyar.Dialect.Standard.Nodes;
using Nyar.EGraph;
using Nyar.IR.Intent;
using Nyar.ObjectAlgebra;
using static Nyar.IR.Intent.AlgebraNode;

namespace Nyar.Dialect.Standard.PartialEval;

/// <summary>
///     Standard 方言到 Core 方言的部分求值降级工厂。
///     将 Standard 方言专有节点降级为普通 `Apply(Symbol(...), args)` 调用，
///     避免在 IR 中继续保留通用运算节点概念。
/// </summary>
public sealed class StandardToCoreFactory : IPEFactory
{
    /// <inheritdoc />
    public string name => "std-to-core";

    /// <inheritdoc />
    public string source_dialect => "std";

    /// <inheritdoc />
    public string target_dialect => "core";

    /// <inheritdoc />
    public bool apply(EGraph<AlgebraNode> egraph)
    {
        // 预先创建 None 节点用于零参数操作
        var noneId = egraph.add(new None());

        // 符号缓存，避免重复创建 Symbol 节点
        var symbolCache = new Dictionary<string, Id>();

        // 收集所有需要 union 的 (原始节点Id, 降级节点)
        var unions = new List<(Id originalId, AlgebraNode lowered)>();

        // 遍历所有等价类，收集需要降级的 Standard 节点
        foreach (var (classId, eclass) in egraph.classes)
        {
            foreach (var node in eclass.nodes)
            {
                var lowered = LowerNode(node, egraph, noneId, symbolCache);
                if (lowered is not null)
                {
                    unions.Add((new Id(classId), lowered));
                }
            }
        }

        if (unions.Count == 0)
        {
            return false;
        }

        // 添加所有降级节点并建立等价关系
        var changed = false;
        foreach (var (originalId, lowered) in unions)
        {
            var loweredId = egraph.add(lowered);
            var loweredRoot = egraph.union_find.find(loweredId);
            var originalRoot = egraph.union_find.find(originalId);
            if (loweredRoot != originalRoot)
            {
                egraph.union(originalRoot, loweredRoot);
                changed = true;
            }
        }

        if (changed)
        {
            egraph.rebuild();
        }

        return changed;
    }

    /// <summary>
    ///     将单个 Standard 方言节点降级为 Core 方言节点
    /// </summary>
    /// <param name="node">Standard 方言节点</param>
    /// <param name="egraph">目标 EGraph</param>
    /// <param name="noneId">None 节点的 Id，用于零参数操作</param>
    /// <param name="symbolCache">符号节点缓存</param>
    /// <returns>降级后的 Core 节点，若无法降级则返回 null</returns>
    private static AlgebraNode? LowerNode(AlgebraNode node, EGraph<AlgebraNode> egraph, Id noneId, Dictionary<string, Id> symbolCache)
    {
        return node switch
        {
            #region UTF-8 文本操作

            Utf8Concat uc => create_apply("utf8_concat", egraph, symbolCache, uc.left, uc.right),
            Utf8Format uf => LowerUtf8Format(uf, egraph, symbolCache),
            Utf8Substr us => create_apply("utf8_substr", egraph, symbolCache, us.value, us.start, us.length),
            Utf8Len ul => create_apply("utf8_len", egraph, symbolCache, ul.value),
            Utf8Compare uc => create_apply("utf8_compare", egraph, symbolCache, uc.left, uc.right),

            #endregion

            #region 数组与切片操作

            ArrayNew an => create_apply("arrnew", egraph, symbolCache, an.length, an.initialValue),
            ArrayLength al => create_apply("arrlen", egraph, symbolCache, al.array),
            ArrayGet ag => create_apply("arrget", egraph, symbolCache, ag.array, ag.index),
            ArraySet ars => create_apply("arrset", egraph, symbolCache, ars.array, ars.index, ars.value),
            GetOrdinalIdx getOrdinalIdx => new Nyar.Dialect.Core.GetOrdinalIdx(getOrdinalIdx.obj, getOrdinalIdx.index),
            SetOrdinalIdx setOrdinalIdx => new Nyar.Dialect.Core.SetOrdinalIdx(setOrdinalIdx.obj, setOrdinalIdx.index, setOrdinalIdx.value),
            GetOffsetIdx getOffsetIdx => new Nyar.Dialect.Core.GetOffsetIdx(getOffsetIdx.obj, getOffsetIdx.index),
            SetOffsetIdx setOffsetIdx => new Nyar.Dialect.Core.SetOffsetIdx(setOffsetIdx.obj, setOffsetIdx.index, setOffsetIdx.value),
            SliceNew sn => create_apply("slicenew", egraph, symbolCache, sn.pointer, sn.length),

            #endregion

            #region 结构体操作

            StructDeclare sd => create_apply("stdecl", egraph, symbolCache,
                [GetSymbolId(sd.name, egraph, symbolCache), ..GetSymbolIds(sd.fields, egraph, symbolCache)]),
            StructNew sn_ => create_apply("stnew", egraph, symbolCache,
                [GetSymbolId(sn_.name, egraph, symbolCache), ..sn_.fields]),
            StructGet sg => create_apply("stget", egraph, symbolCache, sg.target, GetSymbolId(sg.fieldName, egraph, symbolCache)),
            StructSet ss_ => create_apply("stset", egraph, symbolCache, ss_.target, GetSymbolId(ss_.fieldName, egraph, symbolCache), ss_.value),

            #endregion

            #region 类型转换与位操作

            Cast c => create_apply("cast", egraph, symbolCache, c.value, GetSymbolId(c.targetType, egraph, symbolCache)),
            Trunc t => create_apply("trunc", egraph, symbolCache, t.value, GetSymbolId(t.targetType, egraph, symbolCache)),
            ZExt z => create_apply("zext", egraph, symbolCache, z.value, GetSymbolId(z.targetType, egraph, symbolCache)),
            SExt s => create_apply("sext", egraph, symbolCache, s.value, GetSymbolId(s.targetType, egraph, symbolCache)),
            BitAnd ba => create_apply("bitand", egraph, symbolCache, ba.left, ba.right),
            BitOr bo => create_apply("bitor", egraph, symbolCache, bo.left, bo.right),
            BitXor bx => create_apply("bitxor", egraph, symbolCache, bx.left, bx.right),
            Shl sh => create_apply("shl", egraph, symbolCache, sh.value, sh.shift),
            LShr ls => create_apply("lshr", egraph, symbolCache, ls.value, ls.shift),
            AShr ars => create_apply("ashr", egraph, symbolCache, ars.value, ars.shift),

            #endregion

            #region 原子操作

            AtomicLoad al => create_apply("atomicload", egraph, symbolCache, al.pointer, GetSymbolId(al.order, egraph, symbolCache)),
            AtomicStore ast => create_apply("atomicstore", egraph, symbolCache, ast.pointer, ast.value, GetSymbolId(ast.order, egraph, symbolCache)),
            AtomicCas ac => create_apply("atomiccas", egraph, symbolCache, ac.pointer, ac.expected, ac.desired,
                GetSymbolId(ac.order, egraph, symbolCache)),

            #endregion

            #region 并发操作

            Fork f => create_apply("fork", egraph, symbolCache, f.body),
            Yield => create_apply("yield", egraph, symbolCache, noneId),
            Await a => create_apply("await", egraph, symbolCache, a.task),

            #endregion

            #region 时序操作

            Sleep s => create_apply("sleep", egraph, symbolCache, s.ms),
            TimeNow => create_apply("timenow", egraph, symbolCache, noneId),

            #endregion

            _ => null
        };
    }

    /// <summary>
    ///     降级 `Utf8Format` 节点：将模板与参数打平成普通调用。
    /// </summary>
    private static Apply LowerUtf8Format(Utf8Format uf, EGraph<AlgebraNode> egraph, Dictionary<string, Id> symbolCache)
    {
        var templateId = GetSymbolId(uf.template, egraph, symbolCache);
        return create_apply("utf8_format", egraph, symbolCache, [templateId, ..uf.args]);
    }

    /// <summary>
    ///     获取或创建 Symbol 节点的 Id
    /// </summary>
    private static Id GetSymbolId(string name, EGraph<AlgebraNode> egraph, Dictionary<string, Id> cache)
    {
        if (cache.TryGetValue(name, out var cachedId))
        {
            return cachedId;
        }

        var id = egraph.add(new Symbol(name));
        cache[name] = id;
        return id;
    }

    /// <summary>
    ///     将多个字符串转为 `Symbol` 节点标识符数组。
    /// </summary>
    private static Id[] GetSymbolIds(string[] symbols, EGraph<AlgebraNode> egraph, Dictionary<string, Id> cache)
    {
        var ids = new Id[symbols.Length];
        for (var i = 0; i < symbols.Length; i++)
        {
            ids[i] = GetSymbolId(symbols[i], egraph, cache);
        }

        return ids;
    }

    /// <summary>
    ///     构造普通调用节点，统一承载原先依赖通用运算节点的运行时能力。
    /// </summary>
    private static Apply create_apply(string functionName, EGraph<AlgebraNode> egraph,
        Dictionary<string, Id> symbolCache, params Id[] arguments)
    {
        return new Apply(GetSymbolId(functionName, egraph, symbolCache), arguments);
    }
}
