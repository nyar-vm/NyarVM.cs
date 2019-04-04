using Nyar.Types;
using Std.Data.Binary.NyarIR.Data;
using RuntimeNyarFunction = Nyar.Types.NyarFunction;

namespace Nyar.VM.NyarVM.Bytecode;

/// <summary>
///     增强版字节码验证器，在基础格式验证之上增加栈深度验证、类型推断和资源限制检查�?///     防止非法指令、栈溢出、类型不匹配等安全问题�?/// </summary>
public sealed class EnhancedBytecodeValidator
{
    #region 模块结构验证

    private static bool validate_module_structure(NyarModule module, byte[] bytecode, ValidationOptions options,
        List<ValidationDiagnostic> diagnostics)
    {
        var valid = true;

        if (string.IsNullOrEmpty(module.name))
        {
            diagnostics.Add(new ValidationDiagnostic
            {
                level = Severity.error,
                code = "MOD001",
                message = "模块名称为空"
            });
            valid = false;
        }

        if (module.functions.Count > options.max_functions)
        {
            diagnostics.Add(new ValidationDiagnostic
            {
                level = Severity.error,
                code = "MOD002",
                message = $"函数数量 {module.functions.Count} 超过限制 {options.max_functions}"
            });
            valid = false;
        }

        if (module.constants.Count > options.max_constants)
        {
            diagnostics.Add(new ValidationDiagnostic
            {
                level = Severity.error,
                code = "MOD003",
                message = $"常量数量 {module.constants.Count} 超过限制 {options.max_constants}"
            });
            valid = false;
        }

        for (var i = 0; i < module.constants.Count; i++)
        {
            var constant = module.constants[i];
            if (constant is { is_object: true, @object: not null })
            {
                var objStr = constant.@object.ToString();
                if (objStr is { Length: > 1024 * 1024 })
                    diagnostics.Add(new ValidationDiagnostic
                    {
                        level = Severity.warning,
                        code = "MOD004",
                        message = $"常量索引 {i} 的字符串长度超过 1MB"
                    });
            }
        }

        return valid;
    }

    #endregion

    #region 指令验证

