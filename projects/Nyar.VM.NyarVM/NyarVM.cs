using Nyar.Analyzer.ModuleSystem;
using Nyar.Types;
using Nyar.VM.NyarVM.Bytecode;
using Nyar.VM.NyarVM.Debugging;
using Nyar.VM.NyarVM.GC;
using Nyar.VM.NyarVM.HotReload;
using Nyar.VM.NyarVM.Jit;
using Nyar.VM.NyarVM.Lazy;
using Nyar.VM.NyarVM.Observability;
using Nyar.VM.NyarVM.Runtime;
using Std.Data.Binary.NyarIR.Scanner;

namespace Nyar.VM.NyarVM;

/// <summary>
///     Nyar 虚拟机核心，集成执行器、GC、JIT 编译器和模块热加的
/// </summary>
public sealed class NyarVm
{
    #region 字段

    private readonly Dictionary<string, IModule> _modules = new();
    private readonly Dictionary<string, byte[]> _module_code_bytes = new();
    private readonly List<Action<EffectRuntime>> _effect_runtime_initializers = [];
    private readonly ResourceLimits _resource_limits;
    private Executor? _current_executor;
    private IFunction? _current_function;

    #endregion

    #region 构造函的

    /// <summary>
    ///     初始的NyarVM（使用默的JIT 选项的
    /// </summary>
    public NyarVm() : this(new JitOptions())
    {
    }

    /// <summary>
    ///     初始的NyarVM（使用自定义 JIT 选项的
    /// </summary>
    /// <param name="jitOptions">JIT 配置选项。</param>
    /// <param name="resourceLimits">资源限制（可选，传入则启用运行时资源保护的/param>
    public NyarVm(JitOptions jitOptions, ResourceLimits? resourceLimits = null)
    {
        jit_compiler = new JitCompiler(jitOptions);
        gc = new NyarGc(Value.shared_object_table, Value.shared_table_lock);
        intrinsics = new Intrinsics();
        ffi = new Ffi();
        metrics = new VmMetrics();
        events = new VmEvents();
        _resource_limits = resourceLimits ?? ResourceLimits.@default;
        debugger = new NyarDebugger();
        debugger.set_vm(this);
        hot_reloader = new ModuleHotReloader(this, _modules, _module_code_bytes);

        jit_compiler.set_observability(metrics, events);
        gc.set_observability(metrics, events);
    }

    #endregion

    #region 模块加载

    /// <summary>
    ///     加载 NyarModule 模块
    /// </summary>
    /// <param name="module">要加载的模块。</param>
    public void load(NyarModule module)
    {
        ArgumentNullException.ThrowIfNull(module);

        if (module.raw_bytecode is null)
        {
            var encoded = NyarModuleConverter.encode(module);
            module.raw_bytecode = encoded;
        }

        _modules[module.name] = module;
        _module_code_bytes[module.name] = module.raw_bytecode;
        jit_compiler.set_context(module.raw_bytecode, module);
        jit_compiler.warmup();
    }

    /// <summary>
    ///     加载 IModule 模块
    /// </summary>
    /// <param name="module">要加载的模块。</param>
    public void load(IModule module)
    {
        ArgumentNullException.ThrowIfNull(module);
        _modules[module.name] = module;

        if (module.raw_bytecode is not null)
        {
            _module_code_bytes[module.name] = module.raw_bytecode;
            jit_compiler.set_context(module.raw_bytecode, module);
            jit_compiler.warmup();
        }
    }

    /// <summary>
    ///     从 `.nyar` 模块二进制加载模块。
    /// </summary>
    /// <param name="moduleBytes">`.nyar` 格式的模块二进制数据。</param>
    public void load(byte[] moduleBytes)
    {
        ArgumentNullException.ThrowIfNull(moduleBytes);

        var module = NyarModuleConverter.decode(moduleBytes);
        _modules[module.name] = module;
        _module_code_bytes[module.name] = module.raw_bytecode ?? moduleBytes;
        jit_compiler.set_context(module.raw_bytecode ?? moduleBytes, module);
        jit_compiler.warmup();
    }

