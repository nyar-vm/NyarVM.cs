using Nyar.Dialect.Core.Nodes;
using Nyar.Dialect.Standard;
using Nyar.Dialect.Standard.Nodes;
using Nyar.EGraph;
using Nyar.IR.Intent;
using Nyar.IR.Rewrite;

namespace Nyar.Tests.Dialects;

public class StandardDialectTests
{
    private static EGraph<Oa> create_e_graph()
    {
        return new EGraph<Oa>(null);
    }

    [Fact(Skip = "pe_factories 返回空数组，降级规则测试暂不适用")]
    public void BitOp_BitAnd_LowersToApply()
    {
        var egraph = create_e_graph();
        var a = egraph.add(new Literal<long>(0xFF));
        var b = egraph.add(new Literal<long>(0x0F));
        var bitAnd = egraph.add(new BitAnd(a, b));

        var dialect = new StandardDialect();
        var engine = new SaturationEngine<Oa>(dialect.pe_factories);
        var result = engine.run(egraph);

        Assert.True(result.total_unions > 0);

        var eclass = egraph.get_class(bitAnd);
        Assert.NotNull(eclass);
        Assert.True(eclass!.nodes.Any(n => n is Apply), "BitAnd 应降级为 Apply");
    }

    [Fact(Skip = "pe_factories 返回空数组，降级规则测试暂不适用")]
    public void BitOp_AllBitOps_LowerToApply()
    {
        var egraph = create_e_graph();
        var a = egraph.add(new Literal<long>(1));
        var b = egraph.add(new Literal<long>(2));

        var bitOps = new Oa[]
        {
            new BitAnd(a, b),
            new BitOr(a, b),
            new BitXor(a, b),
            new Shl(a, b),
            new LShr(a, b),
            new AShr(a, b)
        };

        foreach (var op in bitOps)
        {
            egraph.add(op);
        }

        var dialect = new StandardDialect();
        var engine = new SaturationEngine<Oa>(dialect.pe_factories);
        var result = engine.run(egraph);

        Assert.True(result.total_unions >= 6, "6 个位操作都应产生降级");
    }

    [Fact(Skip = "pe_factories 返回空数组，降级规则测试暂不适用")]
    public void Utf8Op_Utf8Concat_LowersToApply()
    {
        var egraph = create_e_graph();
        var a = egraph.add(new Literal<string>("hello"));
        var b = egraph.add(new Literal<string>(" world"));
        var concat = egraph.add(new Utf8Concat(a, b));

        var dialect = new StandardDialect();
        var engine = new SaturationEngine<Oa>(dialect.pe_factories);
        var result = engine.run(egraph);

        Assert.True(result.total_unions > 0);

        var eclass = egraph.get_class(concat);
        Assert.NotNull(eclass);
        Assert.True(eclass!.nodes.Any(n => n is Apply), "Utf8Concat 应降级为 Apply");
    }

    [Fact(Skip = "pe_factories 返回空数组，降级规则测试暂不适用")]
    public void Utf8Op_AllUtf8Ops_LowerToApply()
    {
        var egraph = create_e_graph();
        var s = egraph.add(new Literal<string>("test"));
        var start = egraph.add(new Literal<long>(0));
        var len = egraph.add(new Literal<long>(2));

        var utf8Ops = new Oa[]
        {
            new Utf8Concat(s, s),
            new Utf8Len(s),
            new Utf8Substr(s, start, len),
            new Utf8Compare(s, s)
        };

        foreach (var op in utf8Ops)
        {
            egraph.add(op);
        }

        var dialect = new StandardDialect();
        var engine = new SaturationEngine<Oa>(dialect.pe_factories);
        var result = engine.run(egraph);

        Assert.True(result.total_unions >= 4, "4 个 UTF-8 文本操作都应产生降级");
    }

    [Fact(Skip = "pe_factories 返回空数组，降级规则测试暂不适用")]
    public void ArrayOp_AllArrayOps_LowerToApply()
    {
        var egraph = create_e_graph();
        var arr = egraph.add(new Literal<long>(0));
        var len = egraph.add(new Literal<long>(10));
        var initVal = egraph.add(new Literal<long>(0));
        var index = egraph.add(new Literal<long>(3));
        var value = egraph.add(new Literal<long>(42));
        var ptr = egraph.add(new Literal<long>(256));

        var arrayOps = new Oa[]
        {
            new ArrayNew(len, initVal),
            new ArrayLength(arr),
            new ArrayGet(arr, index),
            new ArraySet(arr, index, value),
            new SliceNew(ptr, len)
        };

        foreach (var op in arrayOps)
        {
            egraph.add(op);
        }

        var dialect = new StandardDialect();
        var engine = new SaturationEngine<Oa>(dialect.pe_factories);
        var result = engine.run(egraph);

        Assert.True(result.total_unions >= 5, "5 个数组操作都应产生降级");
    }

    [Fact(Skip = "pe_factories 返回空数组，降级规则测试暂不适用")]
    public void StructOp_AllStructOps_LowerToApply()
    {
        var egraph = create_e_graph();
        var structId = egraph.add(new Literal<long>(1));
        var fieldValue = egraph.add(new Literal<long>(42));
        var fields = new List<Id> { fieldValue };

        var structOps = new Oa[]
        {
            new StructDeclare("Vec2", [("x", "f64"), ("y", "f64")]),
            new StructNew("Vec2", fields),
            new StructGet(structId, "x"),
            new StructSet(structId, "x", fieldValue)
        };

        foreach (var op in structOps)
        {
            egraph.add(op);
        }

        var dialect = new StandardDialect();
        var engine = new SaturationEngine<Oa>(dialect.pe_factories);
        var result = engine.run(egraph);

        Assert.True(result.total_unions >= 4, "4 个结构体操作都应产生降级");
    }

