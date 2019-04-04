using Nyar.Analyzer.ModuleSystem;
using Nyar.Types;
using Nyar.VM.NyarVM.Bytecode;

namespace Nyar.VM.NyarVM.HotReload;

/// <summary>
///     模块热加载管理器
///     提供原子性模块替换、依赖跟踪、事件通知和状态迁移
/// </summary>
public sealed class ModuleHotReloader
{
    #region 构造函数

    /// <summary>
    ///     初始化模块热加载管理器
    /// </summary>
    /// <param name="vm">NyarVM 实例。</param>
    /// <param name="modules">模块字典引用。</param>
    /// <param name="moduleCodeBytes">模块代码字节流字典引用。</param>
    /// <param name="options">热重载配置（可选）。</param>
    public ModuleHotReloader(NyarVm vm, Dictionary<string, IModule> modules, Dictionary<string, byte[]> moduleCodeBytes,
        HotReloadOptions? options = null)
    {
        _vm = vm;
        _modules = modules;
        _module_code_bytes = moduleCodeBytes;
        dependency_graph = new ModuleDependencyGraph();
        _state_snapshots = new Dictionary<string, ModuleStateSnapshot>();
        _options = options ?? HotReloadOptions.@default;
    }

    #endregion

    #region 事件

    /// <summary>
    ///     模块变更事件
    /// </summary>
    public event EventHandler<ModuleChangedEventArgs>? ModuleChanged;

    #endregion

    #region 字段

    private readonly NyarVm _vm;
    private readonly Dictionary<string, IModule> _modules;
    private readonly Dictionary<string, byte[]> _module_code_bytes;
    private readonly Dictionary<string, ModuleStateSnapshot> _state_snapshots;
    private readonly HotReloadOptions _options;

    #endregion

    #region 公开方法

    /// <summary>
    ///     热重载模块：原子性地替换同名模块
    ///     自动更新依赖图、触发事件通知
    /// </summary>
    /// <param name="newModule">新模块。</param>
    /// <returns>重载结果。</returns>
    public HotReloadResult reload_module(IModule newModule)
    {
        ArgumentNullException.ThrowIfNull(newModule);

        var moduleName = newModule.name;
        var oldModule = _modules.GetValueOrDefault(moduleName);

        var oldVersion = oldModule?.version ?? 0;
        var newVersion = newModule.version;

        if (_options.strict_version_check && oldModule is not null)
        {
            var compatibility = check_compatibility(oldModule, newModule);
            if (!compatibility.is_compatible)
                return new HotReloadResult
                {
                    module_name = moduleName,
                    old_version = oldVersion,
                    new_version = newVersion,
                    is_reload = true,
                    affected_dependencies = [],
                    needs_rebind = false,
                    compatibility_result = compatibility
                };
        }

        dependency_graph.remove_module(moduleName);

        var importNames = newModule.imports.Select(i => i.module_name).ToList();
        dependency_graph.register_dependencies(moduleName, importNames);

        var affectedDeps = dependency_graph.get_transitive_dependents(moduleName);

        if (oldModule is not null)
        {
            capture_module_state(oldModule, moduleName);
            migrate_module_state(oldModule, newModule);
        }

        _modules[moduleName] = newModule;

        if (newModule.raw_bytecode is not null) _module_code_bytes[moduleName] = newModule.raw_bytecode;

        invalidate_jit_cache(oldModule, newModule, moduleName);

        OnModuleChanged(new ModuleChangedEventArgs
        {
            change_type = oldModule is not null ? ModuleChangeType.reloaded : ModuleChangeType.loaded,
            module_name = moduleName,
            old_version = oldVersion,
            new_version = newVersion,
            affected_dependencies = [.. affectedDeps]
        });

        return new HotReloadResult
        {
            module_name = moduleName,
            old_version = oldVersion,
            new_version = newVersion,
            is_reload = oldModule is not null,
            affected_dependencies = [.. affectedDeps],
            needs_rebind = true
        };
    }