    private static bool validate_instructions(int funcIndex, RuntimeNyarFunction func, NyarModule module,
        byte[] bytecode,
        ValidationOptions options, List<ValidationDiagnostic> diagnostics)
    {
        var valid = true;
        var funcName = func.name ?? $"func_{funcIndex}";
        var end = func.code_offset + func.code_length;
        var instructionOffsets = new HashSet<int>();

        var stackDepth = func.arity;
        var maxStackDepth = 0;
        var allocationCount = 0;

        for (var pc = func.code_offset; pc < end;)
        {
            if (pc >= bytecode.Length)
            {
                diagnostics.Add(new ValidationDiagnostic
                {
                    level = Severity.error,
                    code = "INS001",
                    message = "PC 超出字节码范围",
                    function_name = funcName,
                    offset = pc
                });
                valid = false;
                break;
            }

            var op = bytecode[pc];
            var opcode = (NyarHeadCode)op;

            if (!Enum.IsDefined(typeof(NyarHeadCode), opcode))
            {
                diagnostics.Add(new ValidationDiagnostic
                {
                    level = Severity.error,
                    code = "INS002",
                    message = $"未定义的操作�?0x{op:X2}",
                    function_name = funcName,
                    offset = pc
                });
                valid = false;
                pc++;
                continue;
            }

            instructionOffsets.Add(pc);

            var instructionSize = get_instruction_size(opcode);
            if (instructionSize == 0)
            {
                diagnostics.Add(new ValidationDiagnostic
                {
                    level = Severity.error,
                    code = "INS003",
                    message = $"无效的指令长度，头码 0x{op:X2} 未被识别",
                    function_name = funcName,
                    offset = pc
                });
                valid = false;
                pc++;
                continue;
            }

            if (pc + instructionSize > end)
            {
                diagnostics.Add(new ValidationDiagnostic
                {
                    level = Severity.error,
                    code = "INS003",
                    message = $"指令跨越函数边界（需要 {instructionSize} 字节，剩余 {end - pc} 字节）",
                    function_name = funcName,
                    offset = pc
                });
                valid = false;
                break;
            }

            if (options.enable_stack_validation)
            {
                var delta = get_stack_delta(opcode, bytecode, pc, func.code_offset, module, out var operandError);
                if (operandError != null)
                {
                    diagnostics.Add(new ValidationDiagnostic
                    {
                        level = Severity.error,
                        code = "INS004",
                        message = operandError,
                        function_name = funcName,
                        offset = pc
                    });
                    valid = false;
                }

                stackDepth += delta;

                if (stackDepth < 0)
                {
                    diagnostics.Add(new ValidationDiagnostic
                    {
                        level = Severity.error,
                        code = "INS005",
                        message = $"栈下溢：栈深度变�?{stackDepth}",
                        function_name = funcName,
                        offset = pc
                    });
                    valid = false;
                    stackDepth = 0;
                }

                if (stackDepth > maxStackDepth) maxStackDepth = stackDepth;

                if (stackDepth > options.max_stack_depth)
                {
                    diagnostics.Add(new ValidationDiagnostic
                    {
                        level = Severity.error,
                        code = "INS006",
                        message = $"栈深�?{stackDepth} 超过限制 {options.max_stack_depth}",
                        function_name = funcName,
                        offset = pc
                    });
                    valid = false;
                }
            }

            if (opcode is NyarHeadCode.new_object or NyarHeadCode.alloc)
            {
                allocationCount++;
                if (allocationCount > options.max_allocations)
                    diagnostics.Add(new ValidationDiagnostic
                    {
                        level = Severity.warning,
                        code = "INS007",
                        message = $"对象分配次数 {allocationCount} 超过建议限制 {options.max_allocations}",
                        function_name = funcName,
                        offset = pc
                    });
            }

            if (opcode is NyarHeadCode.jump or NyarHeadCode.jump_if_true or NyarHeadCode.jump_if_false)
                if (pc + 5 <= end)
                {
                    var relativeOffset = BitConverter.ToInt32(bytecode, pc + 1);
                    var targetPc = pc + 5 + relativeOffset;

                    if (targetPc < func.code_offset || targetPc > end)
                    {
                        diagnostics.Add(new ValidationDiagnostic
                        {
                            level = Severity.error,
                            code = "INS008",
                            message = $"跳转目标 {targetPc} 超出函数范围 [{func.code_offset}, {end}]",
                            function_name = funcName,
                            offset = pc
                        });
                        valid = false;
                    }
                    else if (targetPc != end && !instructionOffsets.Contains(targetPc))
                    {
                        diagnostics.Add(new ValidationDiagnostic
                        {
                            level = Severity.error,
                            code = "INS009",
                            message = $"跳转目标 {targetPc} 未对齐到指令起始位置",
                            function_name = funcName,
                            offset = pc
                        });
                        valid = false;
                    }
                }

            if (opcode == NyarHeadCode.call)
                if (pc + 5 <= end)
                {
                    var calleeIndex = BitConverter.ToInt32(bytecode, pc + 1);
                    if (calleeIndex < 0 || calleeIndex >= module.functions.Count)
                    {
                        diagnostics.Add(new ValidationDiagnostic
                        {
                            level = Severity.error,
                            code = "INS010",
                            message = $"调用目标函数索引 {calleeIndex} 超出范围 [0, {module.functions.Count})",
                            function_name = funcName,
                            offset = pc
                        });
                        valid = false;
                    }
                }

            if (opcode == NyarHeadCode.@const)
                if (pc + 5 <= end)
                {
                    var constIndex = BitConverter.ToInt32(bytecode, pc + 1);
                    if (constIndex < 0 || constIndex >= module.constants.Count)
                    {
                        diagnostics.Add(new ValidationDiagnostic
                        {
                            level = Severity.error,
                            code = "INS011",
                            message = $"常量索引 {constIndex} 超出范围 [0, {module.constants.Count})",
                            function_name = funcName,
                            offset = pc
                        });
                        valid = false;
                    }
                }

            pc += instructionSize;
        }

        return valid;
    }

    #endregion

    #region 栈类型推�?
    /// <summary>
    ///     简化的栈值类型，用于类型推断
    /// </summary>
    private enum StackType
    {
        unknown,
        i32,
        i64,
        f32,
        f64,
        @ref,
        any
    }

    #endregion

    #region 常量

    /// <summary>
    ///     默认最大栈深度
    /// </summary>
    public const int default_max_stack_depth = 1024;

    /// <summary>
    ///     默认最大局部变量数
    /// </summary>
    public const int default_max_locals = 256;

    /// <summary>
    ///     默认最大函数数�?    /// </summary>
    public const int default_max_functions = 65536;

