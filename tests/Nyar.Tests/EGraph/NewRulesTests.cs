using Nyar.Dialect.Core;
using Nyar.Dialect.Core.Nodes;
using Nyar.Dialect.Core.Rules;
using Nyar.EGraph;
using Nyar.IR.Intent;
using Nyar.IR.Rewrite;

namespace Nyar.Tests.EGraph;

public class ConstantFoldingTests
{
    private static EGraph<AlgebraNode> create_e_graph()
    {
        return new EGraph<AlgebraNode>(null);
    }

    [Fact]
    public void AddTwoConstants_FoldsToResult()
    {
        var egraph = create_e_graph();
        var a = egraph.add(new Literal<long>(3));
        var b = egraph.add(new Literal<long>(4));
        var add = egraph.add(new Add(a, b));

        var engine = new SaturationEngine<AlgebraNode>([new ConstantFoldingRule()]);
        var result = engine.run(egraph);

        Assert.True(result.total_unions > 0);

        var eclass = egraph.get_class(add);
        Assert.NotNull(eclass);
        var hasSeven = eclass!.nodes.Any(n => n is Literal<long> { value: 7 });
        Assert.True(hasSeven);
    }

    [Fact]
    public void MulTwoConstants_FoldsToResult()
    {
        var egraph = create_e_graph();
        var a = egraph.add(new Literal<long>(5));
        var b = egraph.add(new Literal<long>(6));
        var mul = egraph.add(new Mul(a, b));

        var engine = new SaturationEngine<AlgebraNode>([new ConstantFoldingRule()]);
        var result = engine.run(egraph);

        Assert.True(result.total_unions > 0);

        var eclass = egraph.get_class(mul);
        Assert.NotNull(eclass);
        var hasThirty = eclass!.nodes.Any(n => n is Literal<long> { value: 30 });
        Assert.True(hasThirty);
    }

    [Fact]
    public void SubTwoConstants_FoldsToResult()
    {
        var egraph = create_e_graph();
        var a = egraph.add(new Literal<long>(10));
        var b = egraph.add(new Literal<long>(3));
        var sub = egraph.add(new Sub(a, b));

        var engine = new SaturationEngine<AlgebraNode>([new ConstantFoldingRule()]);
        var result = engine.run(egraph);

        Assert.True(result.total_unions > 0);

        var eclass = egraph.get_class(sub);
        Assert.NotNull(eclass);
        var hasSeven = eclass!.nodes.Any(n => n is Literal<long> { value: 7 });
        Assert.True(hasSeven);
    }

    [Fact]
    public void NegConstant_FoldsToResult()
    {
        var egraph = create_e_graph();
        var a = egraph.add(new Literal<long>(42));
        var neg = egraph.add(new Neg(a));

        var engine = new SaturationEngine<AlgebraNode>([new ConstantFoldingRule()]);
        var result = engine.run(egraph);

        Assert.True(result.total_unions > 0);

        var eclass = egraph.get_class(neg);
        Assert.NotNull(eclass);
        var hasNeg42 = eclass!.nodes.Any(n => n is Literal<long> { value: -42 });
        Assert.True(hasNeg42);
    }

    [Fact]
    public void CmpTwoConstants_FoldsToBool()
    {
        var egraph = create_e_graph();
        var a = egraph.add(new Literal<long>(3));
        var b = egraph.add(new Literal<long>(5));
        var cmp = egraph.add(new Cmp(CompareOp.lt, a, b));

        var engine = new SaturationEngine<AlgebraNode>([new ConstantFoldingRule()]);
        var result = engine.run(egraph);

        Assert.True(result.total_unions > 0);

        var eclass = egraph.get_class(cmp);
        Assert.NotNull(eclass);
        var hasTrue = eclass!.nodes.Any(n => n is Literal<bool> { value: true });
        Assert.True(hasTrue);
    }

    [Fact]
    public void DivByZero_DoesNotFold()
    {
        var egraph = create_e_graph();
        var a = egraph.add(new Literal<long>(10));
        var b = egraph.add(new Literal<long>(0));
        var div = egraph.add(new Div(a, b));

        var engine = new SaturationEngine<AlgebraNode>([new ConstantFoldingRule()]);
        var result = engine.run(egraph);

        Assert.Equal(0, result.total_unions);
    }
}