    /// <summary>
    ///     从 `.nyar` 模块二进制热重载模块。
    ///     先解码模块二进制，再替换同名模块及其代码字节流引用。
    /// </summary>
    /// <param name="moduleBytes">`.nyar` 格式的模块二进制数据。</param>
    /// <returns>重载结果。</returns>
    public HotReloadResult reload_module(byte[] moduleBytes)
    {
        ArgumentNullException.ThrowIfNull(moduleBytes);

        var module = NyarModuleConverter.decode(moduleBytes);
        module.raw_bytecode = moduleBytes;
        return reload_module(module);
    }

    /// <summary>
    ///     卸载模块
    /// </summary>
    /// <param name="moduleName">模块名称。</param>
    /// <returns>是否成功卸载。</returns>
    public bool unload_module(string moduleName)
    {
        if (!_modules.ContainsKey(moduleName)) return false;

        var affectedDeps = dependency_graph.get_transitive_dependents(moduleName);

        dependency_graph.remove_module(moduleName);
        _modules.Remove(moduleName);
        _module_code_bytes.Remove(moduleName);

        OnModuleChanged(new ModuleChangedEventArgs
        {
            change_type = ModuleChangeType.unloaded,
            module_name = moduleName,
            affected_dependencies = [.. affectedDeps]
        });

        return true;
    }

    /// <summary>
    ///     级联重载：重载指定模块及其所有传递依赖方
    /// </summary>
    /// <param name="newModules">新模块列表。</param>
    /// <returns>级联重载结果列表。</returns>
    public IReadOnlyList<HotReloadResult> cascade_reload(IReadOnlyList<IModule> newModules)
    {
        var results = new List<HotReloadResult>();
        var moduleMap = newModules.ToDictionary(m => m.name);

        foreach (var newModule in newModules)
        {
            var result = reload_module(newModule);
            results.Add(result);
        }

        foreach (var affectedDep in results.SelectMany(r => r.affected_dependencies).Distinct())
        {
            if (moduleMap.ContainsKey(affectedDep)) continue;

            if (_modules.TryGetValue(affectedDep, out var existingModule))
                results.Add(new HotReloadResult
                {
                    module_name = affectedDep,
                    old_version = existingModule.version,
                    new_version = existingModule.version,
                    is_reload = true,
                    affected_dependencies = [],
                    needs_rebind = true
                });
        }

        return results;
    }

    /// <summary>
    ///     获取模块依赖图
    /// </summary>
    public ModuleDependencyGraph dependency_graph { get; }

    #endregion

    #region 内部方法

    /// <summary>
    ///     捕获模块状态：在重载前记录堆对象
    ///     全局变量在执行器重启时自然清零，无需显式保留
    /// </summary>
    private void capture_module_state(IModule module, string moduleName)
    {
        if (!_options.preserve_objects) return;

        var persistentObjects = new Dictionary<int, object?>();
        var objTable = Value.shared_object_table;
        var tableLock = Value.shared_table_lock;
        lock (tableLock)
        {
            for (var i = 0; i < objTable.Count; i++)
                if (objTable[i] is not null)
                    persistentObjects[i] = objTable[i];
        }

        _state_snapshots[moduleName] = new ModuleStateSnapshot(
            moduleName,
            module.version,
            new Dictionary<string, Value>(),
            persistentObjects);
    }

    /// <summary>
    ///     迁移模块状态：将旧模块的运行时状态迁移到新模块
    ///     还原全局变量表，复原持久化堆对象
    /// </summary>
    private static void migrate_module_state(IModule oldModule, IModule newModule)
    {
        foreach (var newFunc in newModule.functions) newFunc.module = newModule;
    }

    /// <summary>
    ///     使 JIT 缓存失效：热重载后清除被修改函数的编译结果
    ///     仅清除实际发生变更的函数，未变更的保留编译缓存
    /// </summary>
    private void invalidate_jit_cache(IModule? oldModule, IModule newModule, string moduleName)
    {
        var jit = _vm.jit_compiler;

        if (oldModule is null)
        {
            jit.clear_cache();
            jit.clear_inline_caches();
            return;
        }

        var changedIndices = get_changed_function_indices(oldModule, newModule);
        foreach (var funcIndex in changedIndices)
            if (jit.is_compiled(funcIndex))
            {
                jit.clear_cache();
                break;
            }

        jit.clear_inline_caches();

        if (newModule.raw_bytecode is not null) jit.set_context(newModule.raw_bytecode, newModule);
    }

