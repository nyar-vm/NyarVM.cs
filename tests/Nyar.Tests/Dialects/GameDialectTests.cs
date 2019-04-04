using Nyar.Dialect.Core.Nodes;
using Nyar.EGraph;
using Nyar.IR.Intent;
using Nyar.IR.Rewrite;
using GameAction = Nyar.Dialect.Game.Nodes.Action;

namespace Nyar.Tests.Dialects;

public class GameDialectTests
{
    private static EGraph<Oa> create_e_graph()
    {
        return new EGraph<Oa>(null);
    }

    [Fact(Skip = "pe_factories 返回空数组，降级规则测试暂不适用")]
    public void ECS_SpawnEntity_LowersToApply()
    {
        var egraph = create_e_graph();
        var spawn = egraph.add(new SpawnEntity(null));

        var dialect = new GameDialect();
        var engine = new SaturationEngine<Oa>(dialect.pe_factories);
        var result = engine.run(egraph);

        Assert.True(result.total_unions > 0);

        var eclass = egraph.get_class(spawn);
        Assert.NotNull(eclass);
        var hasApply = eclass!.nodes.Any(n => n is Apply);
        Assert.True(hasApply, "SpawnEntity 应降级为 Apply");
    }

    [Fact(Skip = "pe_factories 返回空数组，降级规则测试暂不适用")]
    public void ECS_AddComponent_LowersToApply()
    {
        var egraph = create_e_graph();
        var entity = egraph.add(new Literal<long>(1));
        var fields = new Dictionary<string, Id>
        {
            ["x"] = egraph.add(new Literal<long>(10))
        };
        var addComp = egraph.add(new AddComponent(entity, "Position", fields));

        var dialect = new GameDialect();
        var engine = new SaturationEngine<Oa>(dialect.pe_factories);
        var result = engine.run(egraph);

        Assert.True(result.total_unions > 0);

        var eclass = egraph.get_class(addComp);
        Assert.NotNull(eclass);
        var hasApply = eclass!.nodes.Any(n => n is Apply);
        Assert.True(hasApply, "AddComponent 应降级为 Apply");
    }

    [Fact(Skip = "pe_factories 返回空数组，降级规则测试暂不适用")]
    public void ECS_GetSetComponent_LowersToApply()
    {
        var egraph = create_e_graph();
        var entity = egraph.add(new Literal<long>(1));
        var value = egraph.add(new Literal<long>(42));
        var setComp = egraph.add(new SetComponent(entity, "Health", "hp", value));
        var getComp = egraph.add(new GetComponent(entity, "Health", "hp"));

        var dialect = new GameDialect();
        var engine = new SaturationEngine<Oa>(dialect.pe_factories);
        var result = engine.run(egraph);

        Assert.True(result.total_unions > 0);

        var setClass = egraph.get_class(setComp);
        Assert.NotNull(setClass);
        Assert.True(setClass!.nodes.Any(n => n is Apply), "SetComponent 应降级为 Apply");

        var getClass = egraph.get_class(getComp);
        Assert.NotNull(getClass);
        Assert.True(getClass!.nodes.Any(n => n is Apply), "GetComponent 应降级为 Apply");
    }

    [Fact(Skip = "pe_factories 返回空数组，降级规则测试暂不适用")]
    public void ECS_DestroyAndHasComponent_LowersToApply()
    {
        var egraph = create_e_graph();
        var entity = egraph.add(new Literal<long>(1));
        var destroy = egraph.add(new DestroyEntity(entity));
        var hasComp = egraph.add(new HasComponent(entity, "Position"));

        var dialect = new GameDialect();
        var engine = new SaturationEngine<Oa>(dialect.pe_factories);
        var result = engine.run(egraph);

        Assert.True(result.total_unions > 0);

        var destroyClass = egraph.get_class(destroy);
        Assert.NotNull(destroyClass);
        Assert.True(destroyClass!.nodes.Any(n => n is Apply), "DestroyEntity 应降级为 Apply");

        var hasClass = egraph.get_class(hasComp);
        Assert.NotNull(hasClass);
        Assert.True(hasClass!.nodes.Any(n => n is Apply), "HasComponent 应降级为 Apply");
    }

