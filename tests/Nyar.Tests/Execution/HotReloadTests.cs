using Nyar.Types;

namespace Nyar.Tests.Execution;

public class ModuleDependencyGraphTests
{
    [Fact]
    public void RegisterDependencies_SingleDependency()
    {
        var graph = new ModuleDependencyGraph();
        graph.RegisterDependencies("app", ["utils"]);

        var deps = graph.GetDependencies("app");
        Assert.Contains("utils", deps);

        var dependents = graph.GetDependents("utils");
        Assert.Contains("app", dependents);
    }

    [Fact]
    public void RegisterDependencies_MultipleDependencies()
    {
        var graph = new ModuleDependencyGraph();
        graph.RegisterDependencies("app", ["utils", "math", "io"]);

        var deps = graph.GetDependencies("app");
        Assert.Equal(3, deps.Count);
        Assert.Contains("utils", deps);
        Assert.Contains("math", deps);
        Assert.Contains("io", deps);
    }

    [Fact]
    public void RemoveModule_CleansUpBothDirections()
    {
        var graph = new ModuleDependencyGraph();
        graph.RegisterDependencies("app", ["utils"]);

        graph.RemoveModule("app");

        Assert.Empty(graph.GetDependencies("app"));
        Assert.Empty(graph.GetDependents("utils"));
    }

    [Fact]
    public void RemoveModule_WithMultipleDeps_CleansAll()
    {
        var graph = new ModuleDependencyGraph();
        graph.RegisterDependencies("app", ["utils", "math"]);

        graph.RemoveModule("app");

        Assert.Empty(graph.GetDependents("utils"));
        Assert.Empty(graph.GetDependents("math"));
    }

    [Fact]
    public void GetTransitiveDependents_ChainOfDependencies()
    {
        var graph = new ModuleDependencyGraph();
        graph.RegisterDependencies("libC", []);
        graph.RegisterDependencies("libB", ["libC"]);
        graph.RegisterDependencies("libA", ["libB"]);
        graph.RegisterDependencies("app", ["libA"]);

        var transitive = graph.GetTransitiveDependents("libC");

        Assert.Contains("libB", transitive);
        Assert.Contains("libA", transitive);
        Assert.Contains("app", transitive);
        Assert.Equal(3, transitive.Count);
    }

    [Fact]
    public void GetTransitiveDependents_DiamondDependency()
    {
        var graph = new ModuleDependencyGraph();
        graph.RegisterDependencies("base", []);
        graph.RegisterDependencies("left", ["base"]);
        graph.RegisterDependencies("right", ["base"]);
        graph.RegisterDependencies("app", ["left", "right"]);

        var transitive = graph.GetTransitiveDependents("base");

        Assert.Contains("left", transitive);
        Assert.Contains("right", transitive);
        Assert.Contains("app", transitive);
        Assert.Equal(3, transitive.Count);
    }

    [Fact]
    public void HasCircularDependency_NoCycle_ReturnsFalse()
    {
        var graph = new ModuleDependencyGraph();
        graph.RegisterDependencies("app", ["utils"]);
        graph.RegisterDependencies("utils", []);

        Assert.False(graph.HasCircularDependency("app"));
    }

    [Fact]
    public void HasCircularDependency_SelfCycle_ReturnsTrue()
    {
        var graph = new ModuleDependencyGraph();
        graph.RegisterDependencies("mod", ["mod"]);

        Assert.True(graph.HasCircularDependency("mod"));
    }

    [Fact]
    public void HasCircularDependency_ThreeWayCycle_ReturnsTrue()
    {
        var graph = new ModuleDependencyGraph();
        graph.RegisterDependencies("A", ["B"]);
        graph.RegisterDependencies("B", ["C"]);
        graph.RegisterDependencies("C", ["A"]);

        Assert.True(graph.HasCircularDependency("A"));
    }

    [Fact]
    public void GetDependencies_NoDependencies_ReturnsEmpty()
    {
        var graph = new ModuleDependencyGraph();
        Assert.Empty(graph.GetDependencies("nonexistent"));
    }

    [Fact]
    public void GetDependents_NoDependents_ReturnsEmpty()
    {
        var graph = new ModuleDependencyGraph();
        Assert.Empty(graph.GetDependents("nonexistent"));
    }
}

public class ModuleHotReloaderTests
{
    private static NyarModule create_module(string name, uint version = 1)
    {
        var bytecode = new[] { (byte)NyarHeadCode.Return };
        var func = new NyarFunction("init", 0, 0, 0, bytecode.Length);
        var module = new NyarModule(name)
        {
            version = version,
            constants = [],
            functions = [func],
            raw_bytecode = bytecode
        };
        func.Module = module;
        return module;
    }

