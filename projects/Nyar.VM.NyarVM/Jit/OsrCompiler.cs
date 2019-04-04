using System.Reflection.Emit;
using Nyar.Analyzer.ModuleSystem;
using Nyar.Types;
using Std.Data.Binary.NyarIR.Data;

namespace Nyar.VM.NyarVM.Jit;

/// <summary>
///     OSR 编译器。
///     它从指定代码字节偏移开始编译函数后半段，生成接受 `locals` 和 `stack` 的入口委托。
/// </summary>
internal sealed class OsrCompiler
{
    /// <summary>
    ///     从指定代码字节偏移开始编译函数体，生成 OSR 入口委托。
    /// </summary>
    /// <param name="functionIndex">函数索引。</param>
    /// <param name="codeBytes">函数所在模块的代码字节流。</param>
    /// <param name="module">模块。</param>
    /// <param name="osrEntryPc">OSR 入口的代码字节偏移。</param>
    /// <param name="localCount">局部变量数量。</param>
    /// <param name="stackDepth">入口处操作数栈深度。</param>
    /// <returns>OSR 编译委托，参数1=locals，参数2=stack values；编译失败返回 null。</returns>
    public Func<Value[], Value[], Value>? compile(
        int functionIndex,
        byte[] codeBytes,
        IModule module,
        int osrEntryPc,
        int localCount,
        int stackDepth)
    {
        var function = module.functions[functionIndex];
        var endPc = function.code_offset + function.code_length;

        if (!is_osr_compatible(codeBytes, osrEntryPc, endPc, module)) return null;

        try
        {
            return emit_osr_method(functionIndex, codeBytes, module, osrEntryPc, endPc, localCount, stackDepth);
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>
    ///     检查从 OSR 入口点到函数结尾的代码字节流是否可 JIT 编译。
    /// </summary>
    private static bool is_osr_compatible(byte[] codeBytes, int osrEntryPc, int endPc, IModule module)
    {
        for (var pc = osrEntryPc; pc < endPc;)
        {
            var opcode = (NyarHeadCode)codeBytes[pc];
            if (!JitEmitCompiler.is_supported_opcode(opcode)) return false;

            pc += JitEmitCompiler.get_instruction_size(opcode);
        }

        return true;
    }

    /// <summary>
    ///     发射 OSR 方法的 IL
    /// </summary>
    private Func<Value[], Value[], Value> emit_osr_method(
        int functionIndex,
        byte[] codeBytes,
        IModule module,
        int osrEntryPc,
        int endPc,
        int localCount,
        int stackDepth)
    {
        var method = new DynamicMethod(
            $"osr_func_{functionIndex}_pc{osrEntryPc}",
            typeof(Value),
            [typeof(Value[]), typeof(Value[])],
            typeof(OsrCompiler),
            true);

        var il = method.GetILGenerator();

        var locals = new LocalBuilder[localCount];
        for (var i = 0; i < localCount; i++)
        {
            locals[i] = il.DeclareLocal(typeof(Value));
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldc_I4, i);
            il.Emit(OpCodes.Ldelem, typeof(Value));
            il.Emit(OpCodes.Stloc, locals[i]);
        }

        var stackSlots = new LocalBuilder[stackDepth + 16];
        for (var i = 0; i < stackDepth; i++)
        {
            stackSlots[i] = il.DeclareLocal(typeof(Value));
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldc_I4, i);
            il.Emit(OpCodes.Ldelem, typeof(Value));
            il.Emit(OpCodes.Stloc, stackSlots[i]);
        }

        for (var i = stackDepth; i < stackSlots.Length; i++) stackSlots[i] = il.DeclareLocal(typeof(Value));

        var returnValue = il.DeclareLocal(typeof(Value));
        var returnLabel = il.DefineLabel();

        var depth = stackDepth;

        for (var pc = osrEntryPc; pc < endPc;)
        {
            var opcode = (NyarHeadCode)codeBytes[pc];

            switch (opcode)
            {
                case NyarHeadCode.@return:
                {
                    if (depth > 0)
                        il.Emit(OpCodes.Ldloc, stackSlots[depth - 1]);
                    else
                        il.Emit(OpCodes.Ldloc, locals[0]);

                    il.Emit(OpCodes.Stloc, returnValue);
                    il.Emit(OpCodes.Br, returnLabel);
                    pc += 1;
                    break;
                }

                case NyarHeadCode.jump:
                {
                    var offset = BitConverter.ToInt32(codeBytes, pc + 1);
                    var targetPc = pc + offset;
                    if (targetPc < osrEntryPc)
                        pc += 5;
                    else
                        pc += 5;

                    break;
                }

                default:
                {
                    pc += JitEmitCompiler.get_instruction_size(opcode);
                    break;
                }
            }
        }

        il.MarkLabel(returnLabel);
        il.Emit(OpCodes.Ldloc, returnValue);
        il.Emit(OpCodes.Ret);

        return (Func<Value[], Value[], Value>)method.CreateDelegate(typeof(Func<Value[], Value[], Value>));
    }
}
