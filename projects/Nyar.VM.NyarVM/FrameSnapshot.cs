using Nyar.Types;

namespace Nyar.VM.NyarVM;

/// <summary>
///     帧快照：用于调试器变量查看和续延（Continuation）序列化和恢复
/// </summary>
public class FrameSnapshot
{
    /// <summary>
    ///     初始化帧快照
    /// </summary>
    /// <param name="functionName">函数名称。</param>
    /// <param name="pc">当前程序计数器。</param>
    /// <param name="returnPc">返回地址（用于续延恢复）。</param>
    /// <param name="stackBase">栈基址（用于续延恢复）。</param>
    /// <param name="locals">局部变量列表（用于续延恢复）。</param>
    public FrameSnapshot(string functionName, int pc, int returnPc, int stackBase, List<Value> locals)
    {
        function_name = functionName;
        this.pc = pc;
        return_pc = returnPc;
        stack_base = stackBase;
        this.locals = locals;
    }

    /// <summary>
    ///     函数名称
    /// </summary>
    public string function_name { get; }

    /// <summary>
    ///     当前程序计数器
    /// </summary>
    public int pc { get; }

    /// <summary>
    ///     返回地址（用于续延恢复）
    /// </summary>
    public int return_pc { get; }

    /// <summary>
    ///     栈基址（用于续延恢复）
    /// </summary>
    public int stack_base { get; }

    /// <summary>
    ///     局部变量列表（用于续延恢复）
    /// </summary>
    public List<Value> locals { get; }
}