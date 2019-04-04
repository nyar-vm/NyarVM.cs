using System.Runtime.CompilerServices;
using Nyar.Analyzer.ModuleSystem;
using Nyar.Types;
using Nyar.VM.NyarVM.Debugging;
using Nyar.VM.NyarVM.GC;
using Nyar.VM.NyarVM.Jit;
using Nyar.VM.NyarVM.Observability;
using Nyar.VM.NyarVM.Profiling;
using Nyar.VM.NyarVM.Runtime;
using Std.Data.Binary.NyarIR.Data;
using Std.Data.Binary.NyarIR.Decode;
using RuntimeNyarFunction = Nyar.Types.NyarFunction;
using ValueType = Nyar.Types.ValueType;

namespace Nyar.VM.NyarVM;

/// <summary>
///     Nyar 运行时异常
/// </summary>
public sealed class NyarRuntimeException : Exception
{
    /// <summary>
    ///     初始化 NyarRuntimeException
    /// </summary>
    /// <param name="message">错误消息</param>
    public NyarRuntimeException(string message) : base(message)
    {
    }
}

/// <summary>
///     指令执行器。
///     运行时先从代码字节流解码出指令，再根据指令的头码字段与操作数执行对应逻辑。
/// </summary>
public sealed class Executor
{
    /// <summary>
    ///     GC 检查间隔（指令数），必须是 2 的幂
    /// </summary>
    private const long _gc_check_interval = 1024;

    /// <summary>
    ///     GC 检查间隔掩码（GcCheckInterval - 1），用于位运算替代模运算
    /// </summary>
    private const long _gc_check_interval_mask = _gc_check_interval - 1;

    /// <summary>
    ///     断点命中时的返回值（特殊 sentinel 值）
    /// </summary>
    public static readonly Value break_value = Value.from_int(int.MinValue);

    /// <summary>
    ///     原始代码字节流。
    /// </summary>
    private readonly byte[] _bytecode;

    /// <summary>
    ///     调试器（断点 + 单步执行）
    /// </summary>
    private readonly NyarDebugger _debugger;

    /// <summary>
    ///     是否启用调试器
    /// </summary>
    private readonly bool _debugger_enabled;

    /// <summary>
    ///     预解码指令数组。
    ///     它按代码字节偏移建立索引，但元素本身是解码后的指令对象。
    /// </summary>
    private readonly NyarInstruction[] _decoded_instructions;

    /// <summary>
    ///     代数效应运行时
    /// </summary>
    private readonly EffectRuntime _effect_runtime;

    /// <summary>
    ///     运行时事件系统（可选）
    /// </summary>
    private readonly VmEvents? _events;

    /// <summary>
    ///     外部函数接口
    /// </summary>
    private readonly Ffi _ffi;

    /// <summary>
    ///     帧栈
    /// </summary>
    private readonly Stack<Frame> _frame_stack;

    /// <summary>
    ///     精确非移动 GC
    /// </summary>
    private readonly NyarGc _gc;

    /// <summary>
    ///     GC Root 提供者
    /// </summary>
    private readonly ExecutorGcRootProvider _gc_root_provider;

    /// <summary>
    ///     堆内存
    /// </summary>
    private readonly NyarHeap _heap;

    /// <summary>
    ///     内置函数注册表
    /// </summary>
    private readonly Intrinsics _intrinsics;

    /// <summary>
    ///     JIT 编译器
    /// </summary>
    private readonly JitCompiler _jit_compiler;

    /// <summary>
    ///     运行时指标收集器（可选）
    /// </summary>
    private readonly VmMetrics? _metrics;

    /// <summary>
    ///     是否启用可观测性指标记录
    /// </summary>
    private readonly bool _metrics_enabled;

    /// <summary>
    ///     当前模块
    /// </summary>
    private readonly IModule _module;

    /// <summary>
    ///     OSR 管理器
    /// </summary>
    private readonly OsrManager _osr_manager = new();

    /// <summary>
    ///     OpCode 级性能分析器
    /// </summary>
    private readonly NyarProfiler _profiler;

    /// <summary>
    ///     运行时资源限制
    /// </summary>
    private readonly ResourceLimits _resource_limits;

    /// <summary>
    ///     操作数栈
    /// </summary>
    private readonly ValueStack _stack;