    [Fact(Skip = "pe_factories 返回空数组，降级规则测试暂不适用")]
    public void AtomicOp_AtomicLoad_LowersToCoreLoad()
    {
        var egraph = create_e_graph();
        var ptr = egraph.add(new Literal<long>(256));
        var atomicLoad = egraph.add(new AtomicLoad(ptr, "seq_cst"));

        var dialect = new StandardDialect();
        var engine = new SaturationEngine<Oa>(dialect.pe_factories);
        var result = engine.run(egraph);

        Assert.True(result.total_unions > 0);

        var eclass = egraph.get_class(atomicLoad);
        Assert.NotNull(eclass);
        Assert.True(eclass!.nodes.Any(n => n is Load), "AtomicLoad 应降级为 Core Load");
    }

    [Fact(Skip = "pe_factories 返回空数组，降级规则测试暂不适用")]
    public void AtomicOp_AtomicStore_LowersToCoreStore()
    {
        var egraph = create_e_graph();
        var ptr = egraph.add(new Literal<long>(256));
        var value = egraph.add(new Literal<long>(42));
        var atomicStore = egraph.add(new AtomicStore(ptr, value, "release"));

        var dialect = new StandardDialect();
        var engine = new SaturationEngine<Oa>(dialect.pe_factories);
        var result = engine.run(egraph);

        Assert.True(result.total_unions > 0);

        var eclass = egraph.get_class(atomicStore);
        Assert.NotNull(eclass);
        Assert.True(eclass!.nodes.Any(n => n is Store), "AtomicStore 应降级为 Core Store");
    }

    [Fact(Skip = "pe_factories 返回空数组，降级规则测试暂不适用")]
    public void AtomicOp_AtomicCas_LowersToApply()
    {
        var egraph = create_e_graph();
        var ptr = egraph.add(new Literal<long>(256));
        var expected = egraph.add(new Literal<long>(0));
        var desired = egraph.add(new Literal<long>(1));
        var atomicCas = egraph.add(new AtomicCas(ptr, expected, desired, "seq_cst"));

        var dialect = new StandardDialect();
        var engine = new SaturationEngine<Oa>(dialect.pe_factories);
        var result = engine.run(egraph);

        Assert.True(result.total_unions > 0);

        var eclass = egraph.get_class(atomicCas);
        Assert.NotNull(eclass);
        Assert.True(eclass!.nodes.Any(n => n is Apply), "AtomicCas 应降级为 Apply");
    }

    [Fact(Skip = "pe_factories 返回空数组，降级规则测试暂不适用")]
    public void CastOp_AllCastOps_LowerToApply()
    {
        var egraph = create_e_graph();
        var value = egraph.add(new Literal<long>(42));

        var castOps = new Oa[]
        {
            new Cast(value, "i64"),
            new Trunc(value, "i32"),
            new ZExt(value, "i64"),
            new SExt(value, "i64")
        };

        foreach (var op in castOps)
        {
            egraph.add(op);
        }

        var dialect = new StandardDialect();
        var engine = new SaturationEngine<Oa>(dialect.pe_factories);
        var result = engine.run(egraph);

        Assert.True(result.total_unions >= 4, "4 个类型转换操作都应产生降级");
    }

    [Fact]
    public void StandardDialect_HasEmptyPEFactories()
    {
        var dialect = new StandardDialect();
        Assert.Empty(dialect.pe_factories);
    }

    [Fact(Skip = "pe_factories 返回空数组，降级规则测试暂不适用")]
    public void StandardDialect_All22Nodes_CanLower()
    {
        var egraph = create_e_graph();
        var a = egraph.add(new Literal<long>(1));
        var b = egraph.add(new Literal<long>(2));
        var s = egraph.add(new Literal<string>("test"));
        var start = egraph.add(new Literal<long>(0));
        var len = egraph.add(new Literal<long>(4));
        var ptr = egraph.add(new Literal<long>(256));
        var structId = egraph.add(new Literal<long>(1));

        var allStandardNodes = new Oa[]
        {
            new Cast(a, "i64"),
            new Trunc(a, "i32"),
            new ZExt(a, "i64"),
            new SExt(a, "i64"),
            new BitAnd(a, b),
            new BitOr(a, b),
            new BitXor(a, b),
            new Shl(a, b),
            new LShr(a, b),
            new AShr(a, b),
            new Utf8Concat(s, s),
            new Utf8Len(s),
            new Utf8Substr(s, start, len),
            new Utf8Compare(s, s),
            new ArrayNew(len, a),
            new ArrayLength(a),
            new ArrayGet(a, b),
            new ArraySet(a, b, a),
            new SliceNew(ptr, len),
            new StructDeclare("S", []),
            new StructNew("S", [a]),
            new StructGet(structId, "x"),
            new StructSet(structId, "x", a),
            new AtomicLoad(ptr, "acquire"),
            new AtomicStore(ptr, a, "release"),
            new AtomicCas(ptr, a, b, "seq_cst")
        };

        foreach (var node in allStandardNodes)
        {
            egraph.add(node);
        }

        var dialect = new StandardDialect();
        var engine = new SaturationEngine<Oa>(dialect.pe_factories);
        var result = engine.run(egraph);

        Assert.True(result.total_unions >= 22, $"所有 Standard 节点都应产生降级，实际 {result.total_unions}");
    }
}