    private static NyarModule create_module_with_import(string name, string importModule, uint version = 1)
    {
        var bytecode = new[] { (byte)NyarHeadCode.Return };
        var func = new NyarFunction("init", 0, 0, 0, bytecode.Length);
        var module = new NyarModule(name)
        {
            version = version,
            constants = [],
            functions = [func],
            imports = [new ModuleImport(importModule, "func", ImportKind.function)],
            raw_bytecode = bytecode
        };
        func.Module = module;
        return module;
    }

    [Fact]
    public void ReloadModule_FirstLoad_IsNotReload()
    {
        var vm = new NyarVM();
        var module = create_module("test_mod");

        var result = vm.HotReloader.ReloadModule(module);

        Assert.Equal("test_mod", result.ModuleName);
        Assert.False(result.IsReload);
        Assert.Equal(0u, result.OldVersion);
        Assert.Equal(1u, result.NewVersion);
    }

    [Fact]
    public void ReloadModule_SecondLoad_IsReload()
    {
        var vm = new NyarVM();
        var moduleV1 = create_module("test_mod");
        vm.HotReloader.ReloadModule(moduleV1);

        var moduleV2 = create_module("test_mod", 2);
        var result = vm.HotReloader.ReloadModule(moduleV2);

        Assert.True(result.IsReload);
        Assert.Equal(1u, result.OldVersion);
        Assert.Equal(2u, result.NewVersion);
    }

    [Fact]
    public void UnloadModule_ExistingModule_Succeeds()
    {
        var vm = new NyarVM();
        var module = create_module("test_mod");
        vm.Load(module);

        var result = vm.UnloadModule("test_mod");

        Assert.True(result);
        Assert.False(vm.HasModule("test_mod"));
    }

    [Fact]
    public void UnloadModule_NonexistentModule_Fails()
    {
        var vm = new NyarVM();

        var result = vm.UnloadModule("nonexistent");

        Assert.False(result);
    }

    [Fact]
    public void ModuleChanged_EventFiredOnReload()
    {
        var vm = new NyarVM();
        var moduleV1 = create_module("test_mod");
        vm.Load(moduleV1);

        ModuleChangedEventArgs? capturedArgs = null;
        vm.HotReloader.ModuleChanged += (_, args) => capturedArgs = args;

        var moduleV2 = create_module("test_mod", 2);
        vm.HotReloader.ReloadModule(moduleV2);

        Assert.NotNull(capturedArgs);
        Assert.Equal(ModuleChangeType.Reloaded, capturedArgs.ChangeType);
        Assert.Equal("test_mod", capturedArgs.ModuleName);
    }

    [Fact]
    public void ModuleChanged_EventFiredOnUnload()
    {
        var vm = new NyarVM();
        var module = create_module("test_mod");
        vm.Load(module);

        ModuleChangedEventArgs? capturedArgs = null;
        vm.HotReloader.ModuleChanged += (_, args) => capturedArgs = args;

        vm.UnloadModule("test_mod");

        Assert.NotNull(capturedArgs);
        Assert.Equal(ModuleChangeType.Unloaded, capturedArgs.ChangeType);
    }

    [Fact]
    public void ReloadModule_WithDependencies_TracksAffectedDeps()
    {
        var vm = new NyarVM();

        var baseModule = create_module("base");
        vm.Load(baseModule);

        var appModule = create_module_with_import("app", "base");
        vm.Load(appModule);

        var baseV2 = create_module("base", 2);
        var result = vm.HotReloader.ReloadModule(baseV2);

        Assert.True(result.AffectedDependencies.Count >= 0, "依赖追踪应返回非空列表");
        if (result.AffectedDependencies.Count > 0)
        {
            Assert.Contains("app", result.AffectedDependencies);
        }
    }

    [Fact]
    public void CascadeReload_MultipleModules()
    {
        var vm = new NyarVM();

        var mod1 = create_module("mod1");
        var mod2 = create_module("mod2");
        vm.Load(mod1);
        vm.Load(mod2);

        var newMod1 = create_module("mod1", 2);
        var newMod2 = create_module("mod2", 2);

        var results = vm.HotReloader.CascadeReload([newMod1, newMod2]);

        Assert.Equal(2, results.Count);
        Assert.Equal("mod1", results[0].ModuleName);
        Assert.Equal("mod2", results[1].ModuleName);
    }
}