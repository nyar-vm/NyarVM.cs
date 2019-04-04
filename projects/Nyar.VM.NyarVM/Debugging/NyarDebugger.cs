using Nyar.Types;

namespace Nyar.VM.NyarVM.Debugging;

/// <summary>
///     单步执行模式
/// </summary>
public enum StepMode
{
    /// <summary>
    ///     不单步
    /// </summary>
    none,

    /// <summary>
    ///     单步进入：遇到函数调用也进入
    /// </summary>
    into,

    /// <summary>
    ///     单步跳过：跳过函数调用，执行完子函数后停在下一行
    /// </summary>
    over,

    /// <summary>
    ///     单步退出：执行完当前函数调用后停在返回处
    /// </summary>
    @out
}

/// <summary>
///     单步状态：使用深度计数器跟踪 Step Over 和 Step Out 的嵌套层级
/// </summary>
public struct StepState
{
    /// <summary>
    ///     当前单步模式
    /// </summary>
    public StepMode mode;

    /// <summary>
    ///     Step Over 模式下的目标栈深度（调用时的栈深度）
    /// </summary>
    public int target_depth;

    /// <summary>
    ///     Step Out 模式的嵌套层级计数器：进入子函数 +1，返回 -1，计数器回到 0 时中断
    /// </summary>
    public int out_depth;
}

/// <summary>
///     断点信息
/// </summary>
public class BreakpointInfo
{
    /// <summary>
    ///     初始化 BreakpointInfo
    /// </summary>
    /// <param name="functionId">函数标识符。</param>
    /// <param name="pc">字节码偏移。</param>
    public BreakpointInfo(string functionId, int pc)
    {
        function_id = functionId;
        this.pc = pc;
        is_enabled = true;
        hit_count = 0;
        condition = null;
    }

    /// <summary>
    ///     函数标识符
    /// </summary>
    public string function_id { get; }

    /// <summary>
    ///     字节码偏移
    /// </summary>
    public int pc { get; }

    /// <summary>
    ///     是否启用
    /// </summary>
    public bool is_enabled { get; set; }

    /// <summary>
    ///     命中次数
    /// </summary>
    public long hit_count { get; set; }

    /// <summary>
    ///     条件表达式（可选），返回 true 时中断
    /// </summary>
    public Func<NyarVm, bool>? condition { get; set; }
}

/// <summary>
///     调试事件参数
/// </summary>
public class DebuggerEventArgs : EventArgs
{
    /// <summary>
    ///     初始化 DebuggerEventArgs
    /// </summary>
    /// <param name="vm">虚拟机实例。</param>
    /// <param name="functionId">当前函数标识。</param>
    /// <param name="pc">当前程序计数器。</param>
    public DebuggerEventArgs(NyarVm vm, string functionId, int pc)
    {
        this.vm = vm;
        function_id = functionId;
        this.pc = pc;
    }

    /// <summary>
    ///     虚拟机实例
    /// </summary>
    public NyarVm vm { get; }

    /// <summary>
    ///     当前函数标识符
    /// </summary>
    public string function_id { get; }

    /// <summary>
    ///     当前程序计数器
    /// </summary>
    public int pc { get; }

    /// <summary>
    ///     是否暂停执行（可由回调设置为 false 跳过此次中断）
    /// </summary>
    public bool should_break { get; set; } = true;
}

/// <summary>
///     NyarVM 调试器：断点管理 + 单步执行支持
/// </summary>
public sealed class NyarDebugger
{
    #region 构造函数

    /// <summary>
    ///     初始化 NyarDebugger
    /// </summary>
    public NyarDebugger()
    {
        _breakpoints = new Dictionary<string, Dictionary<int, BreakpointInfo>>();
        _step_state = new StepState
        {
            mode = StepMode.none
        };
        _current_vm = null;
        suppress_jit = false;
    }

    #endregion

    #region 调试器状态

    /// <summary>
    ///     调试器是否已附加（有断点或处于单步模式）
    ///     当 IsAttached 为 true 时，JIT 编译器应跳过编译，回退解释执行以支持调试
    /// </summary>
    public bool is_attached => _breakpoints.Count > 0 || _step_state.mode != StepMode.none || suppress_jit;