    /// <summary>
    ///     从 `.nyar` 模块二进制惰性加载模块。
    ///     仅在首次访问时解码所需段数据，以减少大型模块的启动开销。
    /// </summary>
    /// <param name="moduleBytes">`.nyar` 格式的模块二进制数据。</param>
    /// <returns>惰性加载的模块实例。</returns>
    public LazyNyarModule load_lazy(byte[] moduleBytes)
    {
        ArgumentNullException.ThrowIfNull(moduleBytes);

        var scanner = new NyarScanner(moduleBytes);
        var scanHeader = scanner.scan_header();
        var lazyModule = new LazyNyarModule(moduleBytes, scanHeader);

        _modules[lazyModule.name] = lazyModule;
        _module_code_bytes[lazyModule.name] = moduleBytes;
        jit_compiler.set_context(moduleBytes, lazyModule);
        jit_compiler.warmup();

        return lazyModule;
    }

    #endregion

    #region 执行

    /// <summary>
    ///     执行指定模块的函的
    /// </summary>
    /// <param name="moduleName">模块名称。</param>
    /// <param name="functionName">函数名称。</param>
    /// <param name="args">函数参数。</param>
    /// <returns>函数返回的/returns>
    public Value run(string moduleName, string functionName, params Value[] args)
    {
        if (!_modules.TryGetValue(moduleName, out var module)) throw new NyarRuntimeException($"模块未找的 {moduleName}");

        var function = module.find_function(functionName)
                       ?? throw new NyarRuntimeException($"函数未找的 {moduleName}::{functionName}");

        if (!_module_code_bytes.TryGetValue(moduleName, out var codeBytes))
            throw new NyarRuntimeException($"模块代码字节流未找到: {moduleName}");

        var executor = create_executor(module, codeBytes);
        return executor.execute_function(function, args);
    }

    /// <summary>
    ///     调试执行：支持断点和单步模式，断点命中时暂停并返的
    /// </summary>
    /// <param name="moduleName">模块名称。</param>
    /// <param name="functionName">函数名称。</param>
    /// <param name="args">函数参数。</param>
    /// <returns>函数返回值（正常返回时）的<see cref="ControlResult.break" />（断点命中时通过事件通知的/returns>
    public Value run_debug(string moduleName, string functionName, params Value[] args)
    {
        if (!_modules.TryGetValue(moduleName, out var module)) throw new NyarRuntimeException($"模块未找的 {moduleName}");

        var function = module.find_function(functionName)
                       ?? throw new NyarRuntimeException($"函数未找的 {moduleName}::{functionName}");

        if (!_module_code_bytes.TryGetValue(moduleName, out var codeBytes))
            throw new NyarRuntimeException($"模块代码字节流未找到: {moduleName}");

        _current_executor = create_executor(module, codeBytes);
        _current_function = function;

        debugger.BreakpointHit += OnBreakpointHit;

        try
        {
            return _current_executor.execute_function(function, args);
        }
        finally
        {
            debugger.BreakpointHit -= OnBreakpointHit;
            _current_executor = null;
            _current_function = null;
        }
    }

    /// <summary>
    ///     在断点处继续调试执行（单步完成后恢复的
    /// </summary>
    /// <returns>函数返回值或 <see cref="ControlResult.break" />（再次遇到断点）</returns>
    public Value continue_debug()
    {
        if (_current_executor == null || _current_function == null)
            throw new NyarRuntimeException("没有正在调试的执行上下文，请先调的RunDebug");

        debugger.BreakpointHit += OnBreakpointHit;

        try
        {
            return _current_executor.continue_execution(_current_function);
        }
        finally
        {
            debugger.BreakpointHit -= OnBreakpointHit;
        }
    }

    /// <summary>
    ///     断点命中回调：触发调试器事件
    /// </summary>
    private void OnBreakpointHit(object? sender, DebuggerEventArgs e)
    {
    }

    /// <summary>
    ///     注册全局效应处理器，并自动应用到后续新建的执行器。
    /// </summary>
    /// <param name="effectName">效应名称。</param>
    /// <param name="handler">效应处理器。</param>
    public void register_effect_handler(string effectName, EffectHandlerDelegate handler)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(effectName);
        ArgumentNullException.ThrowIfNull(handler);

        void apply(EffectRuntime runtime)
        {
            runtime.register_handler(effectName, handler);
        }