    /// <summary>
    ///     Witness Table 运行时
    /// </summary>
    private readonly WitnessTable _witness_table;

    /// <summary>
    ///     执行开始时间（用于超时检查）
    /// </summary>
    private long _execution_start_ticks;

    /// <summary>
    ///     GC 自动回收间隔（指令数）
    /// </summary>
    private long _instruction_count;

    /// <summary>
    ///     初始化 Executor
    /// </summary>
    /// <param name="module">要执行的模块</param>
    /// <param name="bytecode">原始代码字节流</param>
    /// <param name="jitCompiler">JIT 编译器（可选）</param>
    /// <param name="gc">GC 实例（可选）</param>
    /// <param name="intrinsics">内置函数注册表（可选）</param>
    /// <param name="ffi">FFI 实例（可选）</param>
    /// <param name="metrics">指标收集器（可选）</param>
    /// <param name="events">事件系统（可选）</param>
    /// <param name="resourceLimits">资源限制（可选）</param>
    public Executor(IModule module, byte[] bytecode, JitCompiler? jitCompiler = null, NyarGc? gc = null,
        Intrinsics? intrinsics = null, Ffi? ffi = null, VmMetrics? metrics = null, VmEvents? events = null,
        ResourceLimits? resourceLimits = null)
    {
        _module = module;
        _bytecode = bytecode;
        _jit_compiler = jitCompiler ?? new JitCompiler();
        _gc = gc ?? new NyarGc(Value.shared_object_table, Value.shared_table_lock);
        _intrinsics = intrinsics ?? new Intrinsics();
        _ffi = ffi ?? new Ffi();
        _metrics = metrics;
        _metrics_enabled = _metrics != null;
        _events = events;
        _resource_limits = resourceLimits ?? new ResourceLimits();

        _stack = new ValueStack(_resource_limits.max_stack_depth);
        _frame_stack = new Stack<Frame>(_resource_limits.max_frame_depth);
        _heap = new NyarHeap();
        _witness_table = new WitnessTable();
        _profiler = new NyarProfiler();
        _debugger = new NyarDebugger();
        _debugger_enabled = true;

        _effect_runtime = new EffectRuntime();
        _gc_root_provider = new ExecutorGcRootProvider(_stack, _frame_stack, _heap);

        _decoded_instructions = new NyarInstruction[_bytecode.Length];
        Array.Fill(_decoded_instructions, default);

        register_witness_entries();
    }

    /// <summary>
    ///     获取内置函数注册表
    /// </summary>
    public Intrinsics intrinsics => _intrinsics;

    /// <summary>
    ///     获取外部函数接口
    /// </summary>
    public Ffi ffi => _ffi;

    /// <summary>
    ///     获取堆内存
    /// </summary>
    public NyarHeap heap => _heap;

    /// <summary>
    ///     获取代数效应运行时
    /// </summary>
    public EffectRuntime effect_runtime => _effect_runtime;

    /// <summary>
    ///     获取 Witness Table 运行时
    /// </summary>
    public WitnessTable witness_table => _witness_table;

    /// <summary>
    ///     获取精确非移动 GC
    /// </summary>
    public NyarGc gc => _gc;

    /// <summary>
    ///     获取 JIT 编译器
    /// </summary>
    public JitCompiler jit_compiler => _jit_compiler;

    /// <summary>
    ///     OpCode 级性能分析器
    /// </summary>
    public NyarProfiler profiler => _profiler;

    /// <summary>
    ///     调试器（断点 + 单步执行）
    /// </summary>
    public NyarDebugger debugger => _debugger;

    /// <summary>
    ///     检查运行时资源限制
    /// </summary>
    private void check_resource_limits()
    {
        if (_resource_limits.max_instructions > 0 && _instruction_count > _resource_limits.max_instructions)
            throw new ResourceLimitExceededException("指令执行数", _instruction_count, _resource_limits.max_instructions);

        if (_resource_limits.max_execution_time_ms > 0 && _execution_start_ticks > 0)
        {
            var elapsedMs = (DateTime.UtcNow.Ticks - _execution_start_ticks) / 10000.0;
            if (elapsedMs > _resource_limits.max_execution_time_ms)
                throw new ResourceLimitExceededException("执行时间", (long)elapsedMs,
                    (long)_resource_limits.max_execution_time_ms);
        }

        if (_resource_limits.max_stack_depth > 0 && _stack.count > _resource_limits.max_stack_depth)
            throw new ResourceLimitExceededException("值栈深度", _stack.count, _resource_limits.max_stack_depth);
    }

