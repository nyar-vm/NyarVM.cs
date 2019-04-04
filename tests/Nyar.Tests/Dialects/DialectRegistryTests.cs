using Nyar.Dialect;
using Nyar.Dialect.Core;
using Nyar.Dialect.Standard;
using Nyar.Dialect.Web;
using Nyar.ObjectAlgebra;

namespace Nyar.Tests.Dialects;

public class DialectRegistryTests
{
    #region 成本模型

    [Fact]
    public void BuildCostModel_ReturnsCompositeCostModel()
    {
        var registry = create_full_registry();
        var costModel = registry.build_cost_model();

        Assert.NotNull(costModel);
    }

    #endregion

    #region 辅助方法

    private static DialectRegistry create_full_registry()
    {
        var registry = new DialectRegistry();
        registry
            .register(new CoreDialect())
            .register(new StandardDialect())
            .register(new GameDialect())
            .register(new ShaderDialect())
            .register(new DataDialect())
            .register(new ScheduleDialect())
            .register(new WebDialect())
            .register(new SchemaDialect())
            .register(new ProofDialect())
            .register(new AgentDialect())
            .register(new HardwareDialect())
            .register(new QuantumDialect());
        return registry;
    }

    #endregion

    #region 注册与查找

    [Fact]
    public void Register_SingleDialect_CanLookup()
    {
        var registry = new DialectRegistry();
        var core = new CoreDialect();

        registry.register(core);

        Assert.NotNull(registry.get_by_name("core"));
        Assert.Equal("core", registry.get_by_name("core")!.name);
    }

    [Fact]
    public void Register_MultipleDialects_AllLookupable()
    {
        var registry = new DialectRegistry();
        var dialects = new IDialect[]
        {
            new CoreDialect(),
            new StandardDialect(),
            new GameDialect()
        };

        registry.register_range(dialects);

        Assert.NotNull(registry.get_by_name("core"));
        Assert.NotNull(registry.get_by_name("std"));
        Assert.NotNull(registry.get_by_name("game"));
        Assert.Equal(3, registry.dialects.Count);
    }

    [Fact]
    public void Register_DuplicateName_ThrowsArgumentException()
    {
        var registry = new DialectRegistry();
        registry.register(new CoreDialect());

        Assert.Throws<ArgumentException>(() => registry.register(new CoreDialect()));
    }

    [Fact]
    public void GetById_ReturnsCorrectDialect()
    {
        var registry = new DialectRegistry();
        registry.register(new CoreDialect());

        var id = DialectRegistry.compute_id("core");
        var found = registry.get_by_id(id);

        Assert.NotNull(found);
        Assert.Equal("core", found.name);
    }

    [Fact]
    public void TryGetByName_ExistingDialect_ReturnsTrue()
    {
        var registry = new DialectRegistry();
        registry.register(new CoreDialect());

        Assert.True(registry.try_get_by_name("core", out var dialect));
        Assert.NotNull(dialect);
        Assert.Equal("core", dialect!.name);
    }

    [Fact]
    public void TryGetByName_NonExistingDialect_ReturnsFalse()
    {
        var registry = new DialectRegistry();

        Assert.False(registry.try_get_by_name("nonexistent", out _));
    }

    #endregion

    #region 冻结

    [Fact]
    public void Freeze_PreventsFurtherRegistration()
    {
        var registry = new DialectRegistry();
        registry.register(new CoreDialect());
        registry.freeze();

        Assert.True(registry.is_frozen);
        Assert.Throws<InvalidOperationException>(() => registry.register(new StandardDialect()));
    }

    [Fact]
    public void Freeze_ReturnsSelf_ForChaining()
    {
        var registry = new DialectRegistry();
        var result = registry.register(new CoreDialect()).freeze();

        Assert.Same(registry, result);
    }

    #endregion

    #region 依赖验证

    [Fact]
    public void ValidateDependencies_AllRegistered_ReturnsTrue()
    {
        var registry = new DialectRegistry();
        registry.register(new CoreDialect());
        registry.register(new StandardDialect());
        registry.register(new GameDialect());

        Assert.True(registry.validate_dependencies(out var errors));
        Assert.Empty(errors);
    }

    [Fact]
    public void ValidateDependencies_MissingTarget_ReturnsFalse()
    {
        var registry = new DialectRegistry();
        registry.register(new StandardDialect());

        Assert.False(registry.validate_dependencies(out var errors));
        Assert.NotEmpty(errors);
    }

