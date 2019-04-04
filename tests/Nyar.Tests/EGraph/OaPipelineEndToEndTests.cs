using System.Collections.Immutable;
using Nyar.EGraph;
using Nyar.IR.Extractor;
using Nyar.IR.Intent;
using Nyar.ObjectAlgebra;
using Nyar.ObjectAlgebra.Generated.Core;

namespace Nyar.Tests.EGraph;

/// <summary>
///     OA 全管线端到端验证：验证 AST→term→EGraph&lt;ENode&gt;→optimize→extract 链路完整性
/// </summary>
public class OaPipelineEndToEndTests
{
    /// <summary>
    ///     验证 OA algebra → Reifier → EGraph&lt;ENode&gt; → Extract 全链路
    /// </summary>
    [Fact]
    public void FullPipeline_AddMulLiteral_ReifyExtractRoundTrip()
    {
        // 1. 创建 EGraph&lt;ENode&gt;（替代旧的 EGraph&lt;AlgebraNode&gt;）
        var egraph = new EGraph<ENode>(null);

        // 2. 使用 CoreReifier 通过 algebra 接口构建表达式
        var reifier = new CoreReifier(egraph);

        // 构建表达式: Add(Literal(40), Literal(2))
        var forty = reifier.Literal<long>(40);
        var two = reifier.Literal<long>(2);
        var add = reifier.Add(forty, two);

        // 验证 reifier 返回了有效的 Id
        Assert.NotEqual(default(Id), forty);
        Assert.NotEqual(default(Id), two);
        Assert.NotEqual(default(Id), add);

        // 3. 验证 Symbols 字典包含所有 Core 操作符
        Assert.True(reifier.symbols.Count > 0);
        Assert.True(reifier.symbols.ContainsKey("add"));
        Assert.True(reifier.symbols.ContainsKey("lit"));
        Assert.True(reifier.symbols.ContainsKey("mul"));

        // 4. 从 EGraph 中提取最优程序（ENode 树）
        var extractedNode = Extractor.extract_tree(egraph, add);

        // 验证提取的节点结构
        Assert.NotNull(extractedNode);
        Assert.Equal("add", extractedNode.@operator.name);
        Assert.Equal(2, extractedNode.children.Length);

        // 5. 构建更复杂的表达式: Mul(Add(1, 2), 3)
        var one = reifier.Literal<long>(1);
        var add2 = reifier.Add(one, two); // Add(1, 2)
        var three = reifier.Literal<long>(3);
        var mul = reifier.Mul(add2, three); // Mul(Add(1, 2), 3)

        var mulExtract = Extractor.extract_tree(egraph, mul);
        Assert.NotNull(mulExtract);
        Assert.Equal("mul", mulExtract.@operator.name);
        Assert.Equal(2, mulExtract.children.Length);
    }

    /// <summary>
    ///     验证 ENode 的 OperatorKey 和 Symbol 一致性
    /// </summary>
    [Fact]
    public void OperatorDescriptor_CoreSymbols_AreConsistent()
    {
        // 验证生成的 CoreSymbols 提供所有必要的操作符
        Assert.NotNull(CoreSymbols.Add);
        Assert.NotNull(CoreSymbols.Mul);
        Assert.NotNull(CoreSymbols.Sub);
        Assert.NotNull(CoreSymbols.Div);
        Assert.NotNull(CoreSymbols.Literal);

        // 验证操作符描述符的字段
        Assert.Equal("add", CoreSymbols.Add.name);
        Assert.Equal("core", CoreSymbols.Add.dialect_name);
        Assert.Equal(2, CoreSymbols.Add.arity);

        // 验证 ByName 字典
        Assert.True(CoreSymbols.ByName.ContainsKey("add"));
        Assert.True(CoreSymbols.ByName.ContainsKey("mul"));
    }

    /// <summary>
    ///     验证 CoreReifier 的 VIReifier&lt;Id&gt; 接口
    /// </summary>
    [Fact]
    public void CoreReifier_Implements_IReifier()
    {
        var egraph = new EGraph<ENode>(null);
        IReifier<Id> reifier = new CoreReifier(egraph);

        Assert.NotNull(reifier.symbols);
        Assert.True(reifier.symbols.Count > 0);
    }

    /// <summary>
    ///     验证新的开放节点模型：不需要修改中央节点类型即可使用 ENode
    /// </summary>
    [Fact]
    public void ENode_OpenModel_WorksWithoutAlgebraNode()
    {
        // 使用 ENode 和 CoreSymbols 直接创建节点，无需 AlgebraNode
        var addNode = new ENode(CoreSymbols.Add, ImmutableArray<Id>.Empty);

        Assert.Equal("add", addNode.@operator.name);
        Assert.Equal("core", addNode.@operator.dialect_name);
        Assert.Empty(addNode.children);

        // 验证创建多个不同的 ENode 并通过 EGraph 管理
        var egraph = new EGraph<ENode>(null);

        var child1 = new ENode(CoreSymbols.Literal, ImmutableArray<Id>.Empty, new { value = (long)1 });
        var child2 = new ENode(CoreSymbols.Literal, ImmutableArray<Id>.Empty, new { value = (long)2 });

        var id1 = egraph.add(child1);
        var id2 = egraph.add(child2);

        var mulNode = new ENode(CoreSymbols.Mul, [id1, id2]);
        var mulId = egraph.add(mulNode);

        Assert.NotEqual(default(Id), mulId);
    }
}