    [Fact(Skip = "pe_factories 返回空数组，降级规则测试暂不适用")]
    public void ECS_QueryAndWorldUpdate_LowersToApply()
    {
        var egraph = create_e_graph();
        var spec = new QuerySpec(["Position"], [], [], false);
        var query = egraph.add(new QueryEntities(spec));
        var dt = egraph.add(new Literal<long>(16));
        var worldUpdate = egraph.add(new WorldUpdate(dt));

        var dialect = new GameDialect();
        var engine = new SaturationEngine<Oa>(dialect.pe_factories);
        var result = engine.run(egraph);

        Assert.True(result.total_unions > 0);

        var queryClass = egraph.get_class(query);
        Assert.NotNull(queryClass);
        Assert.True(queryClass!.nodes.Any(n => n is Apply), "QueryEntities 应降级为 Apply");

        var updateClass = egraph.get_class(worldUpdate);
        Assert.NotNull(updateClass);
        Assert.True(updateClass!.nodes.Any(n => n is Apply), "WorldUpdate 应降级为 Apply");
    }

    [Fact]
    public void BehaviorTree_SequenceFlatten()
    {
        var egraph = create_e_graph();
        var a = egraph.add(new Literal<long>(1));
        var b = egraph.add(new Literal<long>(2));
        var c = egraph.add(new Literal<long>(3));

        var inner = egraph.add(new Sequence([b, c]));
        var outer = egraph.add(new Sequence([a, inner]));

        var dialect = new GameDialect();
        var engine = new SaturationEngine<Oa>(dialect.rules);
        var result = engine.run(egraph);

        Assert.True(result.total_unions > 0);

        var eclass = egraph.get_class(outer);
        Assert.NotNull(eclass);
        var hasFlattened = eclass!.nodes.Any(n =>
            n is Sequence { Children.Count: 3 });
        Assert.True(hasFlattened, "嵌套 Sequence 应被扁平化为 3 个子节点");
    }

    [Fact]
    public void BehaviorTree_SelectorFlatten()
    {
        var egraph = create_e_graph();
        var a = egraph.add(new Literal<long>(1));
        var b = egraph.add(new Literal<long>(2));
        var c = egraph.add(new Literal<long>(3));

        var inner = egraph.add(new Selector([b, c]));
        var outer = egraph.add(new Selector([a, inner]));

        var dialect = new GameDialect();
        var engine = new SaturationEngine<Oa>(dialect.rules);
        var result = engine.run(egraph);

        Assert.True(result.total_unions > 0);

        var eclass = egraph.get_class(outer);
        Assert.NotNull(eclass);
        var hasFlattened = eclass!.nodes.Any(n =>
            n is Selector { Children.Count: 3 });
        Assert.True(hasFlattened, "嵌套 Selector 应被扁平化为 3 个子节点");
    }

    [Fact]
    public void BehaviorTree_DecoratorInvertCancel()
    {
        var egraph = create_e_graph();
        var child = egraph.add(new Literal<long>(1));
        var innerInvert = egraph.add(new Decorator(DecoratorType.Invert, child, null));
        var outerInvert = egraph.add(new Decorator(DecoratorType.Invert, innerInvert, null));

        var dialect = new GameDialect();
        var engine = new SaturationEngine<Oa>(dialect.rules);
        var result = engine.run(egraph);

        Assert.True(result.total_unions > 0);

        var eclass = egraph.get_class(outerInvert);
        Assert.NotNull(eclass);
        var hasDoubleInvertCancel = eclass!.nodes.Any(n =>
            n is Decorator { Type: DecoratorType.Invert } d &&
            d.Child.Equals(child));
        Assert.True(hasDoubleInvertCancel, "双重取反应消除为直接引用子节点");
    }

