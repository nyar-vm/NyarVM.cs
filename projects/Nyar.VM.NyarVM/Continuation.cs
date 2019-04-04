using Nyar.Analyzer.ModuleSystem;
using Nyar.Types;

namespace Nyar.VM.NyarVM;

/// <summary>
///     被 PerformEffect 捕获的一次性续延，handler 可通过 <see cref="resume" /> 恢复执行
/// </summary>
public sealed class Continuation
{
    /// <summary>
    ///     创建续延快照
    /// </summary>
    /// <param name="stackSnapshot">值栈中 handler 基指针以上的值。</param>
    /// <param name="frameSnapshots">handler 之上的帧快照（含当前帧）。</param>
    /// <param name="module">当前模块。</param>
    /// <param name="bytecode">当前字节码。</param>
    /// <param name="resumePc">恢复后继续执行的 PC。</param>
    internal Continuation(Value[] stackSnapshot, FrameSnapshot[] frameSnapshots, IModule module, byte[] bytecode,
        int resumePc)
    {
        _stack_snapshot = stackSnapshot;
        _frames = frameSnapshots;
        _module = module;
        _bytecode = bytecode;
        _resume_pc = resumePc;
    }

    /// <summary>
    ///     值栈快照
    /// </summary>
    internal Value[] _stack_snapshot { get; }

    /// <summary>
    ///     帧栈快照
    /// </summary>
    internal FrameSnapshot[] _frames { get; }

    /// <summary>
    ///     所属模块
    /// </summary>
    internal IModule _module { get; }

    /// <summary>
    ///     所属字节码
    /// </summary>
    internal byte[] _bytecode { get; }

    /// <summary>
    ///     恢复后的程序计数器
    /// </summary>
    internal int _resume_pc { get; }

    /// <summary>
    ///     是否已被调用（一次性别名）
    /// </summary>
    internal bool _invoked { get; private set; }

    /// <summary>
    ///     恢复续延，将 <paramref name="value" /> 作为 perform 表达式的返回值
    /// </summary>
    /// <param name="value">恢复值。</param>
    /// <param name="stack">当前值栈（会被覆盖为快照内容 + resume value）。</param>
    /// <param name="frameStack">当前帧栈（会被覆盖为快照内容）。</param>
    /// <param name="currentFrame">当前帧（会被重写为恢复后的帧）。</param>
    /// <exception cref="InvalidOperationException">续延已恢复过时抛出</exception>
    internal void resume(Value value, ValueStack stack, Stack<Frame> frameStack, ref Frame currentFrame)
    {
        if (_invoked) throw new InvalidOperationException("续延已被恢复，一次性续延只能调用一次。");

        _invoked = true;

        stack.clear();
        for (var i = 0; i < _stack_snapshot.Length; i++) stack.push(_stack_snapshot[i]);

        frameStack.Clear();
        for (var i = _frames.Length - 1; i >= 0; i--)
        {
            var snapshot = _frames[i];
            var func = _module.find_function(snapshot.function_name)
                       ?? throw new InvalidOperationException($"续延恢复失败：函数 '{snapshot.function_name}' 未找到。");

            var frame = new Frame(func, snapshot.return_pc, snapshot.stack_base)
            {
                pc = snapshot.pc
            };

            for (var j = 0; j < snapshot.locals.Count; j++) frame.set_local(j, snapshot.locals[j]);

            frameStack.Push(frame);
        }

        stack.push(value);

        var topFrame = frameStack.Peek();
        topFrame.pc = _resume_pc;
        currentFrame = topFrame;
    }
}