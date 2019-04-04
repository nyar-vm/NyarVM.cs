namespace Nyar.VM.NyarVM.Observability;

/// <summary>
///     VM 运行时事件系统，提供 JIT/GC/OSR/FFI 事件通知
/// </summary>
public sealed class VmEvents
{
    #region JIT 事件

    /// <summary>
    ///     JIT 编译开始事件
    /// </summary>
    public event EventHandler<JitCompilationEventArgs>? JitCompilationStarted;

    /// <summary>
    ///     JIT 编译完成事件
    /// </summary>
    public event EventHandler<JitCompilationEventArgs>? JitCompilationCompleted;

    /// <summary>
    ///     触发 JIT 编译开始事件
    /// </summary>
    /// <param name="functionIndex">函数索引。</param>
    /// <param name="functionName">函数名。</param>
    public void OnJitCompilationStarted(int functionIndex, string functionName)
    {
        JitCompilationStarted?.Invoke(this, new JitCompilationEventArgs(functionIndex, functionName));
    }

    /// <summary>
    ///     触发 JIT 编译完成事件
    /// </summary>
    /// <param name="functionIndex">函数索引。</param>
    /// <param name="functionName">函数名。</param>
    /// <param name="durationMs">编译耗时（毫秒）。</param>
    public void OnJitCompilationCompleted(int functionIndex, string functionName, double durationMs)
    {
        JitCompilationCompleted?.Invoke(this, new JitCompilationEventArgs(functionIndex, functionName, durationMs));
    }

    #endregion

    #region GC 事件

    /// <summary>
    ///     GC 回收开始事件
    /// </summary>
    public event EventHandler<GcCollectionEventArgs>? GcCollectionStarted;

    /// <summary>
    ///     GC 回收完成事件
    /// </summary>
    public event EventHandler<GcCollectionEventArgs>? GcCollectionCompleted;

    /// <summary>
    ///     触发 GC 回收开始事件
    /// </summary>
    /// <param name="isMajor">是否为 Major GC。</param>
    public void OnGcCollectionStarted(bool isMajor)
    {
        GcCollectionStarted?.Invoke(this, new GcCollectionEventArgs(isMajor));
    }

    /// <summary>
    ///     触发 GC 回收完成事件
    /// </summary>
    /// <param name="isMajor">是否为 Major GC。</param>
    /// <param name="pauseMs">暂停时间（毫秒）。</param>
    /// <param name="liveCount">存活对象数。</param>
    /// <param name="reclaimed">回收对象数。</param>
    /// <param name="promoted">晋升对象数（仅 Minor GC）。</param>
    public void OnGcCollectionCompleted(bool isMajor, double pauseMs, int liveCount, long reclaimed, long promoted = 0)
    {
        GcCollectionCompleted?.Invoke(this,
            new GcCollectionEventArgs(isMajor, pauseMs, liveCount, reclaimed, promoted));
    }

    #endregion

    #region OSR 事件

    /// <summary>
    ///     OSR 编译完成事件
    /// </summary>
    public event EventHandler<OsrEventArgs>? OsrCompilationCompleted;

    /// <summary>
    ///     OSR 迁移事件
    /// </summary>
    public event EventHandler<OsrEventArgs>? OsrTransition;

    /// <summary>
    ///     触发 OSR 编译完成事件
    /// </summary>
    /// <param name="functionIndex">函数索引。</param>
    /// <param name="bytecodePc">字节码 PC。</param>
    public void OnOsrCompilationCompleted(int functionIndex, int bytecodePc)
    {
        OsrCompilationCompleted?.Invoke(this, new OsrEventArgs(functionIndex, bytecodePc));
    }

    /// <summary>
    ///     触发 OSR 迁移事件
    /// </summary>
    /// <param name="functionIndex">函数索引。</param>
    /// <param name="bytecodePc">字节码 PC。</param>
    public void OnOsrTransition(int functionIndex, int bytecodePc)
    {
        OsrTransition?.Invoke(this, new OsrEventArgs(functionIndex, bytecodePc));
    }