        _effect_runtime_initializers.Add(apply);
        _current_executor?.effect_runtime.register_handler(effectName, handler);
    }

    /// <summary>
    ///     注册生成器 `Yielder` 宿主处理器，并自动应用到后续新建的执行器。
    /// </summary>
    /// <param name="onYield">收到 `yield` 值时的回调。</param>
    /// <param name="onComplete">收到 `yield break` 时的回调。</param>
    public void register_yielder_handlers(Action<Value> onYield, Action onComplete)
    {
        ArgumentNullException.ThrowIfNull(onYield);
        ArgumentNullException.ThrowIfNull(onComplete);

        void apply(EffectRuntime runtime)
        {
            runtime.register_yielder_handlers(onYield, onComplete);
        }

        _effect_runtime_initializers.Add(apply);
        _current_executor?.effect_runtime.register_yielder_handlers(onYield, onComplete);
    }

    /// <summary>
    ///     获取帧栈快照（用于调试器变量查看的
    /// </summary>
    /// <returns>帧栈快照列表。</returns>
    public List<FrameSnapshot> get_frame_snapshots()
    {
        if (_current_executor == null) return [];

        return _current_executor.get_frame_snapshots();
    }

    private Executor create_executor(IModule module, byte[] codeBytes)
    {
        var executor = new Executor(module, codeBytes, jit_compiler, gc, intrinsics, ffi, metrics, events,
            _resource_limits);
        foreach (var initialize in _effect_runtime_initializers)
        {
            initialize(executor.effect_runtime);
        }

        return executor;
    }

    #endregion

    #region 模块管理

    /// <summary>
    ///     检查模块是否已加载
    /// </summary>
    /// <param name="moduleName">模块名称。</param>
    /// <returns>模块是否已加的/returns>
    public bool has_module(string moduleName)
    {
        return _modules.ContainsKey(moduleName);
    }

    /// <summary>
    ///     获取指定模块
    /// </summary>
    /// <param name="moduleName">模块名称。</param>
    /// <returns>模块实例，未找到返回 null。</returns>
    public IModule? get_module(string moduleName)
    {
        return _modules.GetValueOrDefault(moduleName);
    }

    /// <summary>
    ///     卸载模块（通过热加载管理器的
    /// </summary>
    /// <param name="moduleName">模块名称。</param>
    /// <returns>是否成功卸载。</returns>
    public bool unload_module(string moduleName)
    {
        return hot_reloader.unload_module(moduleName);
    }

    #endregion

    #region 属的

    /// <summary>
    ///     获取 JIT 编译的
    /// </summary>
    public JitCompiler jit_compiler { get; }

    /// <summary>
    ///     启用 JIT 编译结果持久化缓的
    /// </summary>
    /// <param name="cache">JIT 缓存实现。</param>
    public void enable_jit_cache(IJitCache cache)
    {
        jit_compiler.set_cache(cache);
    }

    /// <summary>
    ///     获取精确非移的GC
    /// </summary>
    public NyarGc gc { get; }

    /// <summary>
    ///     获取内置函数注册表（基于模块系统的原生函数查找与调用的
    /// </summary>
    public Intrinsics intrinsics { get; }

    /// <summary>
    ///     获取外部函数接口（原生共享库加载和调用）
    /// </summary>
    public Ffi ffi { get; }

    /// <summary>
    ///     获取运行时统一指标收集的
    /// </summary>
    public VmMetrics metrics { get; }

    /// <summary>
    ///     获取运行时事件系的
    /// </summary>
    public VmEvents events { get; }

    /// <summary>
    ///     获取调试器（断点 + 单步执行的
    /// </summary>
    public NyarDebugger debugger { get; }

    /// <summary>
    ///     获取模块热加载管理器（原子替的+ 依赖跟踪 + 状态迁移）
    /// </summary>
    public ModuleHotReloader hot_reloader { get; }

    /// <summary>
    ///     失效指定调用点的内联缓存
    ///     适用于运行时类型结构变化后需要重新分派的场景
    /// </summary>
    /// <param name="cacheSlot">
    ///     缓存槽索的/param>
    ///     <returns>是否成功失效。</returns>
    public bool invalidate_inline_cache(int cacheSlot)
    {
        return jit_compiler.invalidate_inline_cache(cacheSlot);
    }

    /// <summary>
    ///     失效指定调用点中特定类型的缓存条的
    /// </summary>
    /// <param name="cacheSlot">
    ///     缓存槽索的/param>
    ///     <param name="typeTag">
    ///         接收者类型标的/param>
    ///         <returns>是否成功移除。</returns>
    public bool invalidate_inline_cache_type(int cacheSlot, byte typeTag)
    {
        return jit_compiler.invalidate_inline_cache_type(cacheSlot, typeTag);
    }

    #endregion
}