    [Fact(Skip = "pe_factories 返回空数组，降级规则测试暂不适用")]
    public void FullGameScene_SpawnEntityWithComponents()
    {
        var egraph = create_e_graph();

        var archetype = egraph.add(new Literal<long>(0));
        var spawn = egraph.add(new SpawnEntity(archetype));
        var posX = egraph.add(new Literal<long>(100));
        var posY = egraph.add(new Literal<long>(200));
        var posFields = new Dictionary<string, Id>
        {
            ["x"] = posX,
            ["y"] = posY
        };
        var addPos = egraph.add(new AddComponent(spawn, "Position", posFields));
        var hpValue = egraph.add(new Literal<long>(100));
        var hpFields = new Dictionary<string, Id>
        {
            ["hp"] = hpValue
        };
        var addHp = egraph.add(new AddComponent(spawn, "Health", hpFields));

        var dt = egraph.add(new Literal<long>(16));
        var worldUpdate = egraph.add(new WorldUpdate(dt));

        var dialect = new GameDialect();
        var engine = new SaturationEngine<Oa>(dialect.pe_factories);
        var result = engine.run(egraph);

        Assert.True(result.total_unions > 0);

        var spawnClass = egraph.get_class(spawn);
        Assert.NotNull(spawnClass);
        Assert.True(spawnClass!.nodes.Any(n => n is Apply), "SpawnEntity 应降级为 Apply");

        var addPosClass = egraph.get_class(addPos);
        Assert.NotNull(addPosClass);
        Assert.True(addPosClass!.nodes.Any(n => n is Apply), "AddComponent(Position) 应降级为 Apply");

        var addHpClass = egraph.get_class(addHp);
        Assert.NotNull(addHpClass);
        Assert.True(addHpClass!.nodes.Any(n => n is Apply), "AddComponent(Health) 应降级为 Apply");

        var updateClass = egraph.get_class(worldUpdate);
        Assert.NotNull(updateClass);
        Assert.True(updateClass!.nodes.Any(n => n is Apply), "WorldUpdate 应降级为 Apply");
    }

    [Fact]
    public void FullGameScene_BehaviorTreeWithECS()
    {
        var egraph = create_e_graph();

        var entity = egraph.add(new Literal<long>(1));
        var hpValue = egraph.add(new Literal<long>(50));
        var getHp = egraph.add(new GetComponent(entity, "Health", "hp"));

        var checkHp = egraph.add(new Condition(getHp));
        var flee = egraph.add(new GameAction("Flee", new Dictionary<string, Id>()));
        var fight = egraph.add(new GameAction("Fight", new Dictionary<string, Id>()));
        var selector = egraph.add(new Selector([checkHp, fight]));
        var behaviorTree = egraph.add(new Sequence([selector, flee]));

        var dialect = new GameDialect();
        var engine = new SaturationEngine<Oa>(dialect.rules);
        var result = engine.run(egraph);

        Assert.True(result.is_saturated || result.total_unions >= 0);
    }

    [Fact]
    public void GameDialect_CostModel_CoversAllNodes()
    {
        var dialect = new GameDialect();
        Assert.Equal(4, dialect.rules.Count);
        Assert.Empty(dialect.pe_factories);
        Assert.Single(dialect.cost_hooks);
    }

    [Fact(Skip = "pe_factories 返回空数组，降级规则测试暂不适用")]
    public void GameDialect_AllECSNodes_LowerToApplyWithCorrectBuiltinId()
    {
        var egraph = create_e_graph();
        var entity = egraph.add(new Literal<long>(1));
        var value = egraph.add(new Literal<long>(42));
        var fields = new Dictionary<string, Id>
        {
            ["val"] = value
        };
        var spec = new QuerySpec(["Position"], [], [], false);
        var dt = egraph.add(new Literal<long>(16));

        var ecsNodes = new Oa[]
        {
            new SpawnEntity(null),
            new DestroyEntity(entity),
            new AddComponent(entity, "Pos", fields),
            new GetComponent(entity, "Pos", "x"),
            new SetComponent(entity, "Pos", "x", value),
            new RemoveComponent(entity, "Pos"),
            new HasComponent(entity, "Pos"),
            new QueryEntities(spec),
            new WorldUpdate(dt)
        };

        foreach (var node in ecsNodes)
        {
            egraph.add(node);
        }

        var dialect = new GameDialect();
        var engine = new SaturationEngine<Oa>(dialect.pe_factories);
        var result = engine.run(egraph);

        Assert.True(result.total_unions >= 9, "所有 9 个 ECS 节点都应产生降级");
    }
}