using Nyar.Analyzer.ModuleSystem;
using Nyar.Types;
using Nyar.VM.NyarVM.Bytecode;
using Nyar.VM.NyarVM.HotReload;

namespace Nyar.VM.NyarVM;

/// <summary>
///     Nyar 标准虚拟机，基于 Standard 方言的参考实现
/// </summary>
[Obsolete("NyarStandardVM 和 NyarVM 接下来不做区分")]
public sealed class NyarStandardVm
{
    private readonly NyarVm _inner;

    /// <summary>
    ///     初始化 NyarStandardVM
    /// </summary>
    public NyarStandardVm()
    {
        _inner = new NyarVm();
    }

    /// <summary>
    ///     获取模块热加载管理器
    /// </summary>
    public ModuleHotReloader hot_reloader => _inner.hot_reloader;

    /// <summary>
    ///     加载模块
    /// </summary>
    /// <param name="module">要加载的模块。</param>
    public void load_module(NyarModule module)
    {
        ArgumentNullException.ThrowIfNull(module);

        if (module.raw_bytecode is null)
        {
            var encoded = NyarModuleConverter.encode(module);
            module.raw_bytecode = encoded;
        }

        _inner.load(module);
    }

    /// <summary>
    ///     加载字节码
    /// </summary>
    /// <param name="bytecode">字节码数据。</param>
    public void load_bytecode(byte[] bytecode)
    {
        _inner.load(bytecode);
    }

    /// <summary>
    ///     执行函数
    /// </summary>
    /// <param name="moduleName">模块名称。</param>
    /// <param name="functionName">函数名称。</param>
    /// <param name="args">函数参数。</param>
    /// <returns>函数返回值。</returns>
    public Value run(string moduleName, string functionName, params Value[] args)
    {
        return _inner.run(moduleName, functionName, args);
    }

    /// <summary>
    ///     检查模块是否已加载
    /// </summary>
    /// <param name="moduleName">模块名称。</param>
    /// <returns>模块是否已加载。</returns>
    public bool has_module(string moduleName)
    {
        return _inner.has_module(moduleName);
    }

    /// <summary>
    ///     卸载模块（通过热加载管理器，自动更新依赖图和触发事件）
    /// </summary>
    /// <param name="moduleName">模块名称。</param>
    /// <returns>是否成功卸载。</returns>
    public bool unload_module(string moduleName)
    {
        return _inner.unload_module(moduleName);
    }

    /// <summary>
    ///     热重载模块：原子性地替换同名模块
    /// </summary>
    /// <param name="newModule">新模块。</param>
    /// <returns>重载结果。</returns>
    public HotReloadResult reload_module(NyarModule newModule)
    {
        ArgumentNullException.ThrowIfNull(newModule);

        if (newModule.raw_bytecode is null)
        {
            var encoded = NyarModuleConverter.encode(newModule);
            newModule.raw_bytecode = encoded;
        }

        return _inner.hot_reloader.reload_module(newModule);
    }

    /// <summary>
    ///     级联重载：重载指定模块及其所有传递依赖方
    /// </summary>
    /// <param name="newModules">新模块列表。</param>
    /// <returns>级联重载结果列表。</returns>
    public IReadOnlyList<HotReloadResult> cascade_reload(IReadOnlyList<NyarModule> newModules)
    {
        var modules = newModules as IList<NyarModule> ?? [.. newModules];

        foreach (var module in modules)
            if (module.raw_bytecode is null)
            {
                var encoded = NyarModuleConverter.encode(module);
                module.raw_bytecode = encoded;
            }

        return _inner.hot_reloader.cascade_reload([.. modules.Cast<IModule>()]);
    }

    /// <summary>
    ///     模块变更事件
    /// </summary>
    public event EventHandler<ModuleChangedEventArgs>? ModuleChanged
    {
        add => _inner.hot_reloader.ModuleChanged += value;
        remove => _inner.hot_reloader.ModuleChanged -= value;
    }
}