    /// <summary>
    ///     默认最大常量数�?    /// </summary>
    public const int default_max_constants = 65536;

    /// <summary>
    ///     默认最大调用深�?    /// </summary>
    public const int default_max_call_depth = 512;

    /// <summary>
    ///     默认最大对象分配数
    /// </summary>
    public const int default_max_allocations = 1_000_000;

    #endregion

    #region 验证结果类型

    /// <summary>
    ///     验证严重级别
    /// </summary>
    public enum Severity
    {
        /// <summary>
        ///     错误：验证失�?        /// </summary>
        error,


        /// <summary>
        ///     警告：可能有问题但不阻止执行
        /// </summary>
        warning
    }

    /// <summary>
    ///     验证诊断信息
    /// </summary>
    public sealed class ValidationDiagnostic
    {
        /// <summary>
        ///     严重级别
        /// </summary>
        public Severity level { get; init; }


        /// <summary>
        ///     诊断代码
        /// </summary>
        public string code { get; init; } = "";


        /// <summary>
        ///     诊断消息
        /// </summary>
        public string message { get; init; } = "";


        /// <summary>
        ///     相关函数名（可选）
        /// </summary>
        public string? function_name { get; init; }


        /// <summary>
        ///     相关字节码偏移（可选）
        /// </summary>
        public int? offset { get; init; }
    }

    /// <summary>
    ///     验证结果
    /// </summary>
    public sealed class ValidationResult
    {
        /// <summary>
        ///     是否通过验证
        /// </summary>
        public bool is_valid { get; init; }


        /// <summary>
        ///     诊断信息列表
        /// </summary>
        public List<ValidationDiagnostic> diagnostics { get; init; } = [];


        /// <summary>
        ///     错误数量
        /// </summary>
        public int error_count => diagnostics.Count(d => d.level == Severity.error);


        /// <summary>
        ///     警告数量
        /// </summary>
        public int warning_count => diagnostics.Count(d => d.level == Severity.warning);
    }

    /// <summary>
    ///     验证配置
    /// </summary>
    public sealed class ValidationOptions
    {
        /// <summary>
        ///     最大栈深度
        /// </summary>
        public int max_stack_depth { get; init; } = default_max_stack_depth;


        /// <summary>
        ///     最大局部变量数
        /// </summary>
        public int max_locals { get; init; } = default_max_locals;


        /// <summary>
        ///     最大函数数�?        /// </summary>
        public int max_functions { get; init; } = default_max_functions;


        /// <summary>
        ///     最大常量数�?        /// </summary>
        public int max_constants { get; init; } = default_max_constants;


        /// <summary>
        ///     最大调用深�?        /// </summary>
        public int max_call_depth { get; init; } = default_max_call_depth;


        /// <summary>
        ///     最大对象分配数
        /// </summary>
        public int max_allocations { get; init; } = default_max_allocations;


        /// <summary>
        ///     是否启用类型推断验证
        /// </summary>
        public bool enable_type_inference { get; init; } = true;


        /// <summary>
        ///     是否启用栈深度验�?        /// </summary>
        public bool enable_stack_validation { get; init; } = true;
    }

    #endregion

    #region 公有方法

    /// <summary>
    ///     验证模块代码字节流的安全性，使用默认配置。
    /// </summary>
    /// <param name="module">要验证的模块。</param>
    /// <param name="bytecode">原始代码字节流。</param>
    /// <returns>验证结果。</returns>
    public ValidationResult validate(NyarModule module, byte[] bytecode)
    {
        return validate(module, bytecode, new ValidationOptions());
    }

    /// <summary>
    ///     验证模块代码字节流的安全性，使用自定义配置。
    /// </summary>
    /// <param name="module">要验证的模块。</param>
    /// <param name="bytecode">原始代码字节流。</param>
    /// <param name="options">验证配置。</param>
    /// <returns>验证结果。</returns>
    public ValidationResult validate(NyarModule module, byte[] bytecode, ValidationOptions options)
    {
        var diagnostics = new List<ValidationDiagnostic>();
        var valid = true;

        valid = validate_module_structure(module, bytecode, options, diagnostics) && valid;
        valid = validate_all_functions(module, bytecode, options, diagnostics) && valid;

        return new ValidationResult
        {
            is_valid = valid,
            diagnostics = diagnostics
        };
    }

    #endregion

    #region 函数验证

