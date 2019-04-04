using Nyar.Types;

namespace Nyar.VM.NyarVM.Runtime;

/// <summary>
///     代数效应处理器委托
/// </summary>
/// <param name="effectName">效应名称。</param>
/// <param name="payload">效应载荷。</param>
/// <param name="continuation">
///     当前续延（可能为 null，仅当效应在 handle 作用域内触发时存在）
///     调用 <c>continuation.Resume(value)</c> 将值传回 perform 点并继续执行
/// </param>
/// <returns>效应处理结果，若调用了 resume 则此返回值被忽略。</returns>
public delegate Value EffectHandlerDelegate(string effectName, Value payload, Continuation? continuation);

/// <summary>
///     代数效应运行时，管理效应处理器的注册和调度
/// </summary>
public sealed class EffectRuntime
{
    private readonly Dictionary<string, EffectHandlerDelegate> _handlers;
    private readonly Stack<EffectHandlerScope> _scope_stack;

    /// <summary>
    ///     创建代数效应运行时
    /// </summary>
    public EffectRuntime()
    {
        _handlers = new Dictionary<string, EffectHandlerDelegate>();
        _scope_stack = new Stack<EffectHandlerScope>();
    }

    /// <summary>
    ///     注册全局效应处理器
    /// </summary>
    /// <param name="effectName">效应名称。</param>
    /// <param name="handler">处理器委托。</param>
    public void register_handler(string effectName, EffectHandlerDelegate handler)
    {
        _handlers[effectName] = handler;
    }

    /// <summary>
    ///     注册生成器 `Yielder` 的默认宿主处理器。
    ///     `Yielder::Yield` 会提取 `value` 字段并回调，
    ///     `Yielder::YieldBreak` 会通知完成。
    /// </summary>
    /// <param name="onYield">收到 `yield` 值时的回调。</param>
    /// <param name="onComplete">收到 `yield break` 时的回调。</param>
    public void register_yielder_handlers(Action<Value> onYield, Action onComplete)
    {
        ArgumentNullException.ThrowIfNull(onYield);
        ArgumentNullException.ThrowIfNull(onComplete);

        register_handler("Yielder::Yield", (_, payload, _) =>
        {
            onYield(try_get_object_field(payload, "value", out var value) ? value : payload);
            return Value.@null;
        });
        register_handler("Yielder::YieldBreak", (_, _, _) =>
        {
            onComplete();
            return Value.@null;
        });
    }

    /// <summary>
    ///     推入效应处理作用域
    /// </summary>
    /// <param name="scope">效应处理作用域。</param>
    public void push_scope(EffectHandlerScope scope)
    {
        _scope_stack.Push(scope);
    }

    /// <summary>
    ///     弹出效应处理作用域
    /// </summary>
    /// <returns>弹出的作用域。</returns>
    public EffectHandlerScope? pop_scope()
    {
        return _scope_stack.Count > 0 ? _scope_stack.Pop() : null;
    }

    /// <summary>
    ///     执行效应（Perform），不携带续延（简单调用模式）
    /// </summary>
    /// <param name="effectName">效应名称。</param>
    /// <param name="payload">效应载荷。</param>
    /// <returns>效应处理结果。</returns>
    public Value perform(string effectName, Value payload)
    {
        return perform(effectName, payload, null);
    }

    /// <summary>
    ///     执行效应（Perform），携带续延（在 handle 作用域内时使用）
    /// </summary>
    /// <param name="effectName">效应名称。</param>
    /// <param name="payload">效应载荷。</param>
    /// <param name="continuation">当前续延（可能为 null）。</param>
    /// <returns>效应处理结果。</returns>
    /// <exception cref="InvalidOperationException">效应未找到处理器时抛出</exception>
    public Value perform(string effectName, Value payload, Continuation? continuation)
    {
        foreach (var scope in _scope_stack)
            if (scope.try_handle(effectName, payload, continuation, out var result))
                return result;

        if (_handlers.TryGetValue(effectName, out var globalHandler))
            return globalHandler(effectName, payload, continuation);

        throw new InvalidOperationException($"未处理的效应: {effectName}");
    }

    /// <summary>
    ///     检查效应是否有处理器
    /// </summary>
    /// <param name="effectName">效应名称。</param>
    /// <returns>是否有处理器。</returns>
    public bool has_handler(string effectName)
    {
        if (_handlers.ContainsKey(effectName)) return true;

        foreach (var scope in _scope_stack)
            if (scope.has_handler(effectName))
                return true;

        return false;
    }

    private static bool try_get_object_field(Value payload, string fieldName, out Value value)
    {
        if (payload.@object is Dictionary<string, Value> fields &&
            fields.TryGetValue(fieldName, out value))
            return true;

        value = Value.@null;
        return false;
    }
}

/// <summary>
///     效应处理作用域，支持局部效应处理器覆盖
/// </summary>
public sealed class EffectHandlerScope
{
    private readonly Dictionary<string, EffectHandlerDelegate> _handlers;

    /// <summary>
    ///     创建效应处理作用域
    /// </summary>
    public EffectHandlerScope()
    {
        _handlers = new Dictionary<string, EffectHandlerDelegate>();
    }

    /// <summary>
    ///     作用域关联的延续（用于 resume）
    /// </summary>
    public int handler_offset { get; set; }

    /// <summary>
    ///     注册作用域内的效应处理器
    /// </summary>
    /// <param name="effectName">效应名称。</param>
    /// <param name="handler">处理器委托。</param>
    public void register_handler(string effectName, EffectHandlerDelegate handler)
    {
        _handlers[effectName] = handler;
    }

    /// <summary>
    ///     尝试处理效应
    /// </summary>
    /// <param name="effectName">效应名称。</param>
    /// <param name="payload">效应载荷。</param>
    /// <param name="continuation">当前续延（可能为 null）。</param>
    /// <param name="result">处理结果。</param>
    /// <returns>是否成功处理。</returns>
    public bool try_handle(string effectName, Value payload, Continuation? continuation, out Value result)
    {
        if (_handlers.TryGetValue(effectName, out var handler))
        {
            result = handler(effectName, payload, continuation);
            return true;
        }

        result = Value.@null;
        return false;
    }

    /// <summary>
    ///     检查作用域内是否有指定效应的处理器
    /// </summary>
    /// <param name="effectName">效应名称。</param>
    /// <returns>是否有处理器。</returns>
    public bool has_handler(string effectName)
    {
        return _handlers.ContainsKey(effectName);
    }
}