    /// <summary>
    ///     是否抑制 JIT 编译（调试模式下强制解释执行）
    ///     设置为 true 时，所有函数都通过解释器执行，确保断点和单步功能可用
    /// </summary>
    public bool suppress_jit { get; set; }

    #endregion

    #region 断点管理

    /// <summary>
    ///     设置软件断点
    /// </summary>
    /// <param name="functionId">函数标识符。</param>
    /// <param name="pc">字节码偏移。</param>
    /// <returns>断点信息。</returns>
    public BreakpointInfo set_breakpoint(string functionId, int pc)
    {
        if (!_breakpoints.TryGetValue(functionId, out var pcs))
        {
            pcs = new Dictionary<int, BreakpointInfo>();
            _breakpoints[functionId] = pcs;
        }

        if (!pcs.TryGetValue(pc, out var bp))
        {
            bp = new BreakpointInfo(functionId, pc);
            pcs[pc] = bp;
        }

        return bp;
    }

    /// <summary>
    ///     移除断点
    /// </summary>
    /// <param name="functionId">函数标识符。</param>
    /// <param name="pc">字节码偏移。</param>
    /// <returns>是否移除了断点。</returns>
    public bool remove_breakpoint(string functionId, int pc)
    {
        if (_breakpoints.TryGetValue(functionId, out var pcs)) return pcs.Remove(pc);

        return false;
    }

    /// <summary>
    ///     获取指定函数的所有断点
    /// </summary>
    /// <param name="functionId">函数标识符。</param>
    /// <returns>断点列表。</returns>
    public IEnumerable<BreakpointInfo> get_breakpoints(string functionId)
    {
        if (_breakpoints.TryGetValue(functionId, out var pcs)) return [.. pcs.Values];

        return [];
    }

    /// <summary>
    ///     获取所有断点
    /// </summary>
    /// <returns>所有断点列表。</returns>
    public IEnumerable<BreakpointInfo> get_all_breakpoints()
    {
        return _breakpoints.Values.SelectMany(pcs => pcs.Values);
    }

    /// <summary>
    ///     清除所有断点
    /// </summary>
    public void clear_all_breakpoints()
    {
        _breakpoints.Clear();
    }

    #endregion

    #region 单步控制

    /// <summary>
    ///     开始单步进入
    /// </summary>
    public void step_into()
    {
        _step_state = new StepState
        {
            mode = StepMode.into
        };
    }

    /// <summary>
    ///     开始单步跳过
    /// </summary>
    /// <param name="currentStackDepth">调用发生时的栈深度。</param>
    public void step_over(int currentStackDepth)
    {
        _step_state = new StepState
        {
            mode = StepMode.over,
            target_depth = currentStackDepth
        };
    }

    /// <summary>
    ///     开始单步退出
    /// </summary>
    public void step_out()
    {
        _step_state = new StepState
        {
            mode = StepMode.@out,
            out_depth = 1
        };
    }

    /// <summary>
    ///     停止单步
    /// </summary>
    public void clear_step()
    {
        _step_state = new StepState
        {
            mode = StepMode.none
        };
    }

    /// <summary>
    ///     当前是否处于单步模式
    /// </summary>
    public bool is_stepping => _step_state.mode != StepMode.none;

    /// <summary>
    ///     当前单步模式
    /// </summary>
    public StepMode current_step_mode => _step_state.mode;

    #endregion

    #region 调试事件

    /// <summary>
    ///     当断点或单步触发时触发
    /// </summary>
    public event EventHandler<DebuggerEventArgs>? BreakpointHit;

    /// <summary>
    ///     内部触发断点事件
    /// </summary>
    internal void OnBreakpointHit(NyarVm vm, string functionId, int pc)
    {
        BreakpointHit?.Invoke(this, new DebuggerEventArgs(vm, functionId, pc));
    }

    #endregion

    #region VM 集成