    private static bool validate_all_functions(NyarModule module, byte[] bytecode, ValidationOptions options,
        List<ValidationDiagnostic> diagnostics)
    {
        var valid = true;

        for (var i = 0; i < module.functions.Count; i++)
        {
            var func = module.functions[i];
            if (!validate_function(i, func, module, bytecode, options, diagnostics)) valid = false;
        }

        return valid;
    }

    private static bool validate_function(int funcIndex, RuntimeNyarFunction func, NyarModule module, byte[] bytecode,
        ValidationOptions options, List<ValidationDiagnostic> diagnostics)
    {
        var valid = true;
        var funcName = func.name ?? $"func_{funcIndex}";

        if (string.IsNullOrEmpty(func.name))
            diagnostics.Add(new ValidationDiagnostic
            {
                level = Severity.warning,
                code = "FN001",
                message = $"函数索引 {funcIndex} 名称为空",
                function_name = funcName
            });

        if (func.arity < 0)
        {
            diagnostics.Add(new ValidationDiagnostic
            {
                level = Severity.error,
                code = "FN002",
                message = $"参数数量为负�?{func.arity}",
                function_name = funcName
            });
            valid = false;
        }

        if (func.local_count < 0)
        {
            diagnostics.Add(new ValidationDiagnostic
            {
                level = Severity.error,
                code = "FN003",
                message = $"局部变量数量为负数 {func.local_count}",
                function_name = funcName
            });
            valid = false;
        }

        if (func.local_count > options.max_locals)
        {
            diagnostics.Add(new ValidationDiagnostic
            {
                level = Severity.error,
                code = "FN004",
                message = $"局部变量数�?{func.local_count} 超过限制 {options.max_locals}",
                function_name = funcName
            });
            valid = false;
        }

        if (func.code_offset < 0 || func.code_offset >= bytecode.Length)
        {
            diagnostics.Add(new ValidationDiagnostic
            {
                level = Severity.error,
                code = "FN005",
                message = $"代码偏移 {func.code_offset} 超出字节码范围",
                function_name = funcName
            });
            return false;
        }

        if (func.code_offset + func.code_length > bytecode.Length)
        {
            diagnostics.Add(new ValidationDiagnostic
            {
                level = Severity.error,
                code = "FN006",
                message = $"代码范围 [{func.code_offset}, {func.code_offset + func.code_length}) 超出字节码长�?{bytecode.Length}",
                function_name = funcName
            });
            return false;
        }

        valid = validate_instructions(funcIndex, func, module, bytecode, options, diagnostics) && valid;

        return valid;
    }

    #endregion

    #region 指令大小和栈深度

    /// <summary>
    ///     获取指令大小。
    ///     统一复用 IR 侧的头码长度定义。
    /// </summary>
    private static int get_instruction_size(NyarHeadCode opcode)
    {
        return NyarInstruction.code_size(opcode);
    }

