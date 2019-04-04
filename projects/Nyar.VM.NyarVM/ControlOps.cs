using Nyar.Analyzer.ModuleSystem;
using Nyar.Types;
using Nyar.VM.NyarVM.Runtime;
using Std.Data.Binary.NyarIR.Data;
using Std.Data.Binary.NyarIR.Decode;
using ValueType = Nyar.Types.ValueType;

namespace Nyar.VM.NyarVM;

/// <summary>
///     控制流操作
/// </summary>
public static class ControlOps
{
    /// <summary>
    ///     当前线程中待处理的异常值，用于 Throw→Catch 异常传递
    /// </summary>
    [ThreadStatic] private static Value _s_pending_exception;

    /// <summary>
    ///     获取当前线程是否有待处理的异常
    /// </summary>
    public static bool has_pending_exception => _s_pending_exception.type != ValueType.@null;

    /// <summary>
    ///     获取当前线程的待处理异常值（调用前请先检查 <see cref="has_pending_exception" />）
    /// </summary>
    public static Value get_pending_exception()
    {
        return _s_pending_exception;
    }

    /// <summary>
    ///     清除当前线程的待处理异常
    /// </summary>
    public static void clear_pending_exception()
    {
        _s_pending_exception = default;
    }

    /// <summary>
    ///     执行控制流操作
    /// </summary>
    /// <param name="instruction">当前已解码指令。</param>
    /// <param name="bytecode">原始代码字节流，仅用于需要保留代码上下文的场景。</param>
    /// <param name="frame">当前帧。</param>
    /// <param name="stack">值栈。</param>
    /// <param name="frameStack">帧栈。</param>
    /// <param name="module">当前模块。</param>
    /// <param name="effectRuntime">代数效应运行时（可选）。</param>
    /// <returns>控制流结果。</returns>
    public static ControlResult execute(
        NyarInstruction instruction,
        byte[] bytecode,
        Frame frame,
        ValueStack stack,
        Stack<Frame> frameStack,
        IModule module,
        EffectRuntime? effectRuntime = null)
    {
        switch (instruction.code)
        {
            case NyarHeadCode.nop:
                return ControlResult.@continue;

            case NyarHeadCode.jump:
            {
                frame.pc += instruction.operand1;
                return ControlResult.@continue;
            }

            case NyarHeadCode.jump_if_true:
            {
                var condition = stack.pop();
                if (condition.@bool)
                    frame.pc += instruction.operand1;
                else
                    frame.pc += instruction.size;

                return ControlResult.@continue;
            }

            case NyarHeadCode.jump_if_false:
            {
                var condition = stack.pop();
                if (!condition.@bool)
                    frame.pc += instruction.operand1;
                else
                    frame.pc += instruction.size;

                return ControlResult.@continue;
            }

            case NyarHeadCode.call:
            {
                var funcIndex = instruction.operand1;
                frame.pc += instruction.size;

                if (funcIndex < 0 || funcIndex >= module.functions.Count)
                    throw new NyarRuntimeException($"函数索引越界: {funcIndex}");

                var targetFunc = module.functions[funcIndex];
                var args = new Value[targetFunc.arity];
                for (var i = targetFunc.arity - 1; i >= 0; i--) args[i] = stack.pop();

                var newFrame = new Frame(targetFunc, frame.pc, stack.count);
                newFrame.set_arguments(args);
                frameStack.Push(newFrame);
                return ControlResult.@continue;
            }

            case NyarHeadCode.tail_call:
            {
                var funcIndex = instruction.operand1;

                if (funcIndex < 0 || funcIndex >= module.functions.Count)
                    throw new NyarRuntimeException($"尾调用函数索引越界: {funcIndex}");

                var targetFunc = module.functions[funcIndex];
                var args = new Value[targetFunc.arity];
                for (var i = targetFunc.arity - 1; i >= 0; i--) args[i] = stack.pop();

                frameStack.Pop();
                var newFrame = new Frame(targetFunc, 0, stack.count);
                newFrame.set_arguments(args);
                frameStack.Push(newFrame);
                return ControlResult.@continue;
            }

            case NyarHeadCode.@return:
            {
                frameStack.Pop();
                return ControlResult.@return;
            }

            case NyarHeadCode.@throw:
            {
                if (stack.count == 0)
                    _s_pending_exception = Value.@null;
                else
                    _s_pending_exception = stack.pop();

                return ControlResult.@throw;
            }

            case NyarHeadCode.@catch:
            {
                if (has_pending_exception)
                {
                    stack.push(_s_pending_exception);
                    clear_pending_exception();
                    frame.pc += instruction.operand1 - instruction.size;
                }

                return ControlResult.@continue;
            }

            case NyarHeadCode.yield:
                return ControlResult.yield;

            case NyarHeadCode.resume:
            {
                var value = stack.pop();
                var continuationValue = stack.pop();

                if (continuationValue is { type: ValueType.@object, @object: Continuation continuation })
                    continuation.resume(value, stack, frameStack, ref frame);

                return ControlResult.@continue;
            }

            case NyarHeadCode.effect_handle:
            {
                if (effectRuntime is not null && stack.count >= 2)
                {
                    var payload = stack.pop();
                    var effectNameValue = stack.pop();

                    if (effectNameValue is { type: ValueType.utf8, utf8: string effectName })
                    {
                        var result = effectRuntime.perform(effectName, payload);
                        stack.push(result);
                    }
                    else
                    {
                        stack.push(payload);
                    }
                }

                return ControlResult.@continue;
            }

            case NyarHeadCode.perform_effect:
            {
                if (effectRuntime is null || stack.count < 2) return ControlResult.@continue;

                var payload = stack.pop();
                var effectNameValue = stack.pop();

                if (effectNameValue.type != ValueType.utf8
                    || effectNameValue.utf8 is not string effectName)
                    return ControlResult.@continue;

                var resumePc = frame.pc + instruction.size;

                var continuation = capture_continuation(stack, frameStack, frame, module, bytecode, resumePc);

                var result = effectRuntime.perform(effectName, payload, continuation);

                if (!continuation._invoked) stack.push(result);

                return ControlResult.@continue;
            }

            case NyarHeadCode.enter_effect_handler:
            {
                if (effectRuntime is null) return ControlResult.@continue;

                var scope = new EffectHandlerScope();
                effectRuntime.push_scope(scope);
                return ControlResult.@continue;
            }

            case NyarHeadCode.exit_effect_handler:
            {
                effectRuntime?.pop_scope();
                return ControlResult.@continue;
            }

            case NyarHeadCode.enter_try:
            {
                if (effectRuntime is null || stack.count < 1) return ControlResult.@continue;

                var payload = stack.pop();
                effectRuntime.push_scope(new EffectHandlerScope());
                return ControlResult.@continue;
            }

            case NyarHeadCode.exit_try:
            {
                effectRuntime?.pop_scope();
                return ControlResult.@continue;
            }

            default:
                return ControlResult.@continue;
        }
    }

    /// <summary>
    ///     从当前执行状态构造续延快照
    /// </summary>
    private static Continuation capture_continuation(
        ValueStack stack,
        Stack<Frame> frameStack,
        Frame currentFrame,
        IModule module,
        byte[] bytecode,
        int resumePc)
    {
        var stackSnapshot = stack.snapshot();

        var frameArr = frameStack.ToArray();
        var frameSnapshots = new FrameSnapshot[frameArr.Length];

        for (var i = 0; i < frameArr.Length; i++)
        {
            var f = frameArr[i];
            var locals = new Value[f.local_count];
            for (var j = 0; j < f.local_count; j++) locals[j] = f.get_local(j);

            frameSnapshots[i] = f == currentFrame
                ? new FrameSnapshot(f.function.name, resumePc, f.return_pc, f.stack_base, [.. locals])
                : new FrameSnapshot(f.function.name, f.pc, f.return_pc, f.stack_base, [.. locals]);
        }

        return new Continuation(stackSnapshot, frameSnapshots, module, bytecode, resumePc);
    }
}