    /// <summary>
    ///     注册模块中的 Witness 分派条目到运行时 WitnessTable
    /// </summary>
    private void register_witness_entries()
    {
        if (_module is not NyarModule nyarModule) return;

        foreach (var entry in nyarModule.witness_entries)
        {
            _witness_table.register_method(entry.method_id, entry.type_id, entry.method_name, entry.function_index);

            if (entry.interface_id != 0)
            {
                var methodTable = new Dictionary<int, int> { { entry.interface_method_index, entry.method_id } };
                _witness_table.register_interface(entry.type_id, entry.interface_id, methodTable);
            }
        }
    }

    /// <summary>
    ///     执行指定函数
    /// </summary>
    public Value execute_function(IFunction function, ReadOnlySpan<Value> args)
    {
        if (_metrics_enabled) _metrics!.start_execution_timer();
        _execution_start_ticks = DateTime.UtcNow.Ticks;

        var frame = new Frame(function, 0, _stack.count);
        frame.set_arguments(args);
        _frame_stack.Push(frame);

        JitCallHelper.set_context(_jit_compiler, jit_interpret_function);

        while (_frame_stack.Count > 0)
        {
            var currentFrame = _frame_stack.Peek();

            if (_profiler.enabled)
            {
                _profiler.begin_measure();
                var profiledOp = _bytecode[currentFrame.pc];
                var result = step(currentFrame);
                _profiler.end_measure(profiledOp);

                if (result == ControlResult.@return && _frame_stack.Count == 0)
                    return _stack.count > 0 ? _stack.pop() : Value.@null;

                if (result == ControlResult.@throw)
                {
                    if (handle_throw()) continue;
                    return _stack.count > 0 ? _stack.pop() : Value.@null;
                }

                if (result == ControlResult.@break) return break_value;
            }
            else
            {
                var result = step(currentFrame);

                if (result == ControlResult.@return && _frame_stack.Count == 0)
                    return _stack.count > 0 ? _stack.pop() : Value.@null;

                if (result == ControlResult.@throw)
                {
                    if (handle_throw()) continue;
                    return _stack.count > 0 ? _stack.pop() : Value.@null;
                }

                if (result == ControlResult.@break) return break_value;
            }
        }

        return _stack.count > 0 ? _stack.pop() : Value.@null;
    }

    /// <summary>
    ///     从断点处继续执行
    /// </summary>
    public Value continue_execution(IFunction function)
    {
        JitCallHelper.set_context(_jit_compiler, jit_interpret_function);

        while (_frame_stack.Count > 0)
        {
            var currentFrame = _frame_stack.Peek();
            var result = step(currentFrame);

            if (result == ControlResult.@return && _frame_stack.Count == 0)
                return _stack.count > 0 ? _stack.pop() : Value.@null;

            if (result == ControlResult.@throw)
            {
                if (handle_throw()) continue;
                return _stack.count > 0 ? _stack.pop() : Value.@null;
            }

            if (result == ControlResult.@break) return break_value;
        }

        return _stack.count > 0 ? _stack.pop() : Value.@null;
    }

    /// <summary>
    ///     获取当前帧栈快照
    /// </summary>
    public List<FrameSnapshot> get_frame_snapshots()
    {
        var snapshots = new List<FrameSnapshot>();
        foreach (var frame in _frame_stack)
        {
            var locals = new List<Value>();
            if (frame.locals != null)
                for (var i = 0; i < frame.locals.Length; i++)
                    locals.Add(frame.locals[i]);

            snapshots.Add(new FrameSnapshot(frame.function.name, frame.pc, frame.return_pc, frame.stack_base, locals));
        }
        return snapshots;
    }

    private bool handle_throw()
    {
        if (has_catch_handler_in_current_frame()) return true;

        _frame_stack.Pop();

        if (_frame_stack.Count == 0)
        {
            var exMsg = ControlOps.has_pending_exception
                ? $"未捕获的异常: {ControlOps.get_pending_exception()}"
                : "未捕获的异常";
            ControlOps.clear_pending_exception();
            throw new NyarRuntimeException(exMsg);
        }

        if (has_catch_handler_in_current_frame()) return true;

        return handle_throw();
    }