    /// <summary>
    ///     获取指令对栈深度的净影响
    /// </summary>
    private static int get_stack_delta(NyarHeadCode opcode, byte[] bytecode, int pc, int codeOffset, NyarModule module,
        out string? operandError)
    {
        operandError = null;

        switch (opcode)
        {
            case NyarHeadCode.nop:
                return 0;

            case NyarHeadCode.jump:
                return 0;

            case NyarHeadCode.jump_if_true:
            case NyarHeadCode.jump_if_false:
                return -1;

            case NyarHeadCode.call:
            {
                if (pc + 5 <= bytecode.Length)
                {
                    var calleeIndex = BitConverter.ToInt32(bytecode, pc + 1);
                    if (calleeIndex >= 0 && calleeIndex < module.functions.Count)
                    {
                        var callee = module.functions[calleeIndex];
                        return -callee.arity + 1;
                    }

                    operandError = $"无法确定调用目标的栈影响：函数索�?{calleeIndex} 超出范围";
                }

                return 0;
            }

            case NyarHeadCode.@return:
                return -1;

            case NyarHeadCode.tail_call:
            {
                if (pc + 5 <= bytecode.Length)
                {
                    var calleeIndex = BitConverter.ToInt32(bytecode, pc + 1);
                    if (calleeIndex >= 0 && calleeIndex < module.functions.Count)
                    {
                        var callee = module.functions[calleeIndex];
                        return -callee.arity;
                    }
                }

                return 0;
            }

            case NyarHeadCode.@throw:
                return -1;

            case NyarHeadCode.@catch:
                return 0;

            case NyarHeadCode.yield:
                return -1;

            case NyarHeadCode.resume:
                return 0;

            case NyarHeadCode.effect_handle:
            case NyarHeadCode.perform_effect:
            case NyarHeadCode.enter_effect_handler:
            case NyarHeadCode.exit_effect_handler:
            case NyarHeadCode.enter_try:
            case NyarHeadCode.exit_try:
                return 0;

            case NyarHeadCode.@const:
                return 1;

            case NyarHeadCode.pop:
                return -1;

            case NyarHeadCode.dup:
                return 1;

            case NyarHeadCode.swap:
                return 0;

            case NyarHeadCode.load_local:
            case NyarHeadCode.load_arg:
            case NyarHeadCode.load_global:
                return 1;

            case NyarHeadCode.store_local:
            case NyarHeadCode.store_arg:
            case NyarHeadCode.store_global:
                return -1;

            case NyarHeadCode.alloc:
                return 1;

            case NyarHeadCode.free:
                return -1;

            case NyarHeadCode.i32_load:
            case NyarHeadCode.i64_load:
                return 0;

            case NyarHeadCode.i32_store:
            case NyarHeadCode.i64_store:
                return -2;

            case NyarHeadCode.new_object:
                return 1;

            case NyarHeadCode.get_field:
                return 0;

            case NyarHeadCode.set_field:
                return -2;

            case NyarHeadCode.field_store:
                return -1;

            case NyarHeadCode.get_ordinal_index:
            case NyarHeadCode.get_offset_index:
                return -1;

            case NyarHeadCode.set_ordinal_index:
            case NyarHeadCode.set_offset_index:
                return -3;

            case NyarHeadCode.index_store:
                return -3;

            case NyarHeadCode.length:
                return 0;

            case NyarHeadCode.new_closure:
                return 1;

            case NyarHeadCode.get_upvalue:
                return 1;

            case NyarHeadCode.set_upvalue:
                return -1;

            case NyarHeadCode.call_intrinsic:
            case NyarHeadCode.call_native:
            case NyarHeadCode.builtin_call:
                return 0;

            case NyarHeadCode.load_native_lib:
                return -1;

            case NyarHeadCode.get_native_func:
                return 1;

            case NyarHeadCode.access_static:
            case NyarHeadCode.access_witness:
            case NyarHeadCode.access_dynamic:
                return 0;

            case NyarHeadCode.inline_cache_update:
                return -2;

            case NyarHeadCode.array_push:
                return -2;

            case NyarHeadCode.array_get:
                return -1;

            case NyarHeadCode.array_set:
                return -3;

            case NyarHeadCode.simd:
            {
                if (pc + 5 > bytecode.Length)
                {
                    operandError = "SIMD 指令缺少二级子操作码";
                    return 0;
                }

                var simdOpcode = (NyarSimdCode)BitConverter.ToInt32(bytecode, pc + 1);
                return get_simd_stack_delta(simdOpcode);
            }

            default:
                return get_arithmetic_stack_delta(opcode);
        }
    }

    private static int get_simd_stack_delta(NyarSimdCode opcode)
    {
        return opcode switch
        {
            NyarSimdCode.v128_const => 1,
            NyarSimdCode.v128_load => 0,
            NyarSimdCode.v128_store => -2,
            NyarSimdCode.i32_x4_add => -1,
            NyarSimdCode.i32_x4_sub => -1,
            NyarSimdCode.i32_x4_mul => -1,
            NyarSimdCode.f32_x4_add => -1,
            NyarSimdCode.f32_x4_sub => -1,
            NyarSimdCode.f32_x4_mul => -1,
            NyarSimdCode.i8_x16_splat => 0,
            NyarSimdCode.i16_x8_splat => 0,
            NyarSimdCode.i32_x4_splat => 0,
            NyarSimdCode.f32_x4_splat => 0,
            NyarSimdCode.f64_x2_splat => 0,
            NyarSimdCode.i8_x16_extract_lane_s => 0,
            NyarSimdCode.i8_x16_replace_lane => -1,
            NyarSimdCode.i32_x4_extract_lane => 0,
            NyarSimdCode.i32_x4_replace_lane => -1,
            NyarSimdCode.v128_and => -1,
            NyarSimdCode.v128_or => -1,
            NyarSimdCode.v128_xor => -1,
            NyarSimdCode.v128_not => 0,
            NyarSimdCode.v128_bit_select => -2,
            NyarSimdCode.i64_x2_add => -1,
            NyarSimdCode.i64_x2_sub => -1,
            NyarSimdCode.f64_x2_add => -1,
            NyarSimdCode.f64_x2_sub => -1,
            NyarSimdCode.f64_x2_mul => -1,
            NyarSimdCode.i8_x16_shuffle => -1,
            NyarSimdCode.i32_x4_eq => -1,
            NyarSimdCode.f32_x4_eq => -1,
            NyarSimdCode.v128_any_true => 0,
            _ => 0
        };
    }