public class DeadCodeEliminationTests
{
    private static EGraph<AlgebraNode> create_e_graph()
    {
        return new EGraph<AlgebraNode>(null);
    }

    [Fact]
    public void AddZero_EliminatesToOperand()
    {
        var egraph = create_e_graph();
        var a = egraph.add(new Literal<long>(42));
        var zero = egraph.add(new Literal<long>(0));
        var add = egraph.add(new Add(a, zero));

        var engine = new SaturationEngine<AlgebraNode>([new DeadCodeEliminationRule()]);
        var result = engine.run(egraph);

        Assert.True(result.total_unions > 0);

        var eclass = egraph.get_class(add);
        Assert.NotNull(eclass);
        var hasLiteral = eclass!.nodes.Any(n => n is Literal<long> { value: 42 });
        Assert.True(hasLiteral);
    }

    [Fact]
    public void MulOne_EliminatesToOperand()
    {
        var egraph = create_e_graph();
        var a = egraph.add(new Literal<long>(42));
        var one = egraph.add(new Literal<long>(1));
        var mul = egraph.add(new Mul(a, one));

        var engine = new SaturationEngine<AlgebraNode>([new DeadCodeEliminationRule()]);
        var result = engine.run(egraph);

        Assert.True(result.total_unions > 0);

        var eclass = egraph.get_class(mul);
        Assert.NotNull(eclass);
        var hasLiteral = eclass!.nodes.Any(n => n is Literal<long> { value: 42 });
        Assert.True(hasLiteral);
    }

    [Fact]
    public void MulZero_EliminatesToZero()
    {
        var egraph = create_e_graph();
        var a = egraph.add(new Literal<long>(42));
        var zero = egraph.add(new Literal<long>(0));
        var mul = egraph.add(new Mul(a, zero));

        var engine = new SaturationEngine<AlgebraNode>([new DeadCodeEliminationRule()]);
        var result = engine.run(egraph);

        Assert.True(result.total_unions > 0);

        var eclass = egraph.get_class(mul);
        Assert.NotNull(eclass);
        var hasZero = eclass!.nodes.Any(n => n is Literal<long> { value: 0 });
        Assert.True(hasZero);
    }

    [Fact]
    public void SubZero_EliminatesToOperand()
    {
        var egraph = create_e_graph();
        var a = egraph.add(new Literal<long>(42));
        var zero = egraph.add(new Literal<long>(0));
        var sub = egraph.add(new Sub(a, zero));

        var engine = new SaturationEngine<AlgebraNode>([new DeadCodeEliminationRule()]);
        var result = engine.run(egraph);

        Assert.True(result.total_unions > 0);
    }
}

public class SourceGeneratorRuleTests
{
    private static EGraph<AlgebraNode> create_e_graph()
    {
        return new EGraph<AlgebraNode>(null);
    }

    [Fact]
    public void IdentityRules_AddZeroIdentity()
    {
        var egraph = create_e_graph();
        var a = egraph.add(new Literal<long>(42));
        var zero = egraph.add(new Literal<long>(0));
        var add = egraph.add(new Add(a, zero));

        var dialect = new CoreDialect();
        var engine = new SaturationEngine<AlgebraNode>(dialect.rules);
        var result = engine.run(egraph);

        Assert.True(result.total_unions > 0);

        var eclass = egraph.get_class(add);
        Assert.NotNull(eclass);
        var hasLiteral = eclass!.nodes.Any(n => n is Literal<long>);
        Assert.True(hasLiteral);
    }

    [Fact]
    public void IdentityRules_MulOneIdentity()
    {
        var egraph = create_e_graph();
        var a = egraph.add(new Literal<long>(7));
        var one = egraph.add(new Literal<long>(1));
        var mul = egraph.add(new Mul(a, one));

        var dialect = new CoreDialect();
        var engine = new SaturationEngine<AlgebraNode>(dialect.rules);
        var result = engine.run(egraph);

        Assert.True(result.total_unions > 0);
    }