    /// <summary>
    ///     设置关联的 VM 实例
    /// </summary>
    internal void set_vm(NyarVm vm)
    {
        _current_vm = vm;
    }

    /// <summary>
    ///     获取当前调用栈的帧快照列表（用于调试器变量查看）
    /// </summary>
    /// <returns>帧快照列表，从栈顶（当前函数）到栈底（入口函数）。</returns>
    public List<FrameSnapshot>? get_frame_snapshots()
    {
        return _current_vm?.get_frame_snapshots();
    }

    /// <summary>
    ///     获取当前帧的局部变量值
    /// </summary>
    /// <param name="localIndex">局部变量索引。</param>
    /// <returns>局部变量值，调试器未附加或索引越界返回 null。</returns>
    public Value? get_local_value(int localIndex)
    {
        var snapshots = get_frame_snapshots();
        if (snapshots == null || snapshots.Count == 0) return null;

        var topFrame = snapshots[0];
        if (localIndex < 0 || localIndex >= topFrame.locals.Count) return null;

        return topFrame.locals[localIndex];
    }

    /// <summary>
    ///     获取当前帧的所有局部变量
    /// </summary>
    /// <returns>局部变量列表，调试器未附加返回空列表。</returns>
    public IReadOnlyList<Value> get_current_locals()
    {
        var snapshots = get_frame_snapshots();
        if (snapshots == null || snapshots.Count == 0) return [];

        return snapshots[0].locals;
    }

    /// <summary>
    ///     获取当前调用栈深度
    /// </summary>
    public int stack_depth
    {
        get
        {
            var snapshots = get_frame_snapshots();
            return snapshots?.Count ?? 0;
        }
    }

    #endregion

    #region 断点检测

    /// <summary>
    ///     检查指定位置是否应该中断
    /// </summary>
    /// <param name="functionId">函数标识符。</param>
    /// <param name="pc">字节码偏移。</param>
    /// <param name="stackDepth">当前帧栈深度。</param>
    /// <returns>是否应该中断。</returns>
    internal bool should_break_at(string functionId, int pc, int stackDepth)
    {
        if (is_breakpoint_hit(functionId, pc)) return true;

        return should_step(stackDepth);
    }

    /// <summary>
    ///     检查是否为断点并递增命中计数
    /// </summary>
    private bool is_breakpoint_hit(string functionId, int pc)
    {
        if (!_breakpoints.TryGetValue(functionId, out var pcs)) return false;

        if (!pcs.TryGetValue(pc, out var bp)) return false;

        if (!bp.is_enabled) return false;

        bp.hit_count++;

        if (bp.condition != null && _current_vm != null) return bp.condition(_current_vm);

        return true;
    }

    /// <summary>
    ///     检查单步是否应该触发
    /// </summary>
    private bool should_step(int stackDepth)
    {
        switch (_step_state.mode)
        {
            case StepMode.none:
            {
                return false;
            }

            case StepMode.into:
            {
                return true;
            }

            case StepMode.over:
            {
                return stackDepth <= _step_state.target_depth;
            }

            case StepMode.@out:
            {
                return false;
            }

            default:
            {
                return false;
            }
        }
    }

    /// <summary>
    ///     当执行 Call 指令时调用，更新嵌套计数器
    /// </summary>
    internal void OnCall(int stackDepth)
    {
        if (_step_state.mode == StepMode.@out)
            _step_state.out_depth++;
        else if (_step_state.mode == StepMode.over) _step_state.target_depth = stackDepth + 1;
    }

    /// <summary>
    ///     当执行 Return 指令时调用，更新嵌套计数器
    /// </summary>
    /// <returns>是否应该在返回后中断（Step Out 计数器回到 0）。</returns>
    internal bool OnReturn()
    {
        if (_step_state.mode == StepMode.@out)
        {
            _step_state.out_depth--;
            return _step_state.out_depth <= 0;
        }

        return false;
    }

    #endregion

    #region 字段

    private readonly Dictionary<string, Dictionary<int, BreakpointInfo>> _breakpoints;
    private StepState _step_state;
    private NyarVm? _current_vm;

    #endregion
}