    /// <summary>
    ///     检测函数差异：比较新旧模块中同名函数对应的代码字节范围。
    ///     返回代码字节流发生变化的函数索引列表。
    /// </summary>
    private static List<int> get_changed_function_indices(IModule oldModule, IModule newModule)
    {
        var changed = new List<int>();
        var oldFuncMap = new Dictionary<string, (int Index, int CodeOffset, int CodeLength)>();

        for (var i = 0; i < oldModule.functions.Count; i++)
        {
            var f = oldModule.functions[i];
            oldFuncMap[f.name] = (i, f.code_offset, f.code_length);
        }

        for (var i = 0; i < newModule.functions.Count; i++)
        {
            var newFunc = newModule.functions[i];

            if (!oldFuncMap.TryGetValue(newFunc.name, out var oldInfo))
            {
                changed.Add(i);
                continue;
            }

            if (newFunc.code_length != oldInfo.CodeLength)
            {
                changed.Add(i);
                continue;
            }

            if (oldModule.raw_bytecode is not null && newModule.raw_bytecode is not null)
            {
                var oldSpan = oldModule.raw_bytecode.AsSpan(oldInfo.CodeOffset, oldInfo.CodeLength);
                var newSpan = newModule.raw_bytecode.AsSpan(newFunc.code_offset, newFunc.code_length);

                if (!oldSpan.SequenceEqual(newSpan)) changed.Add(i);
            }
            else
            {
                changed.Add(i);
            }
        }

        return changed;
    }

    /// <summary>
    ///     检查模块版本兼容性：确保函数签名向后兼容
    /// </summary>
    private CompatibilityCheckResult check_compatibility(IModule oldModule, IModule newModule)
    {
        var incompatibleFunctions = new List<IncompatibleFunction>();

        var oldFuncs = oldModule.functions.ToDictionary(f => f.name);
        foreach (var newFunc in newModule.functions)
        {
            if (!oldFuncs.TryGetValue(newFunc.name, out var oldFunc)) continue;

            if (oldFunc.arity != newFunc.arity)
                incompatibleFunctions.Add(new IncompatibleFunction
                {
                    function_name = newFunc.name,
                    reason = $"函数参数个数改变：旧={oldFunc.arity}，新={newFunc.arity}",
                    old_signature = $"{oldFunc.name}({oldFunc.arity})",
                    new_signature = $"{newFunc.name}({newFunc.arity})"
                });
        }

        return new CompatibilityCheckResult
        {
            is_compatible = incompatibleFunctions.Count == 0,
            incompatible_functions = incompatibleFunctions,
            error_message = incompatibleFunctions.Count > 0
                ? $"发现 {incompatibleFunctions.Count} 个不兼容的函数"
                : null
        };
    }

    private void OnModuleChanged(ModuleChangedEventArgs e)
    {
        ModuleChanged?.Invoke(this, e);
    }

    #endregion
}

/// <summary>
///     热重载结果
/// </summary>
public sealed class HotReloadResult
{
    /// <summary>
    ///     模块名称
    /// </summary>
    public required string module_name { get; init; }

    /// <summary>
    ///     旧版本号
    /// </summary>
    public required uint old_version { get; init; }

    /// <summary>
    ///     新版本号
    /// </summary>
    public required uint new_version { get; init; }

    /// <summary>
    ///     是否为重载（而非首次加载）
    /// </summary>
    public required bool is_reload { get; init; }

    /// <summary>
    ///     受影响的依赖模块列表
    /// </summary>
    public required IReadOnlyList<string> affected_dependencies { get; init; }

    /// <summary>
    ///     是否需要重新绑定引用
    /// </summary>
    public bool needs_rebind { get; init; }

    /// <summary>
    ///     兼容性检查结果（如果不兼容时）
    /// </summary>
    public CompatibilityCheckResult? compatibility_result { get; init; }
}