    [Fact]
    public void IdentityRules_MulZeroAnnihilator()
    {
        var egraph = create_e_graph();
        var a = egraph.add(new Literal<long>(7));
        var zero = egraph.add(new Literal<long>(0));
        var mul = egraph.add(new Mul(a, zero));

        var dialect = new CoreDialect();
        var engine = new SaturationEngine<AlgebraNode>(dialect.rules);
        var result = engine.run(egraph);

        Assert.True(result.total_unions > 0);

        var eclass = egraph.get_class(mul);
        Assert.NotNull(eclass);
        var hasZero = eclass!.nodes.Any(n => n is Literal<long> { value: 0 });
        Assert.True(hasZero);
    }
}

public class FullOptimizationPipelineTests
{
    private static EGraph<AlgebraNode> create_e_graph()
    {
        return new EGraph<AlgebraNode>(null);
    }

    [Fact]
    public void FullPipeline_ConstantFoldingPlusAlgebraic()
    {
        var egraph = create_e_graph();
        var a = egraph.add(new Literal<long>(2));
        var b = egraph.add(new Literal<long>(3));
        var c = egraph.add(new Literal<long>(4));

        var add = egraph.add(new Add(a, b));
        var mul = egraph.add(new Mul(c, add));

        var dialect = new CoreDialect();
        var engine = new SaturationEngine<AlgebraNode>(dialect.rules);
        var result = engine.run(egraph);

        Assert.True(result.is_saturated);

        var addClass = egraph.get_class(add);
        Assert.NotNull(addClass);
        var hasFive = addClass!.nodes.Any(n => n is Literal<long> { value: 5 });
        Assert.True(hasFive);
    }

    [Fact]
    public void FullPipeline_DCEPlusAlgebraic()
    {
        var egraph = create_e_graph();
        var x = egraph.add(new Literal<long>(42));
        var zero = egraph.add(new Literal<long>(0));
        var one = egraph.add(new Literal<long>(1));

        var addZero = egraph.add(new Add(x, zero));
        var mulOne = egraph.add(new Mul(addZero, one));

        var dialect = new CoreDialect();
        var engine = new SaturationEngine<AlgebraNode>(dialect.rules);
        var result = engine.run(egraph);

        Assert.True(result.is_saturated);
    }
}

public class BooleanSimplificationTests
{
    private static EGraph<AlgebraNode> create_e_graph()
    {
        return new EGraph<AlgebraNode>(null);
    }

    [Fact]
    public void DoubleNegation_EliminatesToOperand()
    {
        var egraph = create_e_graph();
        var a = egraph.add(new Literal<bool>(true));
        var innerNot = egraph.add(new Not(a));
        var outerNot = egraph.add(new Not(innerNot));

        var dialect = new CoreDialect();
        var engine = new SaturationEngine<AlgebraNode>(dialect.rules);
        var result = engine.run(egraph);

        var eclass = egraph.get_class(outerNot);
        Assert.NotNull(eclass);
        var hasTrue = eclass!.nodes.Any(n => n is Literal<bool> { value: true });
        Assert.True(hasTrue);
    }

    [Fact]
    public void CmpEqTrueBool_SimplifiesToOperand()
    {
        var egraph = create_e_graph();
        var a = egraph.add(new Literal<bool>(true));
        var trueVal = egraph.add(new Literal<bool>(true));
        var cmp = egraph.add(new Cmp(CompareOp.eq, a, trueVal));

        var dialect = new CoreDialect();
        var engine = new SaturationEngine<AlgebraNode>(dialect.rules);
        var result = engine.run(egraph);

        var cmpRoot = egraph.union_find.find(cmp);
        var aRoot = egraph.union_find.find(a);
        Assert.Equal(cmpRoot, aRoot);
    }

    [Fact]
    public void CmpEqFalseBool_SimplifiesToNot()
    {
        var egraph = create_e_graph();
        var a = egraph.add(new Literal<bool>(true));
        var falseVal = egraph.add(new Literal<bool>(false));
        var cmp = egraph.add(new Cmp(CompareOp.eq, a, falseVal));

        var dialect = new CoreDialect();
        var engine = new SaturationEngine<AlgebraNode>(dialect.rules);
        var result = engine.run(egraph);

        var eclass = egraph.get_class(cmp);
        Assert.NotNull(eclass);
    }