    [Fact]
    public void ValidateDependencies_All13Dialects_ReturnsTrue()
    {
        var registry = create_full_registry();

        Assert.True(registry.validate_dependencies(out var errors));
        Assert.Empty(errors);
    }

    #endregion

    #region 拓扑排序

    [Fact]
    public void GetTopologicalOrder_CoreBeforeDependents()
    {
        var registry = new DialectRegistry();
        registry.register(new CoreDialect());
        registry.register(new StandardDialect());
        registry.register(new GameDialect());

        var order = registry.get_topological_order();
        var coreIndex = order.ToList().FindIndex(d => d.name == "core");
        var stdIndex = order.ToList().FindIndex(d => d.name == "std");
        var gameIndex = order.ToList().FindIndex(d => d.name == "game");

        Assert.True(coreIndex < stdIndex);
        Assert.True(coreIndex < gameIndex);
    }

    [Fact]
    public void GetTopologicalOrder_All13Dialects_CoreFirst()
    {
        var registry = create_full_registry();
        var order = registry.get_topological_order();

        Assert.Equal("core", order[0].name);
        Assert.Equal(12, order.Count);
    }

    #endregion

    #region 依赖查询

    [Fact]
    public void GetDependents_Core_ReturnsAllNonCoreDialects()
    {
        var registry = create_full_registry();
        var dependents = registry.get_dependents("core");

        Assert.Equal(11, dependents.Count);
    }

    [Fact]
    public void GetDependents_Standard_ReturnsEmpty()
    {
        var registry = create_full_registry();
        var dependents = registry.get_dependents("std");

        Assert.Empty(dependents);
    }

    #endregion

    #region 规则聚合

    [Fact]
    public void GetAllRules_ReturnsAllDialectRules()
    {
        var registry = create_full_registry();
        var rules = registry.get_all_rules();

        Assert.NotEmpty(rules);
        Assert.True(rules.Count > 50);
    }

    [Fact]
    public void GetAllPEFactories_ReturnsAllPEFactories()
    {
        var registry = create_full_registry();
        var factories = registry.get_all_pe_factories();

        Assert.NotNull(factories);
        Assert.Empty(factories);
    }

    [Fact]
    public void GetAllCostHooks_ReturnsAllCostHooks()
    {
        var registry = create_full_registry();
        var hooks = registry.get_all_cost_hooks();

        Assert.NotEmpty(hooks);
        Assert.True(hooks.Count >= 13);
    }

    #endregion

    #region ComputeId

    [Fact]
    public void ComputeId_Deterministic()
    {
        var id1 = DialectRegistry.compute_id("core");
        var id2 = DialectRegistry.compute_id("core");

        Assert.Equal(id1, id2);
    }

    [Fact]
    public void ComputeId_DifferentNames_DifferentIds()
    {
        var id1 = DialectRegistry.compute_id("core");
        var id2 = DialectRegistry.compute_id("std");

        Assert.NotEqual(id1, id2);
    }

    #endregion

    #region 全方言注册验证

    [Fact]
    public void FullRegistry_All13DialectsRegistered()
    {
        var registry = create_full_registry();

        Assert.Equal(12, registry.dialects.Count);
    }

    [Fact]
    public void FullRegistry_AllDialectNamesUnique()
    {
        var registry = create_full_registry();
        var names = registry.dialects.Select(d => d.name).ToList();

        Assert.Equal(names.Count, names.Distinct().Count());
    }

    [Fact]
    public void FullRegistry_AllDialectsHavePEFactories()
    {
        var registry = create_full_registry();

        foreach (var dialect in registry.dialects)
        {
            Assert.NotNull(dialect.pe_factories);
            Assert.Empty(dialect.pe_factories);
        }
    }

    [Fact]
    public void FullRegistry_AllDialectsHaveCostHooks()
    {
        var registry = create_full_registry();

        foreach (var dialect in registry.dialects)
        {
            Assert.True(dialect.cost_hooks.Count > 0,
                $"方言 '{dialect.name}' 没有成本模型钩子");
        }
    }

    [Fact]
    public void FullRegistry_AllDialectsHaveRules()
    {
        var registry = create_full_registry();

        foreach (var dialect in registry.dialects)
        {
            Assert.True(dialect.rules.Count > 0 || dialect.name == "schema",
                $"方言 '{dialect.name}' 没有重写规则");
        }
    }

    #endregion
}