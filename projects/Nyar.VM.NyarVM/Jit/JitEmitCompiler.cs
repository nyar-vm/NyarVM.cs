using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using Nyar.Analyzer.ModuleSystem;
using Nyar.Types;
using Std.Data.Binary.NyarIR.Data;
using ValueType = Nyar.Types.ValueType;

namespace Nyar.VM.NyarVM.Jit;

/// <summary>
///     JIT IL 发射编译器。
///     它从函数对应的代码字节流发射 `DynamicMethod`，而不是直接消费解码后的指令对象。
///     策略：纯 `i32` 函数使用 unboxed `int` 局部变量，纯 `f64` 函数使用 unboxed `double` 局部变量，混合类型函数使用 `Value` 通用路径。
/// </summary>
internal sealed class JitEmitCompiler
{
    #region 指令大小

    /// <summary>
    ///     获取指令大小（字节数）。
    ///     统一复用 IR 侧的编码长度定义，避免 `JIT` 链路维护自己的大小表。
    /// </summary>
    internal static int get_instruction_size(NyarHeadCode headCode)
    {
        return NyarInstruction.code_size(headCode);
    }

    #endregion

    #region 公开 API

    /// <summary>
    ///     从函数代码字节流编译 `DynamicMethod`。
    /// </summary>
    public Func<Value[], Value>? compile(int functionIndex, byte[] codeBytes, IModule module)
    {
        if (functionIndex < 0 || functionIndex >= module.functions.Count) return null;

        var function = module.functions[functionIndex];

        if (!is_jit_compatible(function, codeBytes, module)) return null;

        var stackMap = compute_stack_map(function, codeBytes, module);
        if (stackMap == null) return null;

        var maxStack = stackMap.Values.Max();

        try
        {
            return emit_method(functionIndex, function, codeBytes, module, stackMap, maxStack);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"[JIT] Compile 失败 func_{functionIndex}: {ex.GetType().Name}: {ex.Message}", ex);
        }
    }

    /// <summary>
    ///     从函数代码字节流编译 `Func<int, int>` 形式的 `DynamicMethod`。
    ///     仅适用于 `arity = 1` 的纯 `i32` 函数。
    /// </summary>
    public Func<int, int>? compile_int_int(int functionIndex, byte[] codeBytes, IModule module)
    {
        if (functionIndex < 0 || functionIndex >= module.functions.Count) return null;

        var function = module.functions[functionIndex];

        if (function.arity != 1) return null;

        if (!is_jit_compatible(function, codeBytes, module)) return null;

        if (!is_pure_i32_function(function, codeBytes, module)) return null;

        var stackMap = compute_stack_map(function, codeBytes, module);
        if (stackMap == null) return null;

        var maxStack = stackMap.Values.Max();

        try
        {
            return emit_int_int_method(functionIndex, function, codeBytes, module, stackMap, maxStack);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[JIT] CompileIntInt 失败 func_{functionIndex}: {ex.GetType().Name}: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    ///     从函数代码字节流编译 `Func<double, double>` 形式的 `DynamicMethod`。
    ///     仅适用于 `arity = 1` 的纯 `f64` 函数。
    /// </summary>
    public Func<double, double>? compile_double_double(int functionIndex, byte[] codeBytes, IModule module)
    {
        if (functionIndex < 0 || functionIndex >= module.functions.Count) return null;

        var function = module.functions[functionIndex];

        if (function.arity != 1) return null;

        if (!is_jit_compatible(function, codeBytes, module)) return null;

        if (!is_pure_f64_function(function, codeBytes, module)) return null;

        var stackMap = compute_stack_map(function, codeBytes, module);
        if (stackMap == null) return null;

        var maxStack = stackMap.Values.Max();

        try
        {
            return emit_double_double_method(functionIndex, function, codeBytes, module, stackMap, maxStack);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[JIT] CompileDoubleDouble 失败 func_{functionIndex}: {ex.GetType().Name}: {ex.Message}");
            return null;
        }
    }

    #endregion

    #region 兼容性检�?
    /// <summary>
    ///     检查函数是否可 JIT 编译（支�?i32 + f64 操作的函数）
    /// </summary>
    private static bool is_jit_compatible(IFunction function, byte[] bytecode, IModule module)
    {
        var startPc = function.code_offset;
        var endPc = startPc + function.code_length;
        var pc = startPc;

        while (pc < endPc)
        {
            if (pc >= bytecode.Length) return false;

            var opcode = (NyarHeadCode)bytecode[pc];

            if (!is_supported_opcode(opcode)) return false;

            if (opcode == NyarHeadCode.@const)
            {
                if (pc + 4 >= bytecode.Length) return false;

                var constIndex = BitConverter.ToInt32(bytecode, pc + 1);
                if (constIndex < 0 || constIndex >= module.constants.Count) return false;

                var constType = module.constants[constIndex].type;
                if (constType != ValueType.i32 && constType != ValueType.f64) return false;
            }

            if (opcode == NyarHeadCode.call)
            {
                if (pc + 4 >= bytecode.Length) return false;

                var funcIndex = BitConverter.ToInt32(bytecode, pc + 1);
                if (funcIndex < 0 || funcIndex >= module.functions.Count) return false;
            }

            pc += get_instruction_size(opcode);
        }

        return true;
    }

    /// <summary>
    ///     判断操作码是否被 JIT 编译器支�?    /// </summary>
    internal static bool is_supported_opcode(NyarHeadCode headCode)
    {
        return headCode is
            NyarHeadCode.nop or NyarHeadCode.jump or NyarHeadCode.jump_if_true or NyarHeadCode.jump_if_false or
            NyarHeadCode.call or NyarHeadCode.call_static or NyarHeadCode.@return or NyarHeadCode.tail_call or
            NyarHeadCode.@const or NyarHeadCode.pop or NyarHeadCode.dup or NyarHeadCode.swap or
            NyarHeadCode.load_local or NyarHeadCode.store_local or NyarHeadCode.load_arg or
            NyarHeadCode.i32_add or NyarHeadCode.i32_sub or NyarHeadCode.i32_mul or
            NyarHeadCode.i32_div_s or NyarHeadCode.i32_div_u or
            NyarHeadCode.i32_rem_s or NyarHeadCode.i32_rem_u or
            NyarHeadCode.i32_neg or
            NyarHeadCode.i32_and or NyarHeadCode.i32_or or NyarHeadCode.i32_xor or
            NyarHeadCode.i32_shl or NyarHeadCode.i32_shr_s or NyarHeadCode.i32_shr_u or
            NyarHeadCode.i32_not or
            NyarHeadCode.i32_eq or NyarHeadCode.i32_ne or
            NyarHeadCode.i32_lt_s or NyarHeadCode.i32_le_s or NyarHeadCode.i32_gt_s or NyarHeadCode.i32_ge_s or
            NyarHeadCode.i32_lt_u or NyarHeadCode.i32_le_u or NyarHeadCode.i32_gt_u or NyarHeadCode.i32_ge_u or
            NyarHeadCode.i64_add or NyarHeadCode.i64_sub or NyarHeadCode.i64_mul or
            NyarHeadCode.i64_div_s or NyarHeadCode.i64_div_u or
            NyarHeadCode.i64_rem_s or NyarHeadCode.i64_rem_u or
            NyarHeadCode.i64_neg or
            NyarHeadCode.i64_and or NyarHeadCode.i64_or or NyarHeadCode.i64_xor or
            NyarHeadCode.i64_shl or NyarHeadCode.i64_shr_s or NyarHeadCode.i64_shr_u or
            NyarHeadCode.i64_not or
            NyarHeadCode.i64_eq or NyarHeadCode.i64_ne or
            NyarHeadCode.i64_lt_s or NyarHeadCode.i64_le_s or NyarHeadCode.i64_gt_s or NyarHeadCode.i64_ge_s or
            NyarHeadCode.f64_add or NyarHeadCode.f64_sub or NyarHeadCode.f64_mul or
            NyarHeadCode.f64_div or NyarHeadCode.f64_neg or NyarHeadCode.f64_sqrt or
            NyarHeadCode.f64_eq or NyarHeadCode.f64_ne or
            NyarHeadCode.f64_lt or NyarHeadCode.f64_le or NyarHeadCode.f64_gt or NyarHeadCode.f64_ge or
            NyarHeadCode.i32_extend_i64_s or NyarHeadCode.i32_to_f64_s or NyarHeadCode.i64_trunc_i32_s
            or NyarHeadCode.i64_to_f64 or
            NyarHeadCode.f64_to_i32 or NyarHeadCode.f64_to_i64 or
            NyarHeadCode.load_global or NyarHeadCode.store_global or
            NyarHeadCode.new_object or NyarHeadCode.get_field or NyarHeadCode.set_field or
            NyarHeadCode.array_get or NyarHeadCode.array_set or NyarHeadCode.array_push or
            NyarHeadCode.get_ordinal_index or NyarHeadCode.set_ordinal_index or
            NyarHeadCode.get_offset_index or NyarHeadCode.set_offset_index or NyarHeadCode.length or
            NyarHeadCode.access_static or NyarHeadCode.access_witness or
            NyarHeadCode.new_closure or NyarHeadCode.get_upvalue or NyarHeadCode.set_upvalue or
            NyarHeadCode.alloc or NyarHeadCode.free or
            NyarHeadCode.i32_load or NyarHeadCode.i32_store or
            NyarHeadCode.i64_load or NyarHeadCode.i64_store;
    }

    /// <summary>
    ///     判断函数是否只包�?i32 操作（不�?f64 操作码和 f64 常量�?    /// </summary>
    private static bool is_pure_i32_function(IFunction function, byte[] bytecode, IModule module)
    {
        var startPc = function.code_offset;
        var endPc = startPc + function.code_length;
        var pc = startPc;

        while (pc < endPc)
        {
            if (pc >= bytecode.Length) return false;

            var opcode = (NyarHeadCode)bytecode[pc];

            if (is_f64_opcode(opcode) || is_i64_opcode(opcode) || is_type_conversion_opcode(opcode) ||
                is_object_opcode(opcode)) return false;

            if (opcode == NyarHeadCode.@const)
            {
                var constIndex = BitConverter.ToInt32(bytecode, pc + 1);
                if (constIndex >= 0 && constIndex < module.constants.Count)
                    if (module.constants[constIndex].type == ValueType.f64)
                        return false;
            }

            pc += get_instruction_size(opcode);
        }

        return true;
    }

    /// <summary>
    ///     判断函数是否只包�?f64 操作（不�?i32 操作码和 i32 常量�?    /// </summary>
    private static bool is_pure_f64_function(IFunction function, byte[] bytecode, IModule module)
    {
        var startPc = function.code_offset;
        var endPc = startPc + function.code_length;
        var pc = startPc;

        while (pc < endPc)
        {
            if (pc >= bytecode.Length) return false;

            var opcode = (NyarHeadCode)bytecode[pc];

            if (is_i32_arithmetic_opcode(opcode) || is_i64_opcode(opcode) || is_type_conversion_opcode(opcode) ||
                is_object_opcode(opcode))
                return false;

            if (opcode == NyarHeadCode.@const)
            {
                var constIndex = BitConverter.ToInt32(bytecode, pc + 1);
                if (constIndex >= 0 && constIndex < module.constants.Count)
                    if (module.constants[constIndex].type == ValueType.i32)
                        return false;
            }

            pc += get_instruction_size(opcode);
        }

        return true;
    }

    /// <summary>
    ///     判断操作码是否为对象操作
    /// </summary>
    private static bool is_object_opcode(NyarHeadCode headCode)
    {
        return headCode is
            NyarHeadCode.new_object or NyarHeadCode.get_field or NyarHeadCode.set_field or
            NyarHeadCode.array_get or NyarHeadCode.array_set or NyarHeadCode.array_push or
            NyarHeadCode.get_ordinal_index or NyarHeadCode.set_ordinal_index or
            NyarHeadCode.get_offset_index or NyarHeadCode.set_offset_index or NyarHeadCode.length or
            NyarHeadCode.access_static or NyarHeadCode.access_witness or
            NyarHeadCode.new_closure or NyarHeadCode.get_upvalue or NyarHeadCode.set_upvalue or
            NyarHeadCode.alloc or NyarHeadCode.free or
            NyarHeadCode.i32_load or NyarHeadCode.i32_store or
            NyarHeadCode.ref_eq or NyarHeadCode.ref_ne or
            NyarHeadCode.i64_load or NyarHeadCode.i64_store;
    }

    /// <summary>
    ///     判断操作码是否为 f64 操作（含 f64 比较�?    /// </summary>
    private static bool is_f64_opcode(NyarHeadCode headCode)
    {
        return headCode is
            NyarHeadCode.f64_add or NyarHeadCode.f64_sub or NyarHeadCode.f64_mul or
            NyarHeadCode.f64_div or NyarHeadCode.f64_neg or NyarHeadCode.f64_sqrt or
            NyarHeadCode.f64_eq or NyarHeadCode.f64_ne or
            NyarHeadCode.f64_lt or NyarHeadCode.f64_le or NyarHeadCode.f64_gt or NyarHeadCode.f64_ge;
    }

    /// <summary>
    ///     判断操作码是否为 i64 操作
    /// </summary>
    private static bool is_i64_opcode(NyarHeadCode headCode)
    {
        return headCode is
            NyarHeadCode.i64_add or NyarHeadCode.i64_sub or NyarHeadCode.i64_mul or
            NyarHeadCode.i64_div_s or NyarHeadCode.i64_div_u or
            NyarHeadCode.i64_rem_s or NyarHeadCode.i64_rem_u or
            NyarHeadCode.i64_neg or
            NyarHeadCode.i64_and or NyarHeadCode.i64_or or NyarHeadCode.i64_xor or
            NyarHeadCode.i64_shl or NyarHeadCode.i64_shr_s or NyarHeadCode.i64_shr_u or
            NyarHeadCode.i64_not or
            NyarHeadCode.i64_eq or NyarHeadCode.i64_ne or
            NyarHeadCode.i64_lt_s or NyarHeadCode.i64_le_s or NyarHeadCode.i64_gt_s or NyarHeadCode.i64_ge_s;
    }

    /// <summary>
    ///     判断操作码是否为类型转换操作
    /// </summary>
    private static bool is_type_conversion_opcode(NyarHeadCode headCode)
    {
        return headCode is
            NyarHeadCode.i32_extend_i64_s or NyarHeadCode.i32_to_f64_s or
            NyarHeadCode.i64_trunc_i32_s or NyarHeadCode.i64_to_f64 or
            NyarHeadCode.f64_to_i32 or NyarHeadCode.f64_to_i64;
    }

    /// <summary>
    ///     判断操作码是否为 i32 算术/比较操作（不含控制流和栈操作�?    /// </summary>
    private static bool is_i32_arithmetic_opcode(NyarHeadCode headCode)
    {
        return headCode is
            NyarHeadCode.i32_add or NyarHeadCode.i32_sub or NyarHeadCode.i32_mul or
            NyarHeadCode.i32_div_s or NyarHeadCode.i32_div_u or
            NyarHeadCode.i32_rem_s or NyarHeadCode.i32_rem_u or
            NyarHeadCode.i32_neg or
            NyarHeadCode.i32_and or NyarHeadCode.i32_or or NyarHeadCode.i32_xor or
            NyarHeadCode.i32_shl or NyarHeadCode.i32_shr_s or NyarHeadCode.i32_shr_u or
            NyarHeadCode.i32_not or
            NyarHeadCode.i32_eq or NyarHeadCode.i32_ne or
            NyarHeadCode.i32_lt_s or NyarHeadCode.i32_le_s or NyarHeadCode.i32_gt_s or NyarHeadCode.i32_ge_s or
            NyarHeadCode.i32_lt_u or NyarHeadCode.i32_le_u or NyarHeadCode.i32_gt_u or NyarHeadCode.i32_ge_u;
    }

    #endregion

    #region 栈映射计�?
    /// <summary>
    ///     计算每个 PC 位置的栈深度（PC �?栈深度映射）
    ///     使用工作列表算法处理控制�?    /// </summary>
    private static Dictionary<int, int>? compute_stack_map(IFunction function, byte[] bytecode, IModule module)
    {
        var startPc = function.code_offset;
        var endPc = startPc + function.code_length;
        var stackMap = new Dictionary<int, int>();
        var workList = new Queue<int>();

        stackMap[startPc] = 0;
        workList.Enqueue(startPc);

        while (workList.Count > 0)
        {
            var start = workList.Dequeue();
            var depth = stackMap[start];
            var pc = start;

            while (pc >= startPc && pc < endPc)
            {
                if (stackMap.TryGetValue(pc, out var existingDepth))
                {
                    if (existingDepth != depth) return null;

                    if (pc != start) break;
                }

                stackMap[pc] = depth;

                var opcode = (NyarHeadCode)bytecode[pc];
                var delta = get_stack_delta(opcode, bytecode, pc, module);
                depth += delta;

                if (depth < 0) return null;

                var nextPc = pc + get_instruction_size(opcode);

                if (opcode == NyarHeadCode.jump)
                {
                    var offset = BitConverter.ToInt32(bytecode, pc + 1);
                    var targetPc = pc + offset;

                    if (!stackMap.TryGetValue(targetPc, out var value))
                    {
                        stackMap[targetPc] = depth;
                        workList.Enqueue(targetPc);
                    }
                    else if (value != depth)
                    {
                        return null;
                    }

                    break;
                }

                if (opcode is NyarHeadCode.jump_if_true or NyarHeadCode.jump_if_false)
                {
                    var offset = BitConverter.ToInt32(bytecode, pc + 1);
                    var targetPc = pc + offset;

                    if (!stackMap.TryGetValue(targetPc, out var value))
                    {
                        stackMap[targetPc] = depth;
                        workList.Enqueue(targetPc);
                    }
                    else if (value != depth)
                    {
                        return null;
                    }

                    if (!stackMap.TryGetValue(nextPc, out var value1))
                    {
                        stackMap[nextPc] = depth;
                        workList.Enqueue(nextPc);
                    }
                    else if (value1 != depth)
                    {
                        return null;
                    }

                    break;
                }

                if (opcode == NyarHeadCode.@return) break;

                pc = nextPc;
            }
        }

        return stackMap;
    }

    /// <summary>
    ///     获取指令的栈深度变化�?    /// </summary>
    private static int get_stack_delta(NyarHeadCode headCode, byte[] bytecode, int pc, IModule module)
    {
        return headCode switch
        {
            NyarHeadCode.nop => 0,
            NyarHeadCode.@const => 1,
            NyarHeadCode.pop => -1,
            NyarHeadCode.dup => 1,
            NyarHeadCode.swap => 0,
            NyarHeadCode.load_local => 1,
            NyarHeadCode.store_local => -1,
            NyarHeadCode.load_arg => 1,
            NyarHeadCode.jump => 0,
            NyarHeadCode.jump_if_true => -1,
            NyarHeadCode.jump_if_false => -1,
            NyarHeadCode.@return => -1,
            NyarHeadCode.call or NyarHeadCode.tail_call => get_call_stack_delta(bytecode, pc, module),
            NyarHeadCode.i32_add or NyarHeadCode.i32_sub or NyarHeadCode.i32_mul or
                NyarHeadCode.i32_div_s or NyarHeadCode.i32_div_u or
                NyarHeadCode.i32_rem_s or NyarHeadCode.i32_rem_u or
                NyarHeadCode.i32_and or NyarHeadCode.i32_or or NyarHeadCode.i32_xor or
                NyarHeadCode.i32_shl or NyarHeadCode.i32_shr_s or NyarHeadCode.i32_shr_u or
                NyarHeadCode.i32_eq or NyarHeadCode.i32_ne or
                NyarHeadCode.i32_lt_s or NyarHeadCode.i32_le_s or NyarHeadCode.i32_gt_s or NyarHeadCode.i32_ge_s or
                NyarHeadCode.i32_lt_u or NyarHeadCode.i32_le_u or NyarHeadCode.i32_gt_u or NyarHeadCode.i32_ge_u or
                NyarHeadCode.i64_add or NyarHeadCode.i64_sub or NyarHeadCode.i64_mul or NyarHeadCode.i64_div_s or
                NyarHeadCode.f64_add or NyarHeadCode.f64_sub or NyarHeadCode.f64_mul or NyarHeadCode.f64_div or
                NyarHeadCode.f64_eq or NyarHeadCode.f64_ne or
                NyarHeadCode.f64_lt or NyarHeadCode.f64_le or NyarHeadCode.f64_gt or NyarHeadCode.f64_ge => -1,
            NyarHeadCode.i32_neg or NyarHeadCode.i32_not or NyarHeadCode.i64_neg or NyarHeadCode.f64_neg or
                NyarHeadCode.i32_extend_i64_s or NyarHeadCode.i32_to_f64_s or
                NyarHeadCode.i64_trunc_i32_s or NyarHeadCode.i64_to_f64 or
                NyarHeadCode.f64_to_i32 or NyarHeadCode.f64_to_i64 or
                NyarHeadCode.length or NyarHeadCode.access_static or NyarHeadCode.access_witness => 0,
            NyarHeadCode.new_object or NyarHeadCode.new_closure => 1,
            NyarHeadCode.array_push => -1,
            NyarHeadCode.array_get => -1,
            NyarHeadCode.array_set => -3,
            NyarHeadCode.get_field or NyarHeadCode.get_ordinal_index or NyarHeadCode.get_offset_index => -1,
            NyarHeadCode.set_field or NyarHeadCode.set_ordinal_index or NyarHeadCode.set_offset_index => -3,
            NyarHeadCode.get_upvalue => 0,
            NyarHeadCode.set_upvalue => -1,
            NyarHeadCode.alloc => 1,
            NyarHeadCode.free => -1,
            NyarHeadCode.i32_load => 0,
            NyarHeadCode.i32_store => -2,
            NyarHeadCode.i64_load => 0,
            NyarHeadCode.i64_store => -2,
            _ => 0
        };
    }

    /// <summary>
    ///     获取 Call 指令的栈深度变化量（1 - arity�?    /// </summary>
    private static int get_call_stack_delta(byte[] bytecode, int pc, IModule module)
    {
        var funcIndex = BitConverter.ToInt32(bytecode, pc + 1);
        if (funcIndex < 0 || funcIndex >= module.functions.Count) return 0;

        var targetFunc = module.functions[funcIndex];
        return 1 - targetFunc.arity;
    }

    #endregion

    #region IL 发射 �?Value 通用路径

    /// <summary>
    ///     发射 IL 并创�?DynamicMethod 委托（Value 接口�?    ///     支持 i32 + f64 混合类型函数
    /// </summary>
    private static Func<Value[], Value>? emit_method(
        int functionIndex,
        IFunction function,
        byte[] bytecode,
        IModule module,
        Dictionary<int, int> stackMap,
        int maxStack)
    {
        var startPc = function.code_offset;
        var endPc = startPc + function.code_length;

        var method = new DynamicMethod(
            $"jit_func_{functionIndex}",
            typeof(Value),
            [typeof(Value[])],
            typeof(JitEmitCompiler),
            true);

        var il = method.GetILGenerator();

        var totalLocals = function.arity + function.local_count;
        var localVars = new LocalBuilder[totalLocals];
        for (var i = 0; i < totalLocals; i++)
            localVars[i] = il.DeclareLocal(typeof(Value));

        var stackSlots = new LocalBuilder[maxStack + 2];
        for (var i = 0; i < stackSlots.Length; i++)
            stackSlots[i] = il.DeclareLocal(typeof(Value));

        var labels = new Dictionary<int, Label>();
        foreach (var pcOffset in stackMap.Keys)
            if (pcOffset >= startPc && pcOffset < endPc)
                labels[pcOffset] = il.DefineLabel();

        var returnLabel = il.DefineLabel();
        var returnValueLocal = il.DeclareLocal(typeof(Value));

        for (var i = 0; i < function.arity; i++)
        {
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldc_I4, i);
            il.Emit(OpCodes.Ldelem, typeof(Value));
            il.Emit(OpCodes.Stloc, localVars[i]);
        }

        var pc = startPc;
        while (pc < endPc)
        {
            if (labels.TryGetValue(pc, out var label)) il.MarkLabel(label);

            if (!stackMap.TryGetValue(pc, out var depth))
            {
                var opcode = (NyarHeadCode)bytecode[pc];
                pc += get_instruction_size(opcode);
                continue;
            }

            var op = (NyarHeadCode)bytecode[pc];

            emit_value_instruction(il, op, bytecode, pc, depth, module, localVars, stackSlots, labels, returnLabel,
                returnValueLocal, functionIndex);

            pc += get_instruction_size(op);
        }

        il.MarkLabel(returnLabel);
        il.Emit(OpCodes.Ldloc, returnValueLocal);
        il.Emit(OpCodes.Ret);

        return (Func<Value[], Value>)method.CreateDelegate(typeof(Func<Value[], Value>));
    }

    /// <summary>
    ///     发射单条指令�?IL（Value 通用路径，支�?i32 + f64 混合类型�?    /// </summary>
    private static void emit_value_instruction(
        ILGenerator il,
        NyarHeadCode headCode,
        byte[] bytecode,
        int pc,
        int depth,
        IModule module,
        LocalBuilder[] localVars,
        LocalBuilder[] stackSlots,
        Dictionary<int, Label> labels,
        Label returnLabel,
        LocalBuilder returnValueLocal,
        int functionIndex)
    {
        switch (headCode)
        {
            case NyarHeadCode.nop:
                break;

            case NyarHeadCode.@const:
            {
                var constIndex = BitConverter.ToInt32(bytecode, pc + 1);
                var constValue = module.constants[constIndex];

                if (constValue.type == ValueType.i32)
                {
                    il.Emit(OpCodes.Ldc_I4, constValue.i32);
                    il.Emit(OpCodes.Call, _value_from_int);
                }
                else if (constValue.type == ValueType.f64)
                {
                    il.Emit(OpCodes.Ldc_R8, constValue.f64);
                    il.Emit(OpCodes.Call, _value_from_double);
                }

                il.Emit(OpCodes.Stloc, stackSlots[depth]);
                break;
            }

            case NyarHeadCode.pop:
                break;

            case NyarHeadCode.dup:
                il.Emit(OpCodes.Ldloc, stackSlots[depth - 1]);
                il.Emit(OpCodes.Stloc, stackSlots[depth]);
                break;

            case NyarHeadCode.load_local:
            {
                var localIndex = BitConverter.ToInt32(bytecode, pc + 1);
                il.Emit(OpCodes.Ldloc, localVars[localIndex]);
                il.Emit(OpCodes.Stloc, stackSlots[depth]);
                break;
            }

            case NyarHeadCode.store_local:
            {
                var localIndex = BitConverter.ToInt32(bytecode, pc + 1);
                il.Emit(OpCodes.Ldloc, stackSlots[depth - 1]);
                il.Emit(OpCodes.Stloc, localVars[localIndex]);
                break;
            }

            case NyarHeadCode.load_arg:
            {
                var argIndex = BitConverter.ToInt32(bytecode, pc + 1);
                il.Emit(OpCodes.Ldloc, localVars[argIndex]);
                il.Emit(OpCodes.Stloc, stackSlots[depth]);
                break;
            }

            case NyarHeadCode.load_global:
            {
                var globalIndex = BitConverter.ToInt32(bytecode, pc + 1);
                il.Emit(OpCodes.Ldc_I4, globalIndex);
                il.Emit(OpCodes.Call, _jit_call_helper_load_global);
                il.Emit(OpCodes.Stloc, stackSlots[depth]);
                break;
            }

            case NyarHeadCode.store_global:
            {
                var globalIndex = BitConverter.ToInt32(bytecode, pc + 1);
                il.Emit(OpCodes.Ldc_I4, globalIndex);
                il.Emit(OpCodes.Ldloc, stackSlots[depth - 1]);
                il.Emit(OpCodes.Call, _jit_call_helper_store_global);
                break;
            }

            #region i32 算术（Value 路径：unbox �?计算 �?box�?
            case NyarHeadCode.i32_add:
                emit_value_i32_binary(il, stackSlots, depth, OpCodes.Add);
                break;

            case NyarHeadCode.i32_sub:
                emit_value_i32_binary(il, stackSlots, depth, OpCodes.Sub);
                break;

            case NyarHeadCode.i32_mul:
                emit_value_i32_binary(il, stackSlots, depth, OpCodes.Mul);
                break;

            case NyarHeadCode.i32_div_s:
                emit_value_i32_binary(il, stackSlots, depth, OpCodes.Div);
                break;

            case NyarHeadCode.i32_div_u:
                emit_value_i32_binary_unsigned_div(il, stackSlots, depth);
                break;

            case NyarHeadCode.i32_rem_s:
                emit_value_i32_binary(il, stackSlots, depth, OpCodes.Rem);
                break;

            case NyarHeadCode.i32_rem_u:
                emit_value_i32_binary_unsigned_rem(il, stackSlots, depth);
                break;

            case NyarHeadCode.i32_neg:
                il.Emit(OpCodes.Ldloc, stackSlots[depth - 1]);
                il.Emit(OpCodes.Call, _value_int_getter);
                il.Emit(OpCodes.Neg);
                il.Emit(OpCodes.Call, _value_from_int);
                il.Emit(OpCodes.Stloc, stackSlots[depth - 1]);
                break;

            case NyarHeadCode.i32_and:
                emit_value_i32_binary(il, stackSlots, depth, OpCodes.And);
                break;

            case NyarHeadCode.i32_or:
                emit_value_i32_binary(il, stackSlots, depth, OpCodes.Or);
                break;

            case NyarHeadCode.i32_xor:
                emit_value_i32_binary(il, stackSlots, depth, OpCodes.Xor);
                break;

            case NyarHeadCode.i32_shl:
                emit_value_i32_binary(il, stackSlots, depth, OpCodes.Shl);
                break;

            case NyarHeadCode.i32_shr_s:
                emit_value_i32_binary(il, stackSlots, depth, OpCodes.Shr);
                break;

            case NyarHeadCode.i32_shr_u:
                emit_value_i32_binary(il, stackSlots, depth, OpCodes.Shr_Un);
                break;

            case NyarHeadCode.i32_not:
                il.Emit(OpCodes.Ldloc, stackSlots[depth - 1]);
                il.Emit(OpCodes.Call, _value_int_getter);
                il.Emit(OpCodes.Not);
                il.Emit(OpCodes.Call, _value_from_int);
                il.Emit(OpCodes.Stloc, stackSlots[depth - 1]);
                break;

            #endregion

            #region i32 比较（Value 路径�?
            case NyarHeadCode.i32_eq:
                emit_value_i32_comparison(il, stackSlots, depth, OpCodes.Ceq);
                break;

            case NyarHeadCode.i32_ne:
                emit_value_i32_comparison_ne(il, stackSlots, depth);
                break;

            case NyarHeadCode.i32_lt_s:
                emit_value_i32_comparison(il, stackSlots, depth, OpCodes.Clt);
                break;

            case NyarHeadCode.i32_le_s:
                emit_value_i32_comparison_le(il, stackSlots, depth, false);
                break;

            case NyarHeadCode.i32_gt_s:
                emit_value_i32_comparison(il, stackSlots, depth, OpCodes.Cgt);
                break;

            case NyarHeadCode.i32_ge_s:
                emit_value_i32_comparison_ge(il, stackSlots, depth, false);
                break;

            case NyarHeadCode.i32_lt_u:
                emit_value_i32_comparison(il, stackSlots, depth, OpCodes.Clt_Un);
                break;

            case NyarHeadCode.i32_le_u:
                emit_value_i32_comparison_le(il, stackSlots, depth, true);
                break;

            case NyarHeadCode.i32_gt_u:
                emit_value_i32_comparison(il, stackSlots, depth, OpCodes.Cgt_Un);
                break;

            case NyarHeadCode.i32_ge_u:
                emit_value_i32_comparison_ge(il, stackSlots, depth, true);
                break;

            #endregion

            #region f64 算术（Value 路径：unbox �?计算 �?box�?
            case NyarHeadCode.f64_add:
                emit_value_f64_binary(il, stackSlots, depth, OpCodes.Add);
                break;

            case NyarHeadCode.f64_sub:
                emit_value_f64_binary(il, stackSlots, depth, OpCodes.Sub);
                break;

            case NyarHeadCode.f64_mul:
                emit_value_f64_binary(il, stackSlots, depth, OpCodes.Mul);
                break;

            case NyarHeadCode.f64_div:
                emit_value_f64_binary(il, stackSlots, depth, OpCodes.Div);
                break;

            case NyarHeadCode.f64_neg:
                il.Emit(OpCodes.Ldloc, stackSlots[depth - 1]);
                il.Emit(OpCodes.Call, _value_double_getter);
                il.Emit(OpCodes.Neg);
                il.Emit(OpCodes.Call, _value_from_double);
                il.Emit(OpCodes.Stloc, stackSlots[depth - 1]);
                break;

            case NyarHeadCode.f64_sqrt:
                il.Emit(OpCodes.Ldloc, stackSlots[depth - 1]);
                il.Emit(OpCodes.Call, _value_double_getter);
                il.Emit(OpCodes.Call, _math_sqrt);
                il.Emit(OpCodes.Call, _value_from_double);
                il.Emit(OpCodes.Stloc, stackSlots[depth - 1]);
                break;

            #endregion

            #region i64 算术（Value 路径：unbox long �?计算 �?box�?
            case NyarHeadCode.i64_add:
                emit_value_i64_binary(il, stackSlots, depth, OpCodes.Add);
                break;

            case NyarHeadCode.i64_sub:
                emit_value_i64_binary(il, stackSlots, depth, OpCodes.Sub);
                break;

            case NyarHeadCode.i64_mul:
                emit_value_i64_binary(il, stackSlots, depth, OpCodes.Mul);
                break;

            case NyarHeadCode.i64_div_s:
                emit_value_i64_binary(il, stackSlots, depth, OpCodes.Div);
                break;

            case NyarHeadCode.i64_div_u:
                emit_value_i64_binary_unsigned_div(il, stackSlots, depth);
                break;

            case NyarHeadCode.i64_rem_s:
                emit_value_i64_binary(il, stackSlots, depth, OpCodes.Rem);
                break;

            case NyarHeadCode.i64_rem_u:
                emit_value_i64_binary_unsigned_rem(il, stackSlots, depth);
                break;

            case NyarHeadCode.i64_neg:
                il.Emit(OpCodes.Ldloc, stackSlots[depth - 1]);
                il.Emit(OpCodes.Call, _value_long_getter);
                il.Emit(OpCodes.Neg);
                il.Emit(OpCodes.Call, _value_from_long);
                il.Emit(OpCodes.Stloc, stackSlots[depth - 1]);
                break;

            case NyarHeadCode.i64_and:
                emit_value_i64_binary(il, stackSlots, depth, OpCodes.And);
                break;

            case NyarHeadCode.i64_or:
                emit_value_i64_binary(il, stackSlots, depth, OpCodes.Or);
                break;

            case NyarHeadCode.i64_xor:
                emit_value_i64_binary(il, stackSlots, depth, OpCodes.Xor);
                break;

            case NyarHeadCode.i64_shl:
                emit_value_i64_shift(il, stackSlots, depth, OpCodes.Shl);
                break;

            case NyarHeadCode.i64_shr_s:
                emit_value_i64_shift(il, stackSlots, depth, OpCodes.Shr);
                break;

            case NyarHeadCode.i64_shr_u:
                emit_value_i64_shift(il, stackSlots, depth, OpCodes.Shr_Un);
                break;

            case NyarHeadCode.i64_not:
                il.Emit(OpCodes.Ldloc, stackSlots[depth - 1]);
                il.Emit(OpCodes.Call, _value_long_getter);
                il.Emit(OpCodes.Not);
                il.Emit(OpCodes.Call, _value_from_long);
                il.Emit(OpCodes.Stloc, stackSlots[depth - 1]);
                break;

            #endregion

            #region i64 比较（Value 路径：unbox long �?比较 �?box bool�?
            case NyarHeadCode.i64_eq:
                emit_value_i64_comparison(il, stackSlots, depth, OpCodes.Ceq);
                break;

            case NyarHeadCode.i64_ne:
                emit_value_i64_comparison_ne(il, stackSlots, depth);
                break;

            case NyarHeadCode.i64_lt_s:
                emit_value_i64_comparison(il, stackSlots, depth, OpCodes.Clt);
                break;

            case NyarHeadCode.i64_le_s:
                emit_value_i64_comparison_le(il, stackSlots, depth);
                break;

            case NyarHeadCode.i64_gt_s:
                emit_value_i64_comparison(il, stackSlots, depth, OpCodes.Cgt);
                break;

            case NyarHeadCode.i64_ge_s:
                emit_value_i64_comparison_ge(il, stackSlots, depth);
                break;

            #endregion

            #region 引用比较（Value 路径：调用辅助方法）

            case NyarHeadCode.ref_eq:
                emit_value_ref_comparison(il, stackSlots, depth, _jit_call_helper_ref_eq);
                break;

            case NyarHeadCode.ref_ne:
                emit_value_ref_comparison(il, stackSlots, depth, _jit_call_helper_ref_ne);
                break;

            #endregion

            #region f64 比较（Value 路径：unbox double �?比较 �?box bool�?
            case NyarHeadCode.f64_eq:
                emit_value_f64_comparison(il, stackSlots, depth, OpCodes.Ceq);
                break;

            case NyarHeadCode.f64_ne:
                emit_value_f64_comparison_ne(il, stackSlots, depth);
                break;

            case NyarHeadCode.f64_lt:
                emit_value_f64_comparison(il, stackSlots, depth, OpCodes.Clt);
                break;

            case NyarHeadCode.f64_le:
                emit_value_f64_comparison_le(il, stackSlots, depth);
                break;

            case NyarHeadCode.f64_gt:
                emit_value_f64_comparison(il, stackSlots, depth, OpCodes.Cgt);
                break;

            case NyarHeadCode.f64_ge:
                emit_value_f64_comparison_ge(il, stackSlots, depth);
                break;

            #endregion

            #region 类型转换（Value 路径：unbox �?转换 �?box�?
            case NyarHeadCode.i32_extend_i64_s:
                il.Emit(OpCodes.Ldloc, stackSlots[depth - 1]);
                il.Emit(OpCodes.Call, _value_int_getter);
                il.Emit(OpCodes.Conv_I8);
                il.Emit(OpCodes.Call, _value_from_long);
                il.Emit(OpCodes.Stloc, stackSlots[depth - 1]);
                break;

            case NyarHeadCode.i32_to_f64_s:
                il.Emit(OpCodes.Ldloc, stackSlots[depth - 1]);
                il.Emit(OpCodes.Call, _value_int_getter);
                il.Emit(OpCodes.Conv_R8);
                il.Emit(OpCodes.Call, _value_from_double);
                il.Emit(OpCodes.Stloc, stackSlots[depth - 1]);
                break;

            case NyarHeadCode.i64_trunc_i32_s:
                il.Emit(OpCodes.Ldloc, stackSlots[depth - 1]);
                il.Emit(OpCodes.Call, _value_long_getter);
                il.Emit(OpCodes.Conv_I4);
                il.Emit(OpCodes.Call, _value_from_int);
                il.Emit(OpCodes.Stloc, stackSlots[depth - 1]);
                break;

            case NyarHeadCode.i64_to_f64:
                il.Emit(OpCodes.Ldloc, stackSlots[depth - 1]);
                il.Emit(OpCodes.Call, _value_long_getter);
                il.Emit(OpCodes.Conv_R8);
                il.Emit(OpCodes.Call, _value_from_double);
                il.Emit(OpCodes.Stloc, stackSlots[depth - 1]);
                break;

            case NyarHeadCode.f64_to_i32:
                il.Emit(OpCodes.Ldloc, stackSlots[depth - 1]);
                il.Emit(OpCodes.Call, _value_double_getter);
                il.Emit(OpCodes.Conv_I4);
                il.Emit(OpCodes.Call, _value_from_int);
                il.Emit(OpCodes.Stloc, stackSlots[depth - 1]);
                break;

            case NyarHeadCode.f64_to_i64:
                il.Emit(OpCodes.Ldloc, stackSlots[depth - 1]);
                il.Emit(OpCodes.Call, _value_double_getter);
                il.Emit(OpCodes.Conv_I8);
                il.Emit(OpCodes.Call, _value_from_long);
                il.Emit(OpCodes.Stloc, stackSlots[depth - 1]);
                break;

            #endregion

            #region 栈操作扩�?
            case NyarHeadCode.swap:
            {
                var tempSlot = stackSlots[depth - 1];
                stackSlots[depth - 1] = stackSlots[depth - 2];
                stackSlots[depth - 2] = tempSlot;
                break;
            }

            #endregion

            #region 对象操作

            case NyarHeadCode.new_object:
            {
                var capacity = BitConverter.ToInt32(bytecode, pc + 1);
                il.Emit(OpCodes.Ldc_I4, capacity);
                il.Emit(OpCodes.Call, _jit_call_helper_new_object);
                il.Emit(OpCodes.Stloc, stackSlots[depth]);
                depth++;
                break;
            }

            case NyarHeadCode.get_field:
            {
                il.Emit(OpCodes.Ldloc, stackSlots[depth - 2]);
                il.Emit(OpCodes.Ldloc, stackSlots[depth - 1]);
                il.Emit(OpCodes.Call, _jit_call_helper_get_field);
                il.Emit(OpCodes.Stloc, stackSlots[depth - 2]);
                depth--;
                break;
            }

            case NyarHeadCode.set_field:
            {
                il.Emit(OpCodes.Ldloc, stackSlots[depth - 3]);
                il.Emit(OpCodes.Ldloc, stackSlots[depth - 2]);
                il.Emit(OpCodes.Ldloc, stackSlots[depth - 1]);
                il.Emit(OpCodes.Call, _jit_call_helper_set_field);
                depth -= 3;
                break;
            }

            case NyarHeadCode.array_get:
            {
                il.Emit(OpCodes.Ldloc, stackSlots[depth - 2]);
                il.Emit(OpCodes.Ldloc, stackSlots[depth - 1]);
                il.Emit(OpCodes.Call, _jit_call_helper_array_get);
                il.Emit(OpCodes.Stloc, stackSlots[depth - 2]);
                depth--;
                break;
            }

            case NyarHeadCode.array_set:
            {
                il.Emit(OpCodes.Ldloc, stackSlots[depth - 3]);
                il.Emit(OpCodes.Ldloc, stackSlots[depth - 2]);
                il.Emit(OpCodes.Ldloc, stackSlots[depth - 1]);
                il.Emit(OpCodes.Call, _jit_call_helper_array_set);
                depth -= 3;
                break;
            }

            case NyarHeadCode.get_ordinal_index:
            {
                il.Emit(OpCodes.Ldloc, stackSlots[depth - 2]);
                il.Emit(OpCodes.Ldloc, stackSlots[depth - 1]);
                il.Emit(OpCodes.Call, _jit_call_helper_get_ordinal_index);
                il.Emit(OpCodes.Stloc, stackSlots[depth - 2]);
                depth--;
                break;
            }

            case NyarHeadCode.get_offset_index:
            {
                il.Emit(OpCodes.Ldloc, stackSlots[depth - 2]);
                il.Emit(OpCodes.Ldloc, stackSlots[depth - 1]);
                il.Emit(OpCodes.Call, _jit_call_helper_get_index);
                il.Emit(OpCodes.Stloc, stackSlots[depth - 2]);
                depth--;
                break;
            }

            case NyarHeadCode.set_ordinal_index:
            {
                il.Emit(OpCodes.Ldloc, stackSlots[depth - 3]);
                il.Emit(OpCodes.Ldloc, stackSlots[depth - 2]);
                il.Emit(OpCodes.Ldloc, stackSlots[depth - 1]);
                il.Emit(OpCodes.Call, _jit_call_helper_set_ordinal_index);
                il.Emit(OpCodes.Stloc, stackSlots[depth - 3]);
                depth -= 2;
                break;
            }

            case NyarHeadCode.set_offset_index:
            {
                il.Emit(OpCodes.Ldloc, stackSlots[depth - 3]);
                il.Emit(OpCodes.Ldloc, stackSlots[depth - 2]);
                il.Emit(OpCodes.Ldloc, stackSlots[depth - 1]);
                il.Emit(OpCodes.Call, _jit_call_helper_set_index);
                il.Emit(OpCodes.Stloc, stackSlots[depth - 3]);
                depth -= 2;
                break;
            }

            case NyarHeadCode.length:
            {
                il.Emit(OpCodes.Ldloc, stackSlots[depth - 1]);
                il.Emit(OpCodes.Call, _jit_call_helper_length);
                il.Emit(OpCodes.Stloc, stackSlots[depth - 1]);
                break;
            }

            case NyarHeadCode.access_static:
            {
                var fieldOffset = BitConverter.ToInt32(bytecode, pc + 1);
                il.Emit(OpCodes.Ldloc, stackSlots[depth - 1]);
                il.Emit(OpCodes.Ldc_I4, fieldOffset);
                il.Emit(OpCodes.Call, _jit_call_helper_access_static);
                il.Emit(OpCodes.Stloc, stackSlots[depth - 1]);
                break;
            }

            case NyarHeadCode.access_witness:
            {
                var witnessOffset = BitConverter.ToInt32(bytecode, pc + 1);
                il.Emit(OpCodes.Ldloc, stackSlots[depth - 1]);
                il.Emit(OpCodes.Ldc_I4, witnessOffset);
                il.Emit(OpCodes.Call, _jit_call_helper_access_witness);
                il.Emit(OpCodes.Stloc, stackSlots[depth - 1]);
                break;
            }

            case NyarHeadCode.new_closure:
            {
                var funcIndex = BitConverter.ToInt32(bytecode, pc + 1);
                il.Emit(OpCodes.Ldc_I4, funcIndex);
                il.Emit(OpCodes.Call, _jit_call_helper_new_closure);
                il.Emit(OpCodes.Stloc, stackSlots[depth]);
                depth++;
                break;
            }

            case NyarHeadCode.get_upvalue:
            {
                var upvalueIndex = BitConverter.ToInt32(bytecode, pc + 1);
                il.Emit(OpCodes.Ldloc, stackSlots[depth - 1]);
                il.Emit(OpCodes.Ldc_I4, upvalueIndex);
                il.Emit(OpCodes.Call, _jit_call_helper_get_upvalue);
                il.Emit(OpCodes.Stloc, stackSlots[depth - 1]);
                break;
            }

            case NyarHeadCode.set_upvalue:
            {
                var upvalueIndex = BitConverter.ToInt32(bytecode, pc + 1);
                il.Emit(OpCodes.Ldloc, stackSlots[depth - 2]);
                il.Emit(OpCodes.Ldc_I4, upvalueIndex);
                il.Emit(OpCodes.Ldloc, stackSlots[depth - 1]);
                il.Emit(OpCodes.Call, _jit_call_helper_set_upvalue);
                il.Emit(OpCodes.Stloc, stackSlots[depth - 2]);
                depth--;
                break;
            }

            #endregion

            #region 内存操作

            case NyarHeadCode.alloc:
            {
                var size = BitConverter.ToInt32(bytecode, pc + 1);
                il.Emit(OpCodes.Ldc_I4, size);
                il.Emit(OpCodes.Call, _jit_call_helper_mem_alloc);
                il.Emit(OpCodes.Stloc, stackSlots[depth]);
                depth++;
                break;
            }

            case NyarHeadCode.free:
            {
                il.Emit(OpCodes.Ldloc, stackSlots[depth - 1]);
                il.Emit(OpCodes.Call, _jit_call_helper_mem_free);
                depth--;
                break;
            }

            case NyarHeadCode.i32_load:
            {
                var offset = BitConverter.ToInt32(bytecode, pc + 1);
                il.Emit(OpCodes.Ldloc, stackSlots[depth - 1]);
                il.Emit(OpCodes.Ldc_I4, offset);
                il.Emit(OpCodes.Call, _jit_call_helper_mem_i32_load);
                il.Emit(OpCodes.Stloc, stackSlots[depth - 1]);
                break;
            }

            case NyarHeadCode.i32_store:
            {
                il.Emit(OpCodes.Ldloc, stackSlots[depth - 2]);
                il.Emit(OpCodes.Ldc_I4, BitConverter.ToInt32(bytecode, pc + 1));
                il.Emit(OpCodes.Ldloc, stackSlots[depth - 1]);
                il.Emit(OpCodes.Call, _jit_call_helper_mem_i32_store);
                depth -= 2;
                break;
            }

            case NyarHeadCode.i64_load:
            {
                var offset = BitConverter.ToInt32(bytecode, pc + 1);
                il.Emit(OpCodes.Ldloc, stackSlots[depth - 1]);
                il.Emit(OpCodes.Ldc_I4, offset);
                il.Emit(OpCodes.Call, _jit_call_helper_mem_i64_load);
                il.Emit(OpCodes.Stloc, stackSlots[depth - 1]);
                break;
            }

            case NyarHeadCode.i64_store:
            {
                il.Emit(OpCodes.Ldloc, stackSlots[depth - 2]);
                il.Emit(OpCodes.Ldc_I4, BitConverter.ToInt32(bytecode, pc + 1));
                il.Emit(OpCodes.Ldloc, stackSlots[depth - 1]);
                il.Emit(OpCodes.Call, _jit_call_helper_mem_i64_store);
                depth -= 2;
                break;
            }

            #endregion

            #region 控制�?
            case NyarHeadCode.jump:
            {
                var offset = BitConverter.ToInt32(bytecode, pc + 1);
                var targetPc = pc + offset;
                il.Emit(OpCodes.Br, labels[targetPc]);
                break;
            }

            case NyarHeadCode.jump_if_true:
            {
                var offset = BitConverter.ToInt32(bytecode, pc + 1);
                var targetPc = pc + offset;
                il.Emit(OpCodes.Ldloc, stackSlots[depth - 1]);
                il.Emit(OpCodes.Call, _value_int_getter);
                il.Emit(OpCodes.Ldc_I4_0);
                il.Emit(OpCodes.Ceq);
                il.Emit(OpCodes.Ldc_I4_0);
                il.Emit(OpCodes.Ceq);
                il.Emit(OpCodes.Brtrue, labels[targetPc]);
                break;
            }

            case NyarHeadCode.jump_if_false:
            {
                var offset = BitConverter.ToInt32(bytecode, pc + 1);
                var targetPc = pc + offset;
                il.Emit(OpCodes.Ldloc, stackSlots[depth - 1]);
                il.Emit(OpCodes.Call, _value_int_getter);
                il.Emit(OpCodes.Ldc_I4_0);
                il.Emit(OpCodes.Ceq);
                il.Emit(OpCodes.Brtrue, labels[targetPc]);
                break;
            }

            case NyarHeadCode.call:
            case NyarHeadCode.call_static:
                emit_value_call(il, bytecode, pc, depth, module, stackSlots, functionIndex);
                break;

            case NyarHeadCode.@return:
                il.Emit(OpCodes.Ldloc, stackSlots[depth - 1]);
                il.Emit(OpCodes.Stloc, returnValueLocal);
                il.Emit(OpCodes.Br, returnLabel);
                break;

            #endregion
        }
    }

    /// <summary>
    ///     发射 Value 路径�?i32 二元算术操作
    /// </summary>
    private static void emit_value_i32_binary(ILGenerator il, LocalBuilder[] stackSlots, int depth, OpCode opCode)
    {
        il.Emit(OpCodes.Ldloc, stackSlots[depth - 2]);
        il.Emit(OpCodes.Call, _value_int_getter);
        il.Emit(OpCodes.Ldloc, stackSlots[depth - 1]);
        il.Emit(OpCodes.Call, _value_int_getter);
        il.Emit(opCode);
        il.Emit(OpCodes.Call, _value_from_int);
        il.Emit(OpCodes.Stloc, stackSlots[depth - 2]);
    }

    /// <summary>
    ///     发射 Value 路径�?i32 无符号除�?    /// </summary>
    private static void emit_value_i32_binary_unsigned_div(ILGenerator il, LocalBuilder[] stackSlots, int depth)
    {
        il.Emit(OpCodes.Ldloc, stackSlots[depth - 2]);
        il.Emit(OpCodes.Call, _value_int_getter);
        il.Emit(OpCodes.Conv_U4);
        il.Emit(OpCodes.Ldloc, stackSlots[depth - 1]);
        il.Emit(OpCodes.Call, _value_int_getter);
        il.Emit(OpCodes.Conv_U4);
        il.Emit(OpCodes.Div_Un);
        il.Emit(OpCodes.Call, _value_from_int);
        il.Emit(OpCodes.Stloc, stackSlots[depth - 2]);
    }

    /// <summary>
    ///     发射 Value 路径�?i32 无符号取�?    /// </summary>
    private static void emit_value_i32_binary_unsigned_rem(ILGenerator il, LocalBuilder[] stackSlots, int depth)
    {
        il.Emit(OpCodes.Ldloc, stackSlots[depth - 2]);
        il.Emit(OpCodes.Call, _value_int_getter);
        il.Emit(OpCodes.Conv_U4);
        il.Emit(OpCodes.Ldloc, stackSlots[depth - 1]);
        il.Emit(OpCodes.Call, _value_int_getter);
        il.Emit(OpCodes.Conv_U4);
        il.Emit(OpCodes.Rem_Un);
        il.Emit(OpCodes.Call, _value_from_int);
        il.Emit(OpCodes.Stloc, stackSlots[depth - 2]);
    }

    /// <summary>
    ///     发射 Value 路径�?i32 比较操作
    /// </summary>
    private static void emit_value_i32_comparison(ILGenerator il, LocalBuilder[] stackSlots, int depth, OpCode opCode)
    {
        il.Emit(OpCodes.Ldloc, stackSlots[depth - 2]);
        il.Emit(OpCodes.Call, _value_int_getter);
        il.Emit(OpCodes.Ldloc, stackSlots[depth - 1]);
        il.Emit(OpCodes.Call, _value_int_getter);
        il.Emit(opCode);
        il.Emit(OpCodes.Call, _value_from_bool);
        il.Emit(OpCodes.Stloc, stackSlots[depth - 2]);
    }

    /// <summary>
    ///     发射 Value 路径�?i32 不等比较
    /// </summary>
    private static void emit_value_i32_comparison_ne(ILGenerator il, LocalBuilder[] stackSlots, int depth)
    {
        il.Emit(OpCodes.Ldloc, stackSlots[depth - 2]);
        il.Emit(OpCodes.Call, _value_int_getter);
        il.Emit(OpCodes.Ldloc, stackSlots[depth - 1]);
        il.Emit(OpCodes.Call, _value_int_getter);
        il.Emit(OpCodes.Ceq);
        il.Emit(OpCodes.Ldc_I4_0);
        il.Emit(OpCodes.Ceq);
        il.Emit(OpCodes.Call, _value_from_bool);
        il.Emit(OpCodes.Stloc, stackSlots[depth - 2]);
    }

    /// <summary>
    ///     发射 Value 路径�?i32 小于等于比较
    /// </summary>
    private static void emit_value_i32_comparison_le(ILGenerator il, LocalBuilder[] stackSlots, int depth,
        bool unsigned)
    {
        il.Emit(OpCodes.Ldloc, stackSlots[depth - 2]);
        il.Emit(OpCodes.Call, _value_int_getter);
        il.Emit(OpCodes.Ldloc, stackSlots[depth - 1]);
        il.Emit(OpCodes.Call, _value_int_getter);
        il.Emit(unsigned ? OpCodes.Cgt_Un : OpCodes.Cgt);
        il.Emit(OpCodes.Ldc_I4_0);
        il.Emit(OpCodes.Ceq);
        il.Emit(OpCodes.Call, _value_from_bool);
        il.Emit(OpCodes.Stloc, stackSlots[depth - 2]);
    }

    /// <summary>
    ///     发射 Value 路径�?i32 大于等于比较
    /// </summary>
    private static void emit_value_i32_comparison_ge(ILGenerator il, LocalBuilder[] stackSlots, int depth,
        bool unsigned)
    {
        il.Emit(OpCodes.Ldloc, stackSlots[depth - 2]);
        il.Emit(OpCodes.Call, _value_int_getter);
        il.Emit(OpCodes.Ldloc, stackSlots[depth - 1]);
        il.Emit(OpCodes.Call, _value_int_getter);
        il.Emit(unsigned ? OpCodes.Clt_Un : OpCodes.Clt);
        il.Emit(OpCodes.Ldc_I4_0);
        il.Emit(OpCodes.Ceq);
        il.Emit(OpCodes.Call, _value_from_bool);
        il.Emit(OpCodes.Stloc, stackSlots[depth - 2]);
    }

    /// <summary>
    ///     发射 Value 路径�?f64 二元算术操作
    /// </summary>
    private static void emit_value_f64_binary(ILGenerator il, LocalBuilder[] stackSlots, int depth, OpCode opCode)
    {
        il.Emit(OpCodes.Ldloc, stackSlots[depth - 2]);
        il.Emit(OpCodes.Call, _value_double_getter);
        il.Emit(OpCodes.Ldloc, stackSlots[depth - 1]);
        il.Emit(OpCodes.Call, _value_double_getter);
        il.Emit(opCode);
        il.Emit(OpCodes.Call, _value_from_double);
        il.Emit(OpCodes.Stloc, stackSlots[depth - 2]);
    }

    /// <summary>
    ///     发射 Value 路径�?f64 比较操作
    /// </summary>
    private static void emit_value_f64_comparison(ILGenerator il, LocalBuilder[] stackSlots, int depth, OpCode opCode)
    {
        il.Emit(OpCodes.Ldloc, stackSlots[depth - 2]);
        il.Emit(OpCodes.Call, _value_double_getter);
        il.Emit(OpCodes.Ldloc, stackSlots[depth - 1]);
        il.Emit(OpCodes.Call, _value_double_getter);
        il.Emit(opCode);
        il.Emit(OpCodes.Call, _value_from_bool);
        il.Emit(OpCodes.Stloc, stackSlots[depth - 2]);
    }

    /// <summary>
    ///     发射 Value 路径�?f64 不等比较�?(a == b)�?    /// </summary>
    private static void emit_value_f64_comparison_ne(ILGenerator il, LocalBuilder[] stackSlots, int depth)
    {
        il.Emit(OpCodes.Ldloc, stackSlots[depth - 2]);
        il.Emit(OpCodes.Call, _value_double_getter);
        il.Emit(OpCodes.Ldloc, stackSlots[depth - 1]);
        il.Emit(OpCodes.Call, _value_double_getter);
        il.Emit(OpCodes.Ceq);
        il.Emit(OpCodes.Ldc_I4_0);
        il.Emit(OpCodes.Ceq);
        il.Emit(OpCodes.Call, _value_from_bool);
        il.Emit(OpCodes.Stloc, stackSlots[depth - 2]);
    }

    /// <summary>
    ///     发射 Value 路径�?f64 小于等于比较�?(a > b)�?    /// </summary>
    private static void emit_value_f64_comparison_le(ILGenerator il, LocalBuilder[] stackSlots, int depth)
    {
        il.Emit(OpCodes.Ldloc, stackSlots[depth - 2]);
        il.Emit(OpCodes.Call, _value_double_getter);
        il.Emit(OpCodes.Ldloc, stackSlots[depth - 1]);
        il.Emit(OpCodes.Call, _value_double_getter);
        il.Emit(OpCodes.Cgt);
        il.Emit(OpCodes.Ldc_I4_0);
        il.Emit(OpCodes.Ceq);
        il.Emit(OpCodes.Call, _value_from_bool);
        il.Emit(OpCodes.Stloc, stackSlots[depth - 2]);
    }

    /// <summary>
    ///     发射 Value 路径�?f64 大于等于比较�?(a < b)�?    /// </summary>
    private static void emit_value_f64_comparison_ge(ILGenerator il, LocalBuilder[] stackSlots, int depth)
    {
        il.Emit(OpCodes.Ldloc, stackSlots[depth - 2]);
        il.Emit(OpCodes.Call, _value_double_getter);
        il.Emit(OpCodes.Ldloc, stackSlots[depth - 1]);
        il.Emit(OpCodes.Call, _value_double_getter);
        il.Emit(OpCodes.Clt);
        il.Emit(OpCodes.Ldc_I4_0);
        il.Emit(OpCodes.Ceq);
        il.Emit(OpCodes.Call, _value_from_bool);
        il.Emit(OpCodes.Stloc, stackSlots[depth - 2]);
    }

    /// <summary>
    ///     发射 Value 路径�?i64 二元算术操作
    /// </summary>
    private static void emit_value_i64_binary(ILGenerator il, LocalBuilder[] stackSlots, int depth, OpCode opCode)
    {
        il.Emit(OpCodes.Ldloc, stackSlots[depth - 2]);
        il.Emit(OpCodes.Call, _value_long_getter);
        il.Emit(OpCodes.Ldloc, stackSlots[depth - 1]);
        il.Emit(OpCodes.Call, _value_long_getter);
        il.Emit(opCode);
        il.Emit(OpCodes.Call, _value_from_long);
        il.Emit(OpCodes.Stloc, stackSlots[depth - 2]);
    }

    /// <summary>
    ///     发射 Value 路径�?i64 无符号除法（转换�?ulong 除法再转�?long�?    /// </summary>
    private static void emit_value_i64_binary_unsigned_div(ILGenerator il, LocalBuilder[] stackSlots, int depth)
    {
        il.Emit(OpCodes.Ldloc, stackSlots[depth - 2]);
        il.Emit(OpCodes.Call, _value_long_getter);
        il.Emit(OpCodes.Ldloc, stackSlots[depth - 1]);
        il.Emit(OpCodes.Call, _value_long_getter);
        il.Emit(OpCodes.Div_Un);
        il.Emit(OpCodes.Call, _value_from_long);
        il.Emit(OpCodes.Stloc, stackSlots[depth - 2]);
    }

    /// <summary>
    ///     发射 Value 路径�?i64 无符号取�?    /// </summary>
    private static void emit_value_i64_binary_unsigned_rem(ILGenerator il, LocalBuilder[] stackSlots, int depth)
    {
        il.Emit(OpCodes.Ldloc, stackSlots[depth - 2]);
        il.Emit(OpCodes.Call, _value_long_getter);
        il.Emit(OpCodes.Ldloc, stackSlots[depth - 1]);
        il.Emit(OpCodes.Call, _value_long_getter);
        il.Emit(OpCodes.Rem_Un);
        il.Emit(OpCodes.Call, _value_from_long);
        il.Emit(OpCodes.Stloc, stackSlots[depth - 2]);
    }

    /// <summary>
    ///     发射 Value 路径�?i64 移位操作（第二个操作数为 int，需截断�?6 位）
    /// </summary>
    private static void emit_value_i64_shift(ILGenerator il, LocalBuilder[] stackSlots, int depth, OpCode opCode)
    {
        il.Emit(OpCodes.Ldloc, stackSlots[depth - 2]);
        il.Emit(OpCodes.Call, _value_long_getter);
        il.Emit(OpCodes.Ldloc, stackSlots[depth - 1]);
        il.Emit(OpCodes.Call, _value_long_getter);
        il.Emit(OpCodes.Conv_I4);
        il.Emit(opCode);
        il.Emit(OpCodes.Call, _value_from_long);
        il.Emit(OpCodes.Stloc, stackSlots[depth - 2]);
    }

    /// <summary>
    ///     发射 Value 路径�?i64 比较操作
    /// </summary>
    private static void emit_value_i64_comparison(ILGenerator il, LocalBuilder[] stackSlots, int depth, OpCode opCode)
    {
        il.Emit(OpCodes.Ldloc, stackSlots[depth - 2]);
        il.Emit(OpCodes.Call, _value_long_getter);
        il.Emit(OpCodes.Ldloc, stackSlots[depth - 1]);
        il.Emit(OpCodes.Call, _value_long_getter);
        il.Emit(opCode);
        il.Emit(OpCodes.Call, _value_from_bool);
        il.Emit(OpCodes.Stloc, stackSlots[depth - 2]);
    }

    /// <summary>
    ///     发射 Value 路径�?i64 不等比较
    /// </summary>
    private static void emit_value_i64_comparison_ne(ILGenerator il, LocalBuilder[] stackSlots, int depth)
    {
        il.Emit(OpCodes.Ldloc, stackSlots[depth - 2]);
        il.Emit(OpCodes.Call, _value_long_getter);
        il.Emit(OpCodes.Ldloc, stackSlots[depth - 1]);
        il.Emit(OpCodes.Call, _value_long_getter);
        il.Emit(OpCodes.Ceq);
        il.Emit(OpCodes.Ldc_I4_0);
        il.Emit(OpCodes.Ceq);
        il.Emit(OpCodes.Call, _value_from_bool);
        il.Emit(OpCodes.Stloc, stackSlots[depth - 2]);
    }

    /// <summary>
    ///     发射 Value 路径�?i64 小于等于比较�?(a > b)�?    /// </summary>
    private static void emit_value_i64_comparison_le(ILGenerator il, LocalBuilder[] stackSlots, int depth)
    {
        il.Emit(OpCodes.Ldloc, stackSlots[depth - 2]);
        il.Emit(OpCodes.Call, _value_long_getter);
        il.Emit(OpCodes.Ldloc, stackSlots[depth - 1]);
        il.Emit(OpCodes.Call, _value_long_getter);
        il.Emit(OpCodes.Cgt);
        il.Emit(OpCodes.Ldc_I4_0);
        il.Emit(OpCodes.Ceq);
        il.Emit(OpCodes.Call, _value_from_bool);
        il.Emit(OpCodes.Stloc, stackSlots[depth - 2]);
    }

    /// <summary>
    ///     发射 Value 路径�?i64 大于等于比较�?(a < b)�?    /// </summary>
    private static void emit_value_i64_comparison_ge(ILGenerator il, LocalBuilder[] stackSlots, int depth)
    {
        il.Emit(OpCodes.Ldloc, stackSlots[depth - 2]);
        il.Emit(OpCodes.Call, _value_long_getter);
        il.Emit(OpCodes.Ldloc, stackSlots[depth - 1]);
        il.Emit(OpCodes.Call, _value_long_getter);
        il.Emit(OpCodes.Clt);
        il.Emit(OpCodes.Ldc_I4_0);
        il.Emit(OpCodes.Ceq);
        il.Emit(OpCodes.Call, _value_from_bool);
        il.Emit(OpCodes.Stloc, stackSlots[depth - 2]);
    }

    private static void emit_value_ref_comparison(ILGenerator il, LocalBuilder[] stackSlots, int depth,
        MethodInfo helper)
    {
        il.Emit(OpCodes.Ldloc, stackSlots[depth - 2]);
        il.Emit(OpCodes.Ldloc, stackSlots[depth - 1]);
        il.Emit(OpCodes.Call, helper);
        il.Emit(OpCodes.Stloc, stackSlots[depth - 2]);
    }

    /// <summary>
    ///     发射 Value 路径的函数调�?    /// </summary>
    private static void emit_value_call(
        ILGenerator il,
        byte[] bytecode,
        int pc,
        int depth,
        IModule module,
        LocalBuilder[] stackSlots,
        int currentFunctionIndex)
    {
        var funcIndex = BitConverter.ToInt32(bytecode, pc + 1);
        var targetFunc = module.functions[funcIndex];
        var arity = targetFunc.arity;

        var argsLocal = il.DeclareLocal(typeof(Value[]));

        il.Emit(OpCodes.Ldc_I4, arity);
        il.Emit(OpCodes.Newarr, typeof(Value));
        il.Emit(OpCodes.Stloc, argsLocal);

        for (var i = 0; i < arity; i++)
        {
            il.Emit(OpCodes.Ldloc, argsLocal);
            il.Emit(OpCodes.Ldc_I4, i);
            il.Emit(OpCodes.Ldloc, stackSlots[depth - arity + i]);
            il.Emit(OpCodes.Stelem, typeof(Value));
        }

        il.Emit(OpCodes.Ldc_I4, funcIndex);
        il.Emit(OpCodes.Ldloc, argsLocal);
        il.Emit(OpCodes.Call, _jit_call_helper_call);
        il.Emit(OpCodes.Stloc, stackSlots[depth - arity]);
    }

    #endregion

    #region IL 发射 �?i32 专用路径

    /// <summary>
    ///     发射 IL 并创�?Func&lt;int,int&gt; DynamicMethod 委托
    ///     签名�?int �?int，入�?出口均无 NaN-Boxing 开销
    /// </summary>
    private static Func<int, int>? emit_int_int_method(
        int functionIndex,
        IFunction function,
        byte[] bytecode,
        IModule module,
        Dictionary<int, int> stackMap,
        int maxStack)
    {
        var startPc = function.code_offset;
        var endPc = startPc + function.code_length;

        var method = new DynamicMethod(
            $"jit_int_int_func_{functionIndex}",
            typeof(int),
            [typeof(int)],
            typeof(JitEmitCompiler),
            true);

        var il = method.GetILGenerator();

        var totalLocals = function.arity + function.local_count;
        var localVars = new LocalBuilder[totalLocals];
        for (var i = 0; i < totalLocals; i++)
            localVars[i] = il.DeclareLocal(typeof(int));

        var stackSlots = new LocalBuilder[maxStack + 2];
        for (var i = 0; i < stackSlots.Length; i++)
            stackSlots[i] = il.DeclareLocal(typeof(int));

        var labels = new Dictionary<int, Label>();
        foreach (var pcOffset in stackMap.Keys)
            if (pcOffset >= startPc && pcOffset < endPc)
                labels[pcOffset] = il.DefineLabel();

        var returnLabel = il.DefineLabel();
        var returnValueLocal = il.DeclareLocal(typeof(int));

        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Stloc, localVars[0]);

        var pc = startPc;
        while (pc < endPc)
        {
            if (labels.TryGetValue(pc, out var label)) il.MarkLabel(label);

            if (!stackMap.TryGetValue(pc, out var depth))
            {
                var opcode = (NyarHeadCode)bytecode[pc];
                pc += get_instruction_size(opcode);
                continue;
            }

            var op = (NyarHeadCode)bytecode[pc];

            emit_instruction(il, op, bytecode, pc, depth, module, localVars, stackSlots, labels, returnLabel,
                returnValueLocal, functionIndex);

            pc += get_instruction_size(op);
        }

        il.MarkLabel(returnLabel);
        il.Emit(OpCodes.Ldloc, returnValueLocal);
        il.Emit(OpCodes.Ret);

        return (Func<int, int>)method.CreateDelegate(typeof(Func<int, int>));
    }

    /// <summary>
    ///     发射单条指令�?IL（i32 专用路径，unboxed int 局部变量）
    /// </summary>
    private static void emit_instruction(
        ILGenerator il,
        NyarHeadCode headCode,
        byte[] bytecode,
        int pc,
        int depth,
        IModule module,
        LocalBuilder[] localVars,
        LocalBuilder[] stackSlots,
        Dictionary<int, Label> labels,
        Label returnLabel,
        LocalBuilder returnValueLocal,
        int functionIndex)
    {
        switch (headCode)
        {
            case NyarHeadCode.nop:
                break;

            case NyarHeadCode.@const:
            {
                var constIndex = BitConverter.ToInt32(bytecode, pc + 1);
                var constValue = module.constants[constIndex];
                il.Emit(OpCodes.Ldc_I4, constValue.i32);
                il.Emit(OpCodes.Stloc, stackSlots[depth]);
                break;
            }

            case NyarHeadCode.pop:
                break;

            case NyarHeadCode.dup:
                il.Emit(OpCodes.Ldloc, stackSlots[depth - 1]);
                il.Emit(OpCodes.Stloc, stackSlots[depth]);
                break;

            case NyarHeadCode.load_local:
            {
                var localIndex = BitConverter.ToInt32(bytecode, pc + 1);
                il.Emit(OpCodes.Ldloc, localVars[localIndex]);
                il.Emit(OpCodes.Stloc, stackSlots[depth]);
                break;
            }

            case NyarHeadCode.store_local:
            {
                var localIndex = BitConverter.ToInt32(bytecode, pc + 1);
                il.Emit(OpCodes.Ldloc, stackSlots[depth - 1]);
                il.Emit(OpCodes.Stloc, localVars[localIndex]);
                break;
            }

            case NyarHeadCode.load_arg:
            {
                var argIndex = BitConverter.ToInt32(bytecode, pc + 1);
                il.Emit(OpCodes.Ldloc, localVars[argIndex]);
                il.Emit(OpCodes.Stloc, stackSlots[depth]);
                break;
            }

            case NyarHeadCode.i32_add:
                emit_binary_op(il, stackSlots, depth, OpCodes.Add);
                break;

            case NyarHeadCode.i32_sub:
                emit_binary_op(il, stackSlots, depth, OpCodes.Sub);
                break;

            case NyarHeadCode.i32_mul:
                emit_binary_op(il, stackSlots, depth, OpCodes.Mul);
                break;

            case NyarHeadCode.i32_div_s:
                emit_binary_op(il, stackSlots, depth, OpCodes.Div);
                break;

            case NyarHeadCode.i32_div_u:
                emit_binary_op_unsigned_div(il, stackSlots, depth);
                break;

            case NyarHeadCode.i32_rem_s:
                emit_binary_op(il, stackSlots, depth, OpCodes.Rem);
                break;

            case NyarHeadCode.i32_rem_u:
                emit_binary_op_unsigned_rem(il, stackSlots, depth);
                break;

            case NyarHeadCode.i32_neg:
                il.Emit(OpCodes.Ldloc, stackSlots[depth - 1]);
                il.Emit(OpCodes.Neg);
                il.Emit(OpCodes.Stloc, stackSlots[depth - 1]);
                break;

            case NyarHeadCode.i32_and:
                emit_binary_op(il, stackSlots, depth, OpCodes.And);
                break;

            case NyarHeadCode.i32_or:
                emit_binary_op(il, stackSlots, depth, OpCodes.Or);
                break;

            case NyarHeadCode.i32_xor:
                emit_binary_op(il, stackSlots, depth, OpCodes.Xor);
                break;

            case NyarHeadCode.i32_shl:
                emit_shift_op(il, stackSlots, depth, OpCodes.Shl);
                break;

            case NyarHeadCode.i32_shr_s:
                emit_shift_op(il, stackSlots, depth, OpCodes.Shr);
                break;

            case NyarHeadCode.i32_shr_u:
                emit_shift_op(il, stackSlots, depth, OpCodes.Shr_Un);
                break;

            case NyarHeadCode.i32_not:
                il.Emit(OpCodes.Ldloc, stackSlots[depth - 1]);
                il.Emit(OpCodes.Not);
                il.Emit(OpCodes.Stloc, stackSlots[depth - 1]);
                break;

            case NyarHeadCode.i32_eq:
                emit_comparison(il, stackSlots, depth, OpCodes.Ceq);
                break;

            case NyarHeadCode.i32_ne:
                emit_comparison_ne(il, stackSlots, depth);
                break;

            case NyarHeadCode.i32_lt_s:
                emit_comparison(il, stackSlots, depth, OpCodes.Clt);
                break;

            case NyarHeadCode.i32_le_s:
                emit_comparison_le(il, stackSlots, depth, false);
                break;

            case NyarHeadCode.i32_gt_s:
                emit_comparison(il, stackSlots, depth, OpCodes.Cgt);
                break;

            case NyarHeadCode.i32_ge_s:
                emit_comparison_ge(il, stackSlots, depth, false);
                break;

            case NyarHeadCode.i32_lt_u:
                emit_comparison(il, stackSlots, depth, OpCodes.Clt_Un);
                break;

            case NyarHeadCode.i32_le_u:
                emit_comparison_le(il, stackSlots, depth, true);
                break;

            case NyarHeadCode.i32_gt_u:
                emit_comparison(il, stackSlots, depth, OpCodes.Cgt_Un);
                break;

            case NyarHeadCode.i32_ge_u:
                emit_comparison_ge(il, stackSlots, depth, true);
                break;

            case NyarHeadCode.jump:
            {
                var offset = BitConverter.ToInt32(bytecode, pc + 1);
                var targetPc = pc + offset;
                il.Emit(OpCodes.Br, labels[targetPc]);
                break;
            }

            case NyarHeadCode.jump_if_true:
            {
                var offset = BitConverter.ToInt32(bytecode, pc + 1);
                var targetPc = pc + offset;
                il.Emit(OpCodes.Ldloc, stackSlots[depth - 1]);
                il.Emit(OpCodes.Brtrue, labels[targetPc]);
                break;
            }

            case NyarHeadCode.jump_if_false:
            {
                var offset = BitConverter.ToInt32(bytecode, pc + 1);
                var targetPc = pc + offset;
                il.Emit(OpCodes.Ldloc, stackSlots[depth - 1]);
                il.Emit(OpCodes.Brfalse, labels[targetPc]);
                break;
            }

            case NyarHeadCode.call:
            case NyarHeadCode.call_static:
                emit_call(il, bytecode, pc, depth, module, stackSlots, functionIndex);
                break;

            case NyarHeadCode.@return:
                il.Emit(OpCodes.Ldloc, stackSlots[depth - 1]);
                il.Emit(OpCodes.Stloc, returnValueLocal);
                il.Emit(OpCodes.Br, returnLabel);
                break;
        }
    }

    /// <summary>
    ///     发射二元算术操作（Add/Sub/Mul/Div/Rem/And/Or/Xor�?    /// </summary>
    private static void emit_binary_op(ILGenerator il, LocalBuilder[] stackSlots, int depth, OpCode opCode)
    {
        il.Emit(OpCodes.Ldloc, stackSlots[depth - 2]);
        il.Emit(OpCodes.Ldloc, stackSlots[depth - 1]);
        il.Emit(opCode);
        il.Emit(OpCodes.Stloc, stackSlots[depth - 2]);
    }

    /// <summary>
    ///     发射移位操作（Shl/Shr/ShrUn�?    /// </summary>
    private static void emit_shift_op(ILGenerator il, LocalBuilder[] stackSlots, int depth, OpCode opCode)
    {
        il.Emit(OpCodes.Ldloc, stackSlots[depth - 2]);
        il.Emit(OpCodes.Ldloc, stackSlots[depth - 1]);
        il.Emit(opCode);
        il.Emit(OpCodes.Stloc, stackSlots[depth - 2]);
    }

    /// <summary>
    ///     发射无符号除�?    /// </summary>
    private static void emit_binary_op_unsigned_div(ILGenerator il, LocalBuilder[] stackSlots, int depth)
    {
        il.Emit(OpCodes.Ldloc, stackSlots[depth - 2]);
        il.Emit(OpCodes.Conv_U4);
        il.Emit(OpCodes.Ldloc, stackSlots[depth - 1]);
        il.Emit(OpCodes.Conv_U4);
        il.Emit(OpCodes.Div_Un);
        il.Emit(OpCodes.Stloc, stackSlots[depth - 2]);
    }

    /// <summary>
    ///     发射无符号取�?    /// </summary>
    private static void emit_binary_op_unsigned_rem(ILGenerator il, LocalBuilder[] stackSlots, int depth)
    {
        il.Emit(OpCodes.Ldloc, stackSlots[depth - 2]);
        il.Emit(OpCodes.Conv_U4);
        il.Emit(OpCodes.Ldloc, stackSlots[depth - 1]);
        il.Emit(OpCodes.Conv_U4);
        il.Emit(OpCodes.Rem_Un);
        il.Emit(OpCodes.Stloc, stackSlots[depth - 2]);
    }

    /// <summary>
    ///     发射比较操作（Ceq/Clt/Cgt/Clt_Un/Cgt_Un�?    /// </summary>
    private static void emit_comparison(ILGenerator il, LocalBuilder[] stackSlots, int depth, OpCode opCode)
    {
        il.Emit(OpCodes.Ldloc, stackSlots[depth - 2]);
        il.Emit(OpCodes.Ldloc, stackSlots[depth - 1]);
        il.Emit(opCode);
        il.Emit(OpCodes.Stloc, stackSlots[depth - 2]);
    }

    /// <summary>
    ///     发射不等比较（Ceq + Not�?    /// </summary>
    private static void emit_comparison_ne(ILGenerator il, LocalBuilder[] stackSlots, int depth)
    {
        il.Emit(OpCodes.Ldloc, stackSlots[depth - 2]);
        il.Emit(OpCodes.Ldloc, stackSlots[depth - 1]);
        il.Emit(OpCodes.Ceq);
        il.Emit(OpCodes.Ldc_I4_0);
        il.Emit(OpCodes.Ceq);
        il.Emit(OpCodes.Stloc, stackSlots[depth - 2]);
    }

    /// <summary>
    ///     发射小于等于比较（a <= b 等价�?!(a > b)�?    /// </summary>
    private static void emit_comparison_le(ILGenerator il, LocalBuilder[] stackSlots, int depth, bool unsigned)
    {
        il.Emit(OpCodes.Ldloc, stackSlots[depth - 2]);
        il.Emit(OpCodes.Ldloc, stackSlots[depth - 1]);
        il.Emit(unsigned ? OpCodes.Cgt_Un : OpCodes.Cgt);
        il.Emit(OpCodes.Ldc_I4_0);
        il.Emit(OpCodes.Ceq);
        il.Emit(OpCodes.Stloc, stackSlots[depth - 2]);
    }

    /// <summary>
    ///     发射大于等于比较（a >= b 等价�?!(a < b)�?    /// </summary>
    private static void emit_comparison_ge(ILGenerator il, LocalBuilder[] stackSlots, int depth, bool unsigned)
    {
        il.Emit(OpCodes.Ldloc, stackSlots[depth - 2]);
        il.Emit(OpCodes.Ldloc, stackSlots[depth - 1]);
        il.Emit(unsigned ? OpCodes.Clt_Un : OpCodes.Clt);
        il.Emit(OpCodes.Ldc_I4_0);
        il.Emit(OpCodes.Ceq);
        il.Emit(OpCodes.Stloc, stackSlots[depth - 2]);
    }

    /// <summary>
    ///     发射函数调用
    ///     arity=1 调用：使�?CallInt1 快速路径（消除数组分配�?NaN-Boxing�?    ///     arity>1 调用：使用通用 Call 路径（box 参数 �?JitCallHelper.Call �?unbox 结果�?    /// </summary>
    private static void emit_call(
        ILGenerator il,
        byte[] bytecode,
        int pc,
        int depth,
        IModule module,
        LocalBuilder[] stackSlots,
        int currentFunctionIndex)
    {
        var funcIndex = BitConverter.ToInt32(bytecode, pc + 1);
        var targetFunc = module.functions[funcIndex];
        var arity = targetFunc.arity;

        if (arity == 1)
        {
            il.Emit(OpCodes.Ldc_I4, funcIndex);
            il.Emit(OpCodes.Ldloc, stackSlots[depth - 1]);
            il.Emit(OpCodes.Call, _jit_call_helper_call_int1);
            il.Emit(OpCodes.Stloc, stackSlots[depth - 1]);
            return;
        }

        var argsLocal = il.DeclareLocal(typeof(Value[]));

        il.Emit(OpCodes.Ldc_I4, arity);
        il.Emit(OpCodes.Newarr, typeof(Value));
        il.Emit(OpCodes.Stloc, argsLocal);

        for (var i = 0; i < arity; i++)
        {
            il.Emit(OpCodes.Ldloc, argsLocal);
            il.Emit(OpCodes.Ldc_I4, i);
            il.Emit(OpCodes.Ldloc, stackSlots[depth - arity + i]);
            il.Emit(OpCodes.Call, _value_from_int);
            il.Emit(OpCodes.Stelem, typeof(Value));
        }

        il.Emit(OpCodes.Ldc_I4, funcIndex);
        il.Emit(OpCodes.Ldloc, argsLocal);
        il.Emit(OpCodes.Call, _jit_call_helper_call);
        il.Emit(OpCodes.Call, _value_int_getter);
        il.Emit(OpCodes.Stloc, stackSlots[depth - arity]);
    }

    #endregion

    #region IL 发射 �?f64 专用路径

    /// <summary>
    ///     发射 IL 并创�?Func&lt;double,double&gt; DynamicMethod 委托
    ///     签名�?double �?double，入�?出口均无 NaN-Boxing 开销
    /// </summary>
    private static Func<double, double>? emit_double_double_method(
        int functionIndex,
        IFunction function,
        byte[] bytecode,
        IModule module,
        Dictionary<int, int> stackMap,
        int maxStack)
    {
        var startPc = function.code_offset;
        var endPc = startPc + function.code_length;

        var method = new DynamicMethod(
            $"jit_double_double_func_{functionIndex}",
            typeof(double),
            [typeof(double)],
            typeof(JitEmitCompiler),
            true);

        var il = method.GetILGenerator();

        var totalLocals = function.arity + function.local_count;
        var localVars = new LocalBuilder[totalLocals];
        for (var i = 0; i < totalLocals; i++)
            localVars[i] = il.DeclareLocal(typeof(double));

        var stackSlots = new LocalBuilder[maxStack + 2];
        for (var i = 0; i < stackSlots.Length; i++)
            stackSlots[i] = il.DeclareLocal(typeof(double));

        var labels = new Dictionary<int, Label>();
        foreach (var pcOffset in stackMap.Keys)
            if (pcOffset >= startPc && pcOffset < endPc)
                labels[pcOffset] = il.DefineLabel();

        var returnLabel = il.DefineLabel();
        var returnValueLocal = il.DeclareLocal(typeof(double));

        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Stloc, localVars[0]);

        var pc = startPc;
        while (pc < endPc)
        {
            if (labels.TryGetValue(pc, out var label)) il.MarkLabel(label);

            if (!stackMap.TryGetValue(pc, out var depth))
            {
                var opcode = (NyarHeadCode)bytecode[pc];
                pc += get_instruction_size(opcode);
                continue;
            }

            var op = (NyarHeadCode)bytecode[pc];

            emit_f64_instruction(il, op, bytecode, pc, depth, module, localVars, stackSlots, labels, returnLabel,
                returnValueLocal, functionIndex);

            pc += get_instruction_size(op);
        }

        il.MarkLabel(returnLabel);
        il.Emit(OpCodes.Ldloc, returnValueLocal);
        il.Emit(OpCodes.Ret);

        return (Func<double, double>)method.CreateDelegate(typeof(Func<double, double>));
    }

    /// <summary>
    ///     发射单条指令�?IL（f64 专用路径，unboxed double 局部变量）
    /// </summary>
    private static void emit_f64_instruction(
        ILGenerator il,
        NyarHeadCode headCode,
        byte[] bytecode,
        int pc,
        int depth,
        IModule module,
        LocalBuilder[] localVars,
        LocalBuilder[] stackSlots,
        Dictionary<int, Label> labels,
        Label returnLabel,
        LocalBuilder returnValueLocal,
        int functionIndex)
    {
        switch (headCode)
        {
            case NyarHeadCode.nop:
                break;

            case NyarHeadCode.@const:
            {
                var constIndex = BitConverter.ToInt32(bytecode, pc + 1);
                var constValue = module.constants[constIndex];
                il.Emit(OpCodes.Ldc_R8, constValue.f64);
                il.Emit(OpCodes.Stloc, stackSlots[depth]);
                break;
            }

            case NyarHeadCode.pop:
                break;

            case NyarHeadCode.dup:
                il.Emit(OpCodes.Ldloc, stackSlots[depth - 1]);
                il.Emit(OpCodes.Stloc, stackSlots[depth]);
                break;

            case NyarHeadCode.load_local:
            {
                var localIndex = BitConverter.ToInt32(bytecode, pc + 1);
                il.Emit(OpCodes.Ldloc, localVars[localIndex]);
                il.Emit(OpCodes.Stloc, stackSlots[depth]);
                break;
            }

            case NyarHeadCode.store_local:
            {
                var localIndex = BitConverter.ToInt32(bytecode, pc + 1);
                il.Emit(OpCodes.Ldloc, stackSlots[depth - 1]);
                il.Emit(OpCodes.Stloc, localVars[localIndex]);
                break;
            }

            case NyarHeadCode.load_arg:
            {
                var argIndex = BitConverter.ToInt32(bytecode, pc + 1);
                il.Emit(OpCodes.Ldloc, localVars[argIndex]);
                il.Emit(OpCodes.Stloc, stackSlots[depth]);
                break;
            }

            case NyarHeadCode.f64_add:
                emit_binary_op(il, stackSlots, depth, OpCodes.Add);
                break;

            case NyarHeadCode.f64_sub:
                emit_binary_op(il, stackSlots, depth, OpCodes.Sub);
                break;

            case NyarHeadCode.f64_mul:
                emit_binary_op(il, stackSlots, depth, OpCodes.Mul);
                break;

            case NyarHeadCode.f64_div:
                emit_binary_op(il, stackSlots, depth, OpCodes.Div);
                break;

            case NyarHeadCode.f64_neg:
                il.Emit(OpCodes.Ldloc, stackSlots[depth - 1]);
                il.Emit(OpCodes.Neg);
                il.Emit(OpCodes.Stloc, stackSlots[depth - 1]);
                break;

            case NyarHeadCode.jump:
            {
                var offset = BitConverter.ToInt32(bytecode, pc + 1);
                var targetPc = pc + offset;
                il.Emit(OpCodes.Br, labels[targetPc]);
                break;
            }

            case NyarHeadCode.jump_if_true:
            {
                var offset = BitConverter.ToInt32(bytecode, pc + 1);
                var targetPc = pc + offset;
                il.Emit(OpCodes.Ldloc, stackSlots[depth - 1]);
                il.Emit(OpCodes.Ldc_R8, 0.0);
                il.Emit(OpCodes.Ceq);
                il.Emit(OpCodes.Ldc_I4_0);
                il.Emit(OpCodes.Ceq);
                il.Emit(OpCodes.Brtrue, labels[targetPc]);
                break;
            }

            case NyarHeadCode.jump_if_false:
            {
                var offset = BitConverter.ToInt32(bytecode, pc + 1);
                var targetPc = pc + offset;
                il.Emit(OpCodes.Ldloc, stackSlots[depth - 1]);
                il.Emit(OpCodes.Ldc_R8, 0.0);
                il.Emit(OpCodes.Ceq);
                il.Emit(OpCodes.Brtrue, labels[targetPc]);
                break;
            }

            case NyarHeadCode.call:
            case NyarHeadCode.call_static:
                emit_f64_call(il, bytecode, pc, depth, module, stackSlots, functionIndex);
                break;

            case NyarHeadCode.@return:
                il.Emit(OpCodes.Ldloc, stackSlots[depth - 1]);
                il.Emit(OpCodes.Stloc, returnValueLocal);
                il.Emit(OpCodes.Br, returnLabel);
                break;
        }
    }

    /// <summary>
    ///     发射 f64 专用路径的函数调�?    /// </summary>
    private static void emit_f64_call(
        ILGenerator il,
        byte[] bytecode,
        int pc,
        int depth,
        IModule module,
        LocalBuilder[] stackSlots,
        int currentFunctionIndex)
    {
        var funcIndex = BitConverter.ToInt32(bytecode, pc + 1);
        var targetFunc = module.functions[funcIndex];
        var arity = targetFunc.arity;

        if (arity == 1)
        {
            il.Emit(OpCodes.Ldc_I4, funcIndex);
            il.Emit(OpCodes.Ldloc, stackSlots[depth - 1]);
            il.Emit(OpCodes.Call, _value_from_double);
            il.Emit(OpCodes.Call, _jit_call_helper_call_double1);
            il.Emit(OpCodes.Call, _value_double_getter);
            il.Emit(OpCodes.Stloc, stackSlots[depth - 1]);
            return;
        }

        var argsLocal = il.DeclareLocal(typeof(Value[]));

        il.Emit(OpCodes.Ldc_I4, arity);
        il.Emit(OpCodes.Newarr, typeof(Value));
        il.Emit(OpCodes.Stloc, argsLocal);

        for (var i = 0; i < arity; i++)
        {
            il.Emit(OpCodes.Ldloc, argsLocal);
            il.Emit(OpCodes.Ldc_I4, i);
            il.Emit(OpCodes.Ldloc, stackSlots[depth - arity + i]);
            il.Emit(OpCodes.Call, _value_from_double);
            il.Emit(OpCodes.Stelem, typeof(Value));
        }

        il.Emit(OpCodes.Ldc_I4, funcIndex);
        il.Emit(OpCodes.Ldloc, argsLocal);
        il.Emit(OpCodes.Call, _jit_call_helper_call);
        il.Emit(OpCodes.Call, _value_double_getter);
        il.Emit(OpCodes.Stloc, stackSlots[depth - arity]);
    }

    #endregion

    #region 缓存反射

    private static readonly MethodInfo _value_from_int =
        typeof(Value).GetMethod(nameof(Value.from_int), [typeof(int)])!;

    private static readonly MethodInfo _value_from_double =
        typeof(Value).GetMethod(nameof(Value.from_double), [typeof(double)])!;

    private static readonly MethodInfo _value_from_bool =
        typeof(Value).GetMethod(nameof(Value.from_bool), [typeof(bool)])!;

    private static readonly MethodInfo _value_from_long =
        typeof(Value).GetMethod(nameof(Value.from_long), [typeof(long)])!;

    private static readonly MethodInfo _value_int_getter =
        typeof(Value).GetProperty(nameof(Value.i32))!.GetMethod!;

    private static readonly MethodInfo _value_double_getter =
        typeof(Value).GetProperty(nameof(Value.f64))!.GetMethod!;

    private static readonly MethodInfo _value_bool_getter =
        typeof(Value).GetProperty(nameof(Value.@bool))!.GetMethod!;

    private static readonly MethodInfo _value_long_getter =
        typeof(Value).GetProperty(nameof(Value.i64))!.GetMethod!;

    private static readonly MethodInfo _jit_call_helper_call =
        typeof(JitCallHelper).GetMethod(nameof(JitCallHelper.call), [typeof(int), typeof(Value[])])!;

    private static readonly MethodInfo _jit_call_helper_call_int1 =
        typeof(JitCallHelper).GetMethod(nameof(JitCallHelper.call_int1), [typeof(int), typeof(int)])!;

    private static readonly MethodInfo _jit_call_helper_call_double1 =
        typeof(JitCallHelper).GetMethod(nameof(JitCallHelper.call_double1), [typeof(int), typeof(double)])!;

    private static readonly MethodInfo _jit_call_helper_load_global =
        typeof(JitCallHelper).GetMethod(nameof(JitCallHelper.load_global), [typeof(int)])!;

    private static readonly MethodInfo _jit_call_helper_store_global =
        typeof(JitCallHelper).GetMethod(nameof(JitCallHelper.store_global), [typeof(int), typeof(Value)])!;

    private static readonly MethodInfo _jit_call_helper_new_object =
        typeof(JitCallHelper).GetMethod(nameof(JitCallHelper.new_object), [typeof(int)])!;

    private static readonly MethodInfo _jit_call_helper_get_field =
        typeof(JitCallHelper).GetMethod(nameof(JitCallHelper.get_field), [typeof(Value), typeof(Value)])!;

    private static readonly MethodInfo _jit_call_helper_set_field =
        typeof(JitCallHelper).GetMethod(nameof(JitCallHelper.set_field),
            [typeof(Value), typeof(Value), typeof(Value)])!;

    private static readonly MethodInfo _jit_call_helper_get_index =
        typeof(JitCallHelper).GetMethod(nameof(JitCallHelper.get_offset_index), [typeof(Value), typeof(Value)])!;

    private static readonly MethodInfo _jit_call_helper_array_get =
        typeof(JitCallHelper).GetMethod(nameof(JitCallHelper.array_get), [typeof(Value), typeof(Value)])!;

    private static readonly MethodInfo _jit_call_helper_get_ordinal_index =
        typeof(JitCallHelper).GetMethod(nameof(JitCallHelper.get_ordinal_index), [typeof(Value), typeof(Value)])!;

    private static readonly MethodInfo _jit_call_helper_set_index =
        typeof(JitCallHelper).GetMethod(nameof(JitCallHelper.set_offset_index),
            [typeof(Value), typeof(Value), typeof(Value)])!;

    private static readonly MethodInfo _jit_call_helper_array_set =
        typeof(JitCallHelper).GetMethod(nameof(JitCallHelper.array_set),
            [typeof(Value), typeof(Value), typeof(Value)])!;

    private static readonly MethodInfo _jit_call_helper_set_ordinal_index =
        typeof(JitCallHelper).GetMethod(nameof(JitCallHelper.set_ordinal_index),
            [typeof(Value), typeof(Value), typeof(Value)])!;

    private static readonly MethodInfo _jit_call_helper_length =
        typeof(JitCallHelper).GetMethod(nameof(JitCallHelper.length), [typeof(Value)])!;

    private static readonly MethodInfo _jit_call_helper_access_static =
        typeof(JitCallHelper).GetMethod(nameof(JitCallHelper.access_static), [typeof(Value), typeof(int)])!;

    private static readonly MethodInfo _jit_call_helper_access_witness =
        typeof(JitCallHelper).GetMethod(nameof(JitCallHelper.access_witness), [typeof(Value), typeof(int)])!;

    private static readonly MethodInfo _jit_call_helper_new_closure =
        typeof(JitCallHelper).GetMethod(nameof(JitCallHelper.new_closure), [typeof(int)])!;

    private static readonly MethodInfo _jit_call_helper_get_upvalue =
        typeof(JitCallHelper).GetMethod(nameof(JitCallHelper.get_upvalue), [typeof(Value), typeof(int)])!;

    private static readonly MethodInfo _jit_call_helper_set_upvalue =
        typeof(JitCallHelper).GetMethod(nameof(JitCallHelper.set_upvalue),
            [typeof(Value), typeof(int), typeof(Value)])!;

    private static readonly MethodInfo _jit_call_helper_mem_alloc =
        typeof(JitCallHelper).GetMethod(nameof(JitCallHelper.mem_alloc), [typeof(int)])!;

    private static readonly MethodInfo _jit_call_helper_mem_free =
        typeof(JitCallHelper).GetMethod(nameof(JitCallHelper.mem_free), [typeof(Value)])!;

    private static readonly MethodInfo _jit_call_helper_mem_i32_load =
        typeof(JitCallHelper).GetMethod(nameof(JitCallHelper.mem_i32_load), [typeof(Value), typeof(int)])!;

    private static readonly MethodInfo _jit_call_helper_mem_i32_store =
        typeof(JitCallHelper).GetMethod(nameof(JitCallHelper.mem_i32_store),
            [typeof(Value), typeof(int), typeof(Value)])!;

    private static readonly MethodInfo _jit_call_helper_mem_i64_load =
        typeof(JitCallHelper).GetMethod(nameof(JitCallHelper.mem_i64_load), [typeof(Value), typeof(int)])!;

    private static readonly MethodInfo _jit_call_helper_mem_i64_store =
        typeof(JitCallHelper).GetMethod(nameof(JitCallHelper.mem_i64_store),
            [typeof(Value), typeof(int), typeof(Value)])!;

    private static readonly MethodInfo _jit_call_helper_ref_eq =
        typeof(JitCallHelper).GetMethod(nameof(JitCallHelper.ref_eq), [typeof(Value), typeof(Value)])!;

    private static readonly MethodInfo _jit_call_helper_ref_ne =
        typeof(JitCallHelper).GetMethod(nameof(JitCallHelper.ref_ne), [typeof(Value), typeof(Value)])!;

    private static readonly MethodInfo _math_sqrt =
        typeof(Math).GetMethod(nameof(Math.Sqrt), [typeof(double)])!;

    private static readonly FieldInfo _self_recursive_target_field =
        typeof(JitCallHelper).GetField(nameof(JitCallHelper._self_recursive_target),
            BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)!;

    private static readonly MethodInfo _func_int_int_invoke =
        typeof(Func<int, int>).GetMethod("Invoke")!;

    #endregion
}