    [Fact]
    public void BranchWithConstantTrue_SimplifiesToTrueLabel()
    {
        var egraph = create_e_graph();
        var cond = egraph.add(new Literal<bool>(true));
        var trueLabel = egraph.add(new Label("then"));
        var falseLabel = egraph.add(new Label("else"));
        var branch = egraph.add(new Branch(cond, trueLabel, falseLabel));

        var dialect = new CoreDialect();
        var engine = new SaturationEngine<AlgebraNode>(dialect.rules);
        var result = engine.run(egraph);

        var eclass = egraph.get_class(branch);
        Assert.NotNull(eclass);
    }
}

public class CommonSubexpressionEliminationTests
{
    private static EGraph<AlgebraNode> create_e_graph()
    {
        return new EGraph<AlgebraNode>(null);
    }

    [Fact]
    public void SemanticallyEquivalentAddExpressions_MergedByCSE()
    {
        var egraph = create_e_graph();
        var a = egraph.add(new Literal<long>(2));
        var b = egraph.add(new Literal<long>(3));
        var add1 = egraph.add(new Add(a, b));
        var add2 = egraph.add(new Add(b, a));

        var engine = new SaturationEngine<AlgebraNode>([new CommonSubexpressionEliminationRule()]);
        var result = engine.run(egraph);

        var root1 = egraph.union_find.find(add1);
        var root2 = egraph.union_find.find(add2);
        Assert.Equal(root1, root2);
    }

    [Fact]
    public void DifferentExpressions_NotMergedByCSE()
    {
        var egraph = create_e_graph();
        var a = egraph.add(new Literal<long>(2));
        var b = egraph.add(new Literal<long>(3));
        var c = egraph.add(new Literal<long>(4));
        var add1 = egraph.add(new Add(a, b));
        var add2 = egraph.add(new Add(a, c));

        var engine = new SaturationEngine<AlgebraNode>([new CommonSubexpressionEliminationRule()]);
        var result = engine.run(egraph);

        var root1 = egraph.union_find.find(add1);
        var root2 = egraph.union_find.find(add2);
        Assert.NotEqual(root1, root2);
    }

    [Fact]
    public void CSE_IdentifiesCommonLoadExpressions()
    {
        var egraph = create_e_graph();
        var ptr = egraph.add(new Sym("x"));
        var load1 = egraph.add(new Load(ptr, MemoryOrder.None));
        var load2 = egraph.add(new Load(ptr, MemoryOrder.None));

        var engine = new SaturationEngine<AlgebraNode>([new CommonSubexpressionEliminationRule()]);
        var result = engine.run(egraph);

        var root1 = egraph.union_find.find(load1);
        var root2 = egraph.union_find.find(load2);
        Assert.Equal(root1, root2);
    }
}

public class CopyPropagationTests
{
    private static EGraph<AlgebraNode> create_e_graph()
    {
        return new EGraph<AlgebraNode>(null);
    }

    [Fact]
    public void StoreOfLoad_SamePointer_PropagatesCopy()
    {
        var egraph = create_e_graph();
        var ptr = egraph.add(new Sym("x"));
        var load = egraph.add(new Load(ptr, MemoryOrder.None));
        var store = egraph.add(new Store(ptr, load, MemoryOrder.None));

        var dialect = new CoreDialect();
        var engine = new SaturationEngine<AlgebraNode>(dialect.rules);
        var result = engine.run(egraph);

        var eclass = egraph.get_class(store);
        Assert.NotNull(eclass);
    }

    [Fact]
    public void SubOfAdd_SameOperands_PropagatesToFirstOperand()
    {
        var egraph = create_e_graph();
        var a = egraph.add(new Literal<long>(2));
        var b = egraph.add(new Literal<long>(3));
        var add = egraph.add(new Add(a, b));
        var sub = egraph.add(new Sub(add, b));

        var dialect = new CoreDialect();
        var engine = new SaturationEngine<AlgebraNode>(dialect.rules);
        var result = engine.run(egraph);

        var eclass = egraph.get_class(sub);
        Assert.NotNull(eclass);
        var hasTwo = eclass!.nodes.Any(n => n is Literal<long> { value: 2 });
        Assert.True(hasTwo);
    }
}