    private static int get_arithmetic_stack_delta(NyarHeadCode opcode)
    {
        if (opcode is
            NyarHeadCode.i32_add or NyarHeadCode.i32_sub or NyarHeadCode.i32_mul or
            NyarHeadCode.i32_div_s or NyarHeadCode.i32_div_u or
            NyarHeadCode.i32_rem_s or NyarHeadCode.i32_rem_u or
            NyarHeadCode.i32_and or NyarHeadCode.i32_or or NyarHeadCode.i32_xor or
            NyarHeadCode.i32_shl or NyarHeadCode.i32_shr_s or NyarHeadCode.i32_shr_u or
            NyarHeadCode.i32_eq or NyarHeadCode.i32_ne or
            NyarHeadCode.i32_lt_s or NyarHeadCode.i32_lt_u or
            NyarHeadCode.i32_le_s or NyarHeadCode.i32_le_u or
            NyarHeadCode.i32_gt_s or NyarHeadCode.i32_gt_u or
            NyarHeadCode.i32_ge_s or NyarHeadCode.i32_ge_u or
            NyarHeadCode.i64_add or NyarHeadCode.i64_sub or NyarHeadCode.i64_mul or
            NyarHeadCode.i64_div_s or NyarHeadCode.i64_div_u or
            NyarHeadCode.i64_rem_s or NyarHeadCode.i64_rem_u or
            NyarHeadCode.i64_and or NyarHeadCode.i64_or or NyarHeadCode.i64_xor or
            NyarHeadCode.i64_shl or NyarHeadCode.i64_shr_s or NyarHeadCode.i64_shr_u or
            NyarHeadCode.i64_eq or NyarHeadCode.i64_ne or
            NyarHeadCode.i64_lt_s or NyarHeadCode.i64_lt_u or
            NyarHeadCode.i64_le_s or NyarHeadCode.i64_le_u or
            NyarHeadCode.i64_gt_s or NyarHeadCode.i64_gt_u or
            NyarHeadCode.i64_ge_s or NyarHeadCode.i64_ge_u or
            NyarHeadCode.ref_eq or NyarHeadCode.ref_ne or
            NyarHeadCode.f32_add or NyarHeadCode.f32_sub or NyarHeadCode.f32_mul or NyarHeadCode.f32_div or
            NyarHeadCode.f64_add or NyarHeadCode.f64_sub or NyarHeadCode.f64_mul or NyarHeadCode.f64_div or
            NyarHeadCode.f64_eq or NyarHeadCode.f64_ne or
            NyarHeadCode.f64_lt or NyarHeadCode.f64_le or NyarHeadCode.f64_gt or NyarHeadCode.f64_ge or
            NyarHeadCode.utf8_concat or NyarHeadCode.utf8_eq or NyarHeadCode.utf8_ne or
            NyarHeadCode.big_int_add or NyarHeadCode.big_int_sub or NyarHeadCode.big_int_mul)
            return -1;

        if (opcode is
            NyarHeadCode.i32_neg or NyarHeadCode.i32_not or
            NyarHeadCode.i64_neg or NyarHeadCode.i64_not or
            NyarHeadCode.f32_neg or
            NyarHeadCode.f64_neg or NyarHeadCode.f64_sqrt or
            NyarHeadCode.i32_extend_i64_s or NyarHeadCode.i32_extend_i64_u or
            NyarHeadCode.i64_trunc_i32_s or NyarHeadCode.i64_trunc_i32_u or
            NyarHeadCode.i32_to_f32_s or NyarHeadCode.i32_to_f64_s or
            NyarHeadCode.i64_to_f64 or NyarHeadCode.f64_to_i32 or NyarHeadCode.f64_to_i64 or
            NyarHeadCode.any_to_i32 or NyarHeadCode.any_to_utf8 or
            NyarHeadCode.utf8_len_bytes or NyarHeadCode.utf8_len_chars)
            return 0;

        if (opcode == NyarHeadCode.utf8_substr)
        {
            return -2;
        }

        return 0;
    }

    #endregion
}
