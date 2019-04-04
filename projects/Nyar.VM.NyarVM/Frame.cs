using Nyar.Analyzer.ModuleSystem;
using Nyar.Types;

namespace Nyar.VM.NyarVM;

/// <summary>
///     函数调用帧，管理局部变量、参数和返回地址
///     支持尾调用优化（TCO）时的帧复用
/// </summary>
public sealed class Frame
{
    /// <summary>
    ///     Function 的内部可写字段（TCO 帧复用）
    /// </summary>
    internal IFunction _function;

    /// <summary>
    ///     局部变量数组
    /// </summary>
    private Value[] _locals;

    /// <summary>
    ///     ReturnPc 的内部可写字段（TCO 帧复用）
    /// </summary>
    internal int _return_pc;

    /// <summary>
    ///     StackBase 的内部可写字段（TCO 帧复用）
    /// </summary>
    internal int _stack_base;

    /// <summary>
    ///     初始化 Frame
    /// </summary>
    /// <param name="function">执行的函数。</param>
    /// <param name="returnPc">返回地址。</param>
    /// <param name="stackBase">栈基指针。</param>
    public Frame(IFunction function, int returnPc, int stackBase)
    {
        _function = function;
        _return_pc = returnPc;
        _stack_base = stackBase;
        pc = function.code_offset;
        _locals = new Value[function.arity + function.local_count];
    }

    /// <summary>
    ///     当前执行的函数
    /// </summary>
    public IFunction function => _function;

    /// <summary>
    ///     返回地址（调用者的程序计数器）
    /// </summary>
    public int return_pc => _return_pc;

    /// <summary>
    ///     栈基指针（调用者的栈顶位置，用于恢复栈）
    /// </summary>
    public int stack_base => _stack_base;

    /// <summary>
    ///     当前程序计数器
    /// </summary>
    public int pc { get; set; }

    /// <summary>
    ///     局部变量数量
    /// </summary>
    public int local_count => _locals.Length;

    /// <summary>
    ///     获取局部变量数组的只读引用（用于 GC Root 扫描）
    /// </summary>
    public ReadOnlySpan<Value> locals => _locals;

    /// <summary>
    ///     获取局部变量值
    /// </summary>
    /// <param name="index">局部变量索引。</param>
    /// <returns>局部变量值。</returns>
    public Value get_local(int index)
    {
        if (index < 0 || index >= _locals.Length) throw new ArgumentOutOfRangeException(nameof(index));

        return _locals[index];
    }

    /// <summary>
    ///     设置局部变量值
    /// </summary>
    /// <param name="index">局部变量索引。</param>
    /// <param name="value">要设置的值。</param>
    public void set_local(int index, Value value)
    {
        if (index < 0 || index >= _locals.Length) throw new ArgumentOutOfRangeException(nameof(index));

        _locals[index] = value;
    }

    /// <summary>
    ///     从参数列表初始化局部变量
    /// </summary>
    /// <param name="args">参数值数组。</param>
    public void set_arguments(ReadOnlySpan<Value> args)
    {
        var count = args.Length < _function.arity ? args.Length : _function.arity;
        for (var i = 0; i < count; i++) _locals[i] = args[i];
    }

    /// <summary>
    ///     尾调用优化：复用自己的帧，更新目标函数和栈基
    ///     仅由 Executor.HandleTailCall 的内部路径调用
    ///     所有局部变量清零以确保安全
    /// </summary>
    /// <param name="targetFunc">尾调用目标函数。</param>
    /// <param name="newStackBase">新的栈基指针。</param>
    internal void reset(IFunction targetFunc, int newStackBase)
    {
        _function = targetFunc;
        _return_pc = return_pc;
        _stack_base = newStackBase;
        pc = targetFunc.code_offset;

        var newSize = targetFunc.arity + targetFunc.local_count;
        if (_locals.Length < newSize)
            _locals = new Value[newSize];
        else
            _locals.AsSpan().Clear();
    }
}