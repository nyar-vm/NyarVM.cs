using Nyar.Types;

namespace Nyar.VM.NyarVM.Runtime;

/// <summary>
///     内置函数注册表，基于模块系统的原生函数查找与调用
/// </summary>
public sealed class Intrinsics
{
    /// <summary>
    ///     已加载的模块（模块名 → NyarModule）
    /// </summary>
    private readonly Dictionary<string, NyarModule> _modules;

    /// <summary>
    ///     初始化空的 Intrinsics
    /// </summary>
    public Intrinsics()
    {
        _modules = new Dictionary<string, NyarModule>();
    }

    /// <summary>
    ///     注册模块及其原生函数
    /// </summary>
    /// <param name="module">要注册的模块。</param>
    public void register_module(NyarModule module)
    {
        _modules[module.name] = module;
    }

    /// <summary>
    ///     撤销注册模块
    /// </summary>
    /// <param name="moduleName">模块名称。</param>
    /// <returns>是否成功撤销。</returns>
    public bool unregister_module(string moduleName)
    {
        return _modules.Remove(moduleName);
    }

    /// <summary>
    ///     检查模块是否已注册
    /// </summary>
    /// <param name="moduleName">模块名称。</param>
    /// <returns>是否已注册。</returns>
    public bool has_module(string moduleName)
    {
        return _modules.ContainsKey(moduleName);
    }

    /// <summary>
    ///     查找原生函数（如 "io.println"、"math.sin"）
    /// </summary>
    /// <param name="fullName">函数名称。</param>
    /// <returns>函数实现，未找到返回 null。</returns>
    public Func<Value[], Value>? find(string fullName)
    {
        var dotIndex = fullName.LastIndexOf('.');
        if (dotIndex <= 0 || dotIndex >= fullName.Length - 1) return null;

        var moduleName = fullName[..dotIndex];
        var functionName = fullName[(dotIndex + 1)..];

        if (_modules.TryGetValue(moduleName, out var module))
        {
            var nativeFunc = module.find_native_function(functionName);
            return nativeFunc?.func;
        }

        return null;
    }

    /// <summary>
    ///     直接按模块名和函数名查找原生函数
    /// </summary>
    /// <param name="moduleName">模块名称。</param>
    /// <param name="functionName">函数名称。</param>
    /// <returns>函数实现，未找到返回 null。</returns>
    public Func<Value[], Value>? find(string moduleName, string functionName)
    {
        if (_modules.TryGetValue(moduleName, out var module))
        {
            var nativeFunc = module.find_native_function(functionName);
            return nativeFunc?.func;
        }

        return null;
    }

    /// <summary>
    ///     调用内置函数（如 "io.println"、"math.sin"）
    /// </summary>
    /// <param name="fullName">函数名称。</param>
    /// <param name="args">参数。</param>
    /// <returns>返回值。</returns>
    /// <exception cref="InvalidOperationException">函数未找到时抛出</exception>
    public Value call(string fullName, Value[] args)
    {
        var func = find(fullName);
        if (func == null) throw new InvalidOperationException($"内置函数 '{fullName}' 未找到。");

        return func(args);
    }

    /// <summary>
    ///     调用内置函数（模块名 + 函数名）
    /// </summary>
    /// <param name="moduleName">模块名称。</param>
    /// <param name="functionName">函数名称。</param>
    /// <param name="args">参数。</param>
    /// <returns>返回值。</returns>
    /// <exception cref="InvalidOperationException">函数未找到时抛出</exception>
    public Value call(string moduleName, string functionName, Value[] args)
    {
        var func = find(moduleName, functionName);
        if (func == null) throw new InvalidOperationException($"内置函数 '{moduleName}.{functionName}' 未找到。");

        return func(args);
    }
}