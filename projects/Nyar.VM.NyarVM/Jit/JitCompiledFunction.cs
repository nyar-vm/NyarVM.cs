using Nyar.Types;

namespace Nyar.VM.NyarVM.Jit;

/// <summary>
///     JIT 编译后的函数包装器
///     将字节码函数编译为可直接调用的委托
/// </summary>
public sealed class JitCompiledFunction
{
    #region 字段

    /// <summary>
    ///     JIT 编译后累计执行次数
    /// </summary>
    private long _jit_execution_count;

    #endregion

    #region 属性

    /// <summary>
    ///     原始函数名称
    /// </summary>
    public string function_name { get; }

    /// <summary>
    ///     函数索引（在模块函数表中的位置）
    /// </summary>
    public int function_index { get; }

    /// <summary>
    ///     编译后的委托
    ///     签名：Func&lt;Value[], Value&gt;（参数数组 → 返回值）
    /// </summary>
    public Func<Value[], Value> compiled_delegate { get; }

    /// <summary>
    ///     i32 专用委托（arity=1 的纯 i32 函数）
    ///     签名：Func&lt;int, int&gt;（直接传递 int，消除 NaN-Boxing 开销）
    ///     非 i32-arity1 函数为 null
    /// </summary>
    public Func<int, int>? int_int_delegate { get; }

    /// <summary>
    ///     f64 专用委托（arity=1 的纯 f64 函数）
    ///     签名：Func&lt;double, double&gt;（直接传递 double，消除 NaN-Boxing 开销）
    ///     非 f64-arity1 函数为 null
    /// </summary>
    public Func<double, double>? double_double_delegate { get; }

    /// <summary>
    ///     编译时间戳
    /// </summary>
    public DateTime compiled_at { get; }

    /// <summary>
    ///     编译耗时
    /// </summary>
    public TimeSpan compilation_duration { get; }

    /// <summary>
    ///     编译时的调用次数
    /// </summary>
    public long call_count_at_compilation { get; }

    /// <summary>
    ///     JIT 编译后累计执行次数
    /// </summary>
    public long jit_execution_count => Volatile.Read(ref _jit_execution_count);

    #endregion

    #region 构造函数

    /// <summary>
    ///     初始化 JitCompiledFunction（通用版本）
    /// </summary>
    public JitCompiledFunction(
        string functionName,
        int functionIndex,
        Func<Value[], Value> compiledDelegate,
        TimeSpan compilationDuration,
        long callCountAtCompilation)
    {
        function_name = functionName;
        function_index = functionIndex;
        compiled_delegate = compiledDelegate;
        int_int_delegate = null;
        compiled_at = DateTime.UtcNow;
        compilation_duration = compilationDuration;
        call_count_at_compilation = callCountAtCompilation;
    }

    /// <summary>
    ///     初始化 JitCompiledFunction（i32 专用版本）
    /// </summary>
    public JitCompiledFunction(
        string functionName,
        int functionIndex,
        Func<Value[], Value> compiledDelegate,
        Func<int, int> intIntDelegate,
        TimeSpan compilationDuration,
        long callCountAtCompilation)
    {
        function_name = functionName;
        function_index = functionIndex;
        compiled_delegate = compiledDelegate;
        int_int_delegate = intIntDelegate;
        double_double_delegate = null;
        compiled_at = DateTime.UtcNow;
        compilation_duration = compilationDuration;
        call_count_at_compilation = callCountAtCompilation;
    }

    /// <summary>
    ///     初始化 JitCompiledFunction（f64 专用版本）
    /// </summary>
    public JitCompiledFunction(
        string functionName,
        int functionIndex,
        Func<Value[], Value> compiledDelegate,
        Func<double, double> doubleDoubleDelegate,
        TimeSpan compilationDuration,
        long callCountAtCompilation)
    {
        function_name = functionName;
        function_index = functionIndex;
        compiled_delegate = compiledDelegate;
        int_int_delegate = null;
        double_double_delegate = doubleDoubleDelegate;
        compiled_at = DateTime.UtcNow;
        compilation_duration = compilationDuration;
        call_count_at_compilation = callCountAtCompilation;
    }

    /// <summary>
    ///     初始化 JitCompiledFunction（完整版本，含 i32 和 f64 专用委托）
    /// </summary>
    public JitCompiledFunction(
        string functionName,
        int functionIndex,
        Func<Value[], Value> compiledDelegate,
        Func<int, int>? intIntDelegate,
        Func<double, double>? doubleDoubleDelegate,
        TimeSpan compilationDuration,
        long callCountAtCompilation)
    {
        function_name = functionName;
        function_index = functionIndex;
        compiled_delegate = compiledDelegate;
        int_int_delegate = intIntDelegate;
        double_double_delegate = doubleDoubleDelegate;
        compiled_at = DateTime.UtcNow;
        compilation_duration = compilationDuration;
        call_count_at_compilation = callCountAtCompilation;
    }

    #endregion

    #region 执行

    /// <summary>
    ///     执行编译后的函数（Value 接口）
    /// </summary>
    /// <param name="args">函数参数。</param>
    /// <returns>函数返回值。</returns>
    public Value execute(Value[] args)
    {
        Interlocked.Increment(ref _jit_execution_count);
        return compiled_delegate(args);
    }

    /// <summary>
    ///     执行编译后的函数（i32 专用接口，消除 NaN-Boxing 开销）
    /// </summary>
    /// <param name="arg0">int 参数。</param>
    /// <returns>int 返回值。</returns>
    public int execute_int(int arg0)
    {
        Interlocked.Increment(ref _jit_execution_count);
        return int_int_delegate!(arg0);
    }

    /// <summary>
    ///     执行编译后的函数（i32 专用接口，无统计开销）
    ///     用于 JIT 内部递归调用路径，避免 Interlocked 开销
    /// </summary>
    /// <param name="arg0">int 参数。</param>
    /// <returns>int 返回值。</returns>
    public int execute_int_untracked(int arg0)
    {
        return int_int_delegate!(arg0);
    }

    /// <summary>
    ///     执行编译后的函数（f64 专用接口，消除 NaN-Boxing 开销）
    /// </summary>
    /// <param name="arg0">double 参数。</param>
    /// <returns>double 返回值。</returns>
    public double execute_double(double arg0)
    {
        Interlocked.Increment(ref _jit_execution_count);
        return double_double_delegate!(arg0);
    }

    /// <summary>
    ///     执行编译后的函数（f64 专用接口，无统计开销）
    ///     用于 JIT 内部递归调用路径，避免 Interlocked 开销
    /// </summary>
    /// <param name="arg0">double 参数。</param>
    /// <returns>double 返回值。</returns>
    public double execute_double_untracked(double arg0)
    {
        return double_double_delegate!(arg0);
    }

    #endregion
}