    private bool has_catch_handler_in_current_frame()
    {
        if (_frame_stack.Count == 0) return false;

        var frame = _frame_stack.Peek();
        var func = frame.function;
        if (func is not RuntimeNyarFunction nyarFunc) return false;

        var codeBytes = nyarFunc.module?.raw_bytecode ?? _bytecode;
        if (codeBytes is null) return false;

        var startPc = nyarFunc.code_offset;
        var endPc = startPc + nyarFunc.code_length;

        for (var pc = startPc; pc < endPc && pc < codeBytes.Length; pc++)
            if ((NyarHeadCode)codeBytes[pc] == NyarHeadCode.@catch)
                return true;

        return false;
    }

    private NyarInstruction decode_instruction_at(int pc)
    {
        return InstructionDecoder.decode_at(_bytecode, pc);
    }

    /// <summary>
    ///     执行单步指令。
    ///     使用 AggressiveInlining 标记热点操作，确保解释器循环的高效性。
    /// </summary>
    private ControlResult step(Frame frame)
    {
        if (frame.pc < 0 || frame.pc >= _bytecode.Length) return ControlResult.@return;

        var di = _decoded_instructions[frame.pc];
        if (!di.is_valid)
        {
            di = decode_instruction_at(frame.pc);
            _decoded_instructions[frame.pc] = di;
        }

        _instruction_count++;

        if ((_instruction_count & _gc_check_interval_mask) == 0)
        {
            _gc.maybe_collect(_gc_root_provider);
            if (_metrics_enabled) _metrics!.update_gc_live_count(_gc.live_object_count);
            check_resource_limits();
        }

        if (_debugger_enabled && _debugger.should_break_at(frame.function.name, frame.pc, _frame_stack.Count))
            return ControlResult.@break;

        if (_metrics_enabled) _metrics!.record_instruction();

        var headCode = di.code;

        switch (headCode)
        {
            case NyarHeadCode.@const:
                return inline_handle_const(di, frame);

            case NyarHeadCode.pop:
                _stack.pop();
                frame.pc += di.size;
                return ControlResult.@continue;

            case NyarHeadCode.dup:
                _stack.dup();
                frame.pc += di.size;
                return ControlResult.@continue;

            case NyarHeadCode.swap:
                _stack.swap();
                frame.pc += di.size;
                return ControlResult.@continue;

            case NyarHeadCode.call:
            case NyarHeadCode.call_static:
                return handle_call(di, frame);

            case NyarHeadCode.call_witness:
                return handle_call_witness(di, frame);

            case NyarHeadCode.call_dynamic:
                return handle_call_dynamic(di, frame);

            case NyarHeadCode.call_intrinsic:
                return handle_call_intrinsic(di, frame);

            case NyarHeadCode.builtin_call:
                return handle_builtin_call(di, frame);

            case NyarHeadCode.call_native:
                return handle_call_native(di, frame);

            case NyarHeadCode.load_native_lib:
                return handle_load_native_lib(di, frame);

            case NyarHeadCode.get_native_func:
                return handle_get_native_func(di, frame);

            case NyarHeadCode.tail_call:
                return handle_tail_call(di, frame);

            case NyarHeadCode.@return:
                if (_debugger.OnReturn()) return ControlResult.@break;
                _frame_stack.Pop();
                return ControlResult.@return;

            case NyarHeadCode.nop:
                frame.pc += di.size;
                return ControlResult.@continue;

            case NyarHeadCode.jump:
            {
                var jumpTarget = frame.pc + di.operand1;
                if (jumpTarget < frame.pc)
                {
                    var funcIndex = find_function_index(frame.function);
                    if (funcIndex >= 0) handle_osr_back_edge(funcIndex, jumpTarget, frame.pc, frame);
                }
                frame.pc = jumpTarget;
                return ControlResult.@continue;
            }

            case NyarHeadCode.jump_if_true:
            case NyarHeadCode.jump_if_false:
                return inline_handle_conditional_jump(di, frame);

            case NyarHeadCode.load_local:
                return inline_handle_load_local(di, frame);

            case NyarHeadCode.store_local:
                return inline_handle_store_local(di, frame);

            case NyarHeadCode.load_arg:
                return inline_handle_load_arg(di, frame);

            case NyarHeadCode.store_arg:
                return inline_handle_store_arg(di, frame);

            case NyarHeadCode.load_global:
            case NyarHeadCode.store_global:
            case NyarHeadCode.alloc:
            case NyarHeadCode.free:
            case NyarHeadCode.i32_load:
            case NyarHeadCode.i32_store:
            case NyarHeadCode.i64_load:
            case NyarHeadCode.i64_store:
                MemoryOps.execute(di, frame, _stack, _heap, _gc);
                frame.pc += di.size;
                return ControlResult.@continue;

            case NyarHeadCode.array_push:
            case NyarHeadCode.array_get:
            case NyarHeadCode.array_set:
                ArrayOps.execute(di, _stack, _gc);
                frame.pc += di.size;
                return ControlResult.@continue;

            case NyarHeadCode.new_object:
            case NyarHeadCode.get_field:
            case NyarHeadCode.set_field:
            case NyarHeadCode.field_store:
            case NyarHeadCode.get_ordinal_index:
            case NyarHeadCode.set_ordinal_index:
            case NyarHeadCode.get_offset_index:
            case NyarHeadCode.set_offset_index:
            case NyarHeadCode.index_store:
            case NyarHeadCode.length:
            case NyarHeadCode.new_closure:
            case NyarHeadCode.get_upvalue:
            case NyarHeadCode.set_upvalue:
            case NyarHeadCode.access_static:
            case NyarHeadCode.access_witness:
                ObjectOps.execute(di, frame, _stack, _gc);
                frame.pc += di.size;
                return ControlResult.@continue;

            case NyarHeadCode.access_dynamic:
                return handle_access_dynamic(di, frame);

            case NyarHeadCode.inline_cache_update:
                return handle_inline_cache_update(di, frame);

            default:
                ArithmeticOps.execute(di.code, _stack);
                frame.pc += di.size;
                return ControlResult.@continue;
        }
    }

    #region 热点指令内联实现

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ControlResult inline_handle_load_local(NyarInstruction di, Frame frame)
    {
        _stack.push(frame.get_local(di.operand1));
        frame.pc += di.size;
        return ControlResult.@continue;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ControlResult inline_handle_store_local(NyarInstruction di, Frame frame)
    {
        frame.set_local(di.operand1, _stack.pop());
        frame.pc += di.size;
        return ControlResult.@continue;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ControlResult inline_handle_load_arg(NyarInstruction di, Frame frame)
    {
        _stack.push(frame.get_local(di.operand1));
        frame.pc += di.size;
        return ControlResult.@continue;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ControlResult inline_handle_store_arg(NyarInstruction di, Frame frame)
    {
        frame.set_local(di.operand1, _stack.pop());
        frame.pc += di.size;
        return ControlResult.@continue;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ControlResult inline_handle_const(NyarInstruction di, Frame frame)
    {
        var constVal = _module.constants[di.operand1];
        _stack.push(constVal);
        frame.pc += di.size;
        return ControlResult.@continue;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ControlResult inline_handle_conditional_jump(NyarInstruction di, Frame frame)
    {
        var condition = _stack.pop().@bool;
        var shouldJump = di.code == NyarHeadCode.jump_if_true ? condition : !condition;

        if (shouldJump)
            frame.pc = di.operand1;
        else
            frame.pc += di.size;

        return ControlResult.@continue;
    }

    #endregion

    private ControlResult handle_call(NyarInstruction di, Frame frame)
    {
        var funcIndex = di.operand1;
        if (funcIndex < 0 || funcIndex >= _module.functions.Count)
            throw new NyarRuntimeException($"函数索引越界: {funcIndex}");

        if (_frame_stack.Count >= _resource_limits.max_frame_depth)
            throw new NyarRuntimeException(
                $"资源耗尽：帧栈深度 当前值 {_frame_stack.Count} 超过限制 {_resource_limits.max_frame_depth}{Environment.NewLine}{describe_frame_stack()}");

        var targetFunc = _module.functions[funcIndex];
        targetFunc.module = _module;

        frame.pc += di.size;
        if (_metrics_enabled) _metrics!.record_function_call();

        var args = new Value[targetFunc.arity];
        for (var i = targetFunc.arity - 1; i >= 0; i--)
            args[i] = _stack.pop();

        _jit_compiler.record_call(funcIndex);

        var jitFunc = _jit_compiler.get_compiled_function(funcIndex);
        if (jitFunc != null)
        {
            if (_metrics_enabled) _metrics!.record_jit_execution();
            Value result = jitFunc.execute(args);
            _stack.push(result);
            return ControlResult.@continue;
        }

        var newFrame = new Frame(targetFunc, frame.pc, _stack.count);
        newFrame.set_arguments(args);
        _frame_stack.Push(newFrame);
        return ControlResult.@continue;
    }

    /// <summary>
    ///     输出当前帧栈的顶部函数链，帮助定位自递归与错误分派。
    /// </summary>
    /// <returns>帧栈摘要</returns>
    private string describe_frame_stack()
    {
        if (_frame_stack.Count == 0)
        {
            return "帧栈快照：<empty>";
        }

        var frames = _frame_stack
            .Take(12)
            .Select(current => $"{current.function.name}@pc={current.pc}");

        return $"帧栈快照：{string.Join(" -> ", frames)}";
    }

    private ControlResult handle_call_witness(NyarInstruction di, Frame frame)
    {
        var interfaceId = di.operand1;
        var slotIndex = di.operand2;
        var witnessMethodId = slotIndex != 0 ? slotIndex : interfaceId;

        var dispatchEntry = _witness_table.find_method(0, witnessMethodId.ToString());
        if (dispatchEntry != null)
        {
            frame.pc += di.size;
            var funcIndex = dispatchEntry.function_index;
            if (funcIndex >= 0 && funcIndex < _module.functions.Count)
            {
                var callee = _module.functions[funcIndex];
                var args = new Value[callee.arity];
                for (var i = callee.arity - 1; i >= 0; i--) args[i] = _stack.pop();
                var result = execute_function(callee, args);
                _stack.push(result);
                return ControlResult.@continue;
            }
        }
        return handle_call(di, frame);
    }

    private ControlResult handle_call_dynamic(NyarInstruction di, Frame frame)
    {
        var cacheSlot = di.operand1;
        var argCount = di.operand2;
        var methodNameConstIdx = di.operand3;
        var callSitePc = frame.pc;

        _jit_compiler.register_cache_slot_pc(cacheSlot, callSitePc);
        frame.pc += di.size;

        var args = new Value[argCount];
        for (var i = argCount - 1; i >= 0; i--)
            args[i] = _stack.pop();

        var receiver = _stack.pop();
        var cachedFuncIndex = _jit_compiler.lookup_inline_cache(cacheSlot, receiver);
        if (cachedFuncIndex >= 0)
            return execute_dynamic_call(cacheSlot, receiver, args, cachedFuncIndex, callSitePc, di.size);

        var resolvedFuncIndex = resolve_dynamic_method(receiver, methodNameConstIdx);
        if (resolvedFuncIndex >= 0)
        {
            _jit_compiler.update_inline_cache(cacheSlot, receiver, resolvedFuncIndex);
            return execute_dynamic_call(cacheSlot, receiver, args, resolvedFuncIndex, callSitePc, di.size);
        }

        _stack.push(receiver);
        for (var i = 0; i < args.Length; i++) _stack.push(args[i]);
        return ControlResult.@continue;
    }

    private ControlResult execute_dynamic_call(int cacheSlot, Value receiver, Value[] args, int funcIndex, int callSitePc, int instructionSize)
    {
        var decision = _jit_compiler.record_call_site_hit(cacheSlot, receiver, funcIndex);
        if (decision is DevirtualizationDecision.promote_to_static or DevirtualizationDecision.promote_to_witness)
        {
            var patched = _jit_compiler.patch_bytecode(cacheSlot, decision, funcIndex);
            if (patched) InstructionDecoder.re_decode_at(_decoded_instructions, _bytecode, callSitePc, (byte)instructionSize);
        }

        var targetFunc = _module.functions[funcIndex];
        targetFunc.module = _module;

        var fullArgs = new Value[targetFunc.arity];
        fullArgs[0] = receiver;
        for (var i = 1; i < targetFunc.arity && i <= args.Length; i++)
            fullArgs[i] = args[i - 1];

        var newFrame = new Frame(targetFunc, 0, _stack.count);
        newFrame.set_arguments(fullArgs);
        _frame_stack.Push(newFrame);
        return ControlResult.@continue;
    }

    private int resolve_dynamic_method(Value receiver, int methodNameConstIdx)
    {
        if (methodNameConstIdx < 0 || methodNameConstIdx >= _module.constants.Count) return -1;
        var methodName = _module.constants[methodNameConstIdx].@object as string;
        if (string.IsNullOrEmpty(methodName)) return -1;

        var dispatchEntry = _witness_table.find_method(0, methodName);
        if (dispatchEntry is { function_index: >= 0 } && dispatchEntry.function_index < _module.functions.Count)
            return dispatchEntry.function_index;

        for (var i = 0; i < _module.exports.Count; i++)
            if (_module.exports[i].name == methodName)
            {
                var exportFuncIdx = _module.exports[i].function_index;
                if (exportFuncIdx >= 0 && exportFuncIdx < _module.functions.Count) return exportFuncIdx;
            }

        for (var i = 0; i < _module.functions.Count; i++)
            if (_module.functions[i].name == methodName) return i;

        return -1;
    }

    private ControlResult handle_access_dynamic(NyarInstruction di, Frame frame)
    {
        var cacheSlot = di.operand1;
        var fieldNameConstIdx = di.operand2;
        var callSitePc = frame.pc;

        _jit_compiler.register_cache_slot_pc(cacheSlot, callSitePc);
        frame.pc += di.size;

        var obj = _stack.pop();
        var cachedFieldOffset = _jit_compiler.lookup_inline_cache(cacheSlot, obj);
        if (cachedFieldOffset >= 0)
        {
            var decision = _jit_compiler.record_call_site_hit(cacheSlot, obj, cachedFieldOffset);
            if (decision is DevirtualizationDecision.promote_to_static or DevirtualizationDecision.promote_to_witness)
            {
                var patched = _jit_compiler.patch_bytecode(cacheSlot, decision, cachedFieldOffset);
                if (patched) InstructionDecoder.re_decode_at(_decoded_instructions, _bytecode, callSitePc, di.size);
            }

            if (obj.@object is List<Value> list && cachedFieldOffset < list.Count)
            {
                _stack.push(list[cachedFieldOffset]);
                return ControlResult.@continue;
            }
            _stack.push(Value.@null);
            return ControlResult.@continue;
        }

        if (fieldNameConstIdx >= 0 && fieldNameConstIdx < _module.constants.Count)
        {
            var fieldName = _module.constants[fieldNameConstIdx].utf8 as string;
            if (obj.@object is Dictionary<string, Value> dict && fieldName != null)
            {
                var value = dict.GetValueOrDefault(fieldName, Value.@null);
                _stack.push(value);
                return ControlResult.@continue;
            }
        }
        _stack.push(Value.@null);
        return ControlResult.@continue;
    }

    private ControlResult handle_inline_cache_update(NyarInstruction di, Frame frame)
    {
        var cacheSlot = di.operand1;
        frame.pc += di.size;
        var funcIndexVal = _stack.pop();
        var receiver = _stack.pop();
        if (funcIndexVal.type == ValueType.i32)
            _jit_compiler.update_inline_cache(cacheSlot, receiver, funcIndexVal.i32);
        return ControlResult.@continue;
    }

    private ControlResult handle_tail_call(NyarInstruction di, Frame frame)
    {
        var funcIndex = di.operand1;
        if (funcIndex < 0 || funcIndex >= _module.functions.Count)
            throw new NyarRuntimeException($"尾调用函数索引越界: {funcIndex}");

        var targetFunc = _module.functions[funcIndex];
        targetFunc.module = _module;

        var args = new Value[targetFunc.arity];
        for (var i = targetFunc.arity - 1; i >= 0; i--)
            args[i] = _stack.pop();

        var currentFrame = _frame_stack.Peek();
        if (currentFrame.function.name == targetFunc.name)
        {
            currentFrame.reset(targetFunc, _stack.count);
            currentFrame.set_arguments(args);
            return ControlResult.@continue;
        }

        _frame_stack.Pop();
        var newFrame = new Frame(targetFunc, 0, _stack.count);
        newFrame.set_arguments(args);
        _frame_stack.Push(newFrame);
        return ControlResult.@continue;
    }

    private int find_function_index(IFunction function)
    {
        for (var i = 0; i < _module.functions.Count; i++)
            if (ReferenceEquals(_module.functions[i], function)) return i;
        return -1;
    }

    private void handle_osr_back_edge(int functionIndex, int loopHeadPc, int backEdgePc, Frame frame)
    {
        var shouldOsr = _osr_manager.record_back_edge(functionIndex, loopHeadPc, backEdgePc);
        if (!shouldOsr) return;

        var osrEntry = _osr_manager.find_compiled_osr_entry(functionIndex, loopHeadPc);
        if (osrEntry != null)
        {
            try_osr_transition(osrEntry, frame);
            return;
        }

        var localCount = frame.function.arity + frame.function.local_count;
        osrEntry = _osr_manager.register_osr_entry(functionIndex, loopHeadPc, _stack.count, localCount);

        if (!osrEntry.is_compiled)
        {
            var compiler = new OsrCompiler();
            var compiled = compiler.compile(functionIndex, _bytecode, _module, loopHeadPc, localCount, _stack.count);
            if (compiled != null) _osr_manager.OnOsrCompiled(osrEntry, compiled);
        }
    }

    private void try_osr_transition(OsrEntry osrEntry, Frame frame)
    {
        if (osrEntry._compiled_delegate == null) return;
        var localCount = osrEntry.local_count;
        var locals = new Value[localCount];
        for (var i = 0; i < localCount; i++) locals[i] = frame.get_local(i);

        var stackDepth = osrEntry.stack_depth;
        var stackValues = new Value[stackDepth];
        var count = Math.Min(stackDepth, _stack.count);
        for (var i = 0; i < count; i++) stackValues[count - 1 - i] = _stack.pop();

        try {
            var result = osrEntry._compiled_delegate(locals, stackValues);
            _osr_manager.OnOsrTransition();
        } catch (NyarRuntimeException) { }
    }

    private ControlResult handle_builtin_call(NyarInstruction di, Frame frame) => ControlResult.@continue;

    private ControlResult handle_call_intrinsic(NyarInstruction di, Frame frame)
    {
        // call_intrinsic 是 imm2 形态：operand1 = 常量池索引（函数名），operand2 = 参数数量
        var nameConstIndex = di.operand1;
        var argCount = di.operand2;

        frame.pc += di.size;

        // 从常量池读取函数名
        if (nameConstIndex < 0 || nameConstIndex >= _module.constants.Count)
        {
            _stack.push(Value.@null);
            return ControlResult.@continue;
        }

        var nameValue = _module.constants[nameConstIndex];
        var funcName = nameValue.utf8 as string ?? nameValue.@object as string;
        if (string.IsNullOrEmpty(funcName))
        {
            _stack.push(Value.@null);
            return ControlResult.@continue;
        }

        // 查找宿主注册的 intrinsic
        var intrinsicFunc = _intrinsics.find(funcName);
        if (intrinsicFunc == null)
        {
            // 未找到 intrinsic，弹出参数并压入 null
            for (var i = 0; i < argCount; i++) _stack.pop();
            _stack.push(Value.@null);
            return ControlResult.@continue;
        }

        // 从栈上弹出参数（逆序）
        var args = new Value[argCount];
        for (var i = argCount - 1; i >= 0; i--)
        {
            args[i] = _stack.pop();
        }

        // 调用 intrinsic 并压入返回值
        var result = intrinsicFunc(args);
        _stack.push(result);
        return ControlResult.@continue;
    }

    private ControlResult handle_call_native(NyarInstruction di, Frame frame) => ControlResult.@continue;
    private ControlResult handle_load_native_lib(NyarInstruction di, Frame frame) => ControlResult.@continue;
    private ControlResult handle_get_native_func(NyarInstruction di, Frame frame) => ControlResult.@continue;

    private Value jit_interpret_function(int functionIndex, Value[] args)
    {
        var function = _module.functions[functionIndex];
        var frame = new Frame(function, 0, 0);
        frame.set_arguments(args);
        while (true)
        {
            var result = step(frame);
            if (result == ControlResult.@return) return _stack.count > 0 ? _stack.pop() : Value.@null;
        }
    }
}