    #endregion

    #region FFI 事件

    /// <summary>
    ///     Intrinsic 调用事件
    /// </summary>
    public event EventHandler<FfiCallEventArgs>? IntrinsicCalled;

    /// <summary>
    ///     Native 调用事件
    /// </summary>
    public event EventHandler<FfiCallEventArgs>? NativeCalled;

    /// <summary>
    ///     触发 Intrinsic 调用事件
    /// </summary>
    /// <param name="functionName">函数名。</param>
    /// <param name="argCount">参数数量。</param>
    public void OnIntrinsicCalled(string functionName, int argCount)
    {
        IntrinsicCalled?.Invoke(this, new FfiCallEventArgs(functionName, argCount));
    }

    /// <summary>
    ///     触发 Native 调用事件
    /// </summary>
    /// <param name="functionName">函数名。</param>
    /// <param name="argCount">参数数量。</param>
    public void OnNativeCalled(string functionName, int argCount)
    {
        NativeCalled?.Invoke(this, new FfiCallEventArgs(functionName, argCount));
    }

    #endregion
}

/// <summary>
///     JIT 编译事件参数
/// </summary>
public sealed class JitCompilationEventArgs : EventArgs
{
    /// <summary>
    ///     初始化 JIT 编译事件参数
    /// </summary>
    public JitCompilationEventArgs(int functionIndex, string functionName, double durationMs = 0)
    {
        function_index = functionIndex;
        function_name = functionName;
        duration_ms = durationMs;
    }

    /// <summary>
    ///     函数索引
    /// </summary>
    public int function_index { get; }

    /// <summary>
    ///     函数名
    /// </summary>
    public string function_name { get; }

    /// <summary>
    ///     编译耗时（毫秒），仅在 Completed 事件中有值
    /// </summary>
    public double duration_ms { get; }
}

/// <summary>
///     GC 回收事件参数
/// </summary>
public sealed class GcCollectionEventArgs : EventArgs
{
    /// <summary>
    ///     初始化 GC 回收事件参数
    /// </summary>
    public GcCollectionEventArgs(bool isMajor, double pauseMs = 0, int liveCount = 0, long reclaimed = 0,
        long promoted = 0)
    {
        is_major = isMajor;
        pause_ms = pauseMs;
        live_count = liveCount;
        this.reclaimed = reclaimed;
        this.promoted = promoted;
    }

    /// <summary>
    ///     是否为 Major GC
    /// </summary>
    public bool is_major { get; }

    /// <summary>
    ///     暂停时间（毫秒），仅在 Completed 事件中有值
    /// </summary>
    public double pause_ms { get; }

    /// <summary>
    ///     存活对象数
    /// </summary>
    public int live_count { get; }

    /// <summary>
    ///     回收对象数
    /// </summary>
    public long reclaimed { get; }

    /// <summary>
    ///     晋升对象数（仅 Minor GC）
    /// </summary>
    public long promoted { get; }
}

/// <summary>
///     OSR 事件参数
/// </summary>
public sealed class OsrEventArgs : EventArgs
{
    /// <summary>
    ///     初始化 OSR 事件参数
    /// </summary>
    public OsrEventArgs(int functionIndex, int bytecodePc)
    {
        function_index = functionIndex;
        bytecode_pc = bytecodePc;
    }

    /// <summary>
    ///     函数索引
    /// </summary>
    public int function_index { get; }

    /// <summary>
    ///     字节码 PC
    /// </summary>
    public int bytecode_pc { get; }
}

/// <summary>
///     FFI 调用事件参数
/// </summary>
public sealed class FfiCallEventArgs : EventArgs
{
    /// <summary>
    ///     初始化 FFI 调用事件参数
    /// </summary>
    public FfiCallEventArgs(string functionName, int argCount)
    {
        function_name = functionName;
        arg_count = argCount;
    }

    /// <summary>
    ///     函数名
    /// </summary>
    public string function_name { get; }

    /// <summary>
    ///     参数数量
    /// </summary>
    public int arg_count { get; }
}