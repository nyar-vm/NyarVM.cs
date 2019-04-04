using Std.Data.Binary.Frame;
using Std.Data.Binary.Jvm.Data;
using Std.Data.Binary.NyarIR.Data;
using static Nyar.Assembler.Backends.Jvm.JvmConstantPoolHelper;
using static Nyar.Assembler.Backends.Jvm.JvmTypeMap;

namespace Nyar.Assembler.Backends.Jvm;

/// <summary>
///     <see cref="JvmBackend" /> 的 partial 文件，承载 Class 文件级构建逻辑：
///     常量池构建、可达函数迭代、JVM 方法构建、Java 入口包装。
/// </summary>
public partial class JvmBackend
{
    private static JvmClassFileData build_class_file(GenerateModule module, string className)
    {
        var constantPool = new List<JvmConstant>();
        var methods = new List<JvmMethodInfo>();
        var bootstrapMethods = new List<JvmBootstrapMethod>();

        var thisClassIndex = add_class(constantPool, className);
        var superClassIndex = add_class(constantPool, "java/lang/Object");

        // 构建 JVM 外部链接表，统一收集 [jvm]、[import("jvm", ...)]、[clr]、[wasm] 等外部函数属性
        var linkTable = JvmExternalLinkTable.build(module);

        // 死代码消除：只生成从入口函数可达的函数
        var reachableIndices = linkTable.collect_reachable_functions(module);

        // 收集所有 load_global 指令引用的全局变量名，用于生成静态字段声明
        var globalVarNames = new HashSet<string>();
        foreach (var function in module.functions)
        foreach (var instr in function.instructions)
            if (instr is { head_code: NyarHeadCode.load_global, operands.Count: > 0 } &&
                instr.operands[0] is GenerateOperand.Str globalStr)
            {
                var fieldName = globalStr.value.Replace('.', '_');
                globalVarNames.Add(fieldName);
            }

        // DEBUG: 诊断链式方法 DCE 问题
        if (className.Contains("legion_tools", StringComparison.Ordinal))
        {
            Console.Error.WriteLine($"\n=== DEBUG: 模块 {className} 函数列表 ===");
            for (var i = 0; i < module.functions.Count; i++)
            {
                var f = module.functions[i];
                var reachable = reachableIndices.Contains(i) ? "REACHABLE" : "DEAD";
                Console.Error.WriteLine(
                    $"  [{i:D3}] {reachable} {f.name}({string.Join(",", f.parameters.Select(p => p.type_ref))}) -> {f.return_type_ref}");
                // 查找 call/call_static 指令
                foreach (var instr in f.instructions)
                    if (instr.head_code is NyarHeadCode.call or NyarHeadCode.call_static)
                        if (instr.operands.FirstOrDefault() is GenerateOperand.FuncRef fr)
                            Console.Error.WriteLine(
                                $"         CALL: {fr.name}({string.Join(",", fr.signature.parameters)}) -> {string.Join(",", fr.signature.results)}");
            }

            Console.Error.WriteLine("=== END DEBUG ===\n");
        }

        // 记录是否已经生成了 Java main 入口
        var hasJavaMain = false;

        for (var i = 0; i < module.functions.Count; i++)
        {
            // 跳过不可达函数（死代码消除）
            if (!reachableIndices.Contains(i)) continue;

            var function = module.functions[i];

            // 跳过 [jvm] 属性外部函数（不生成 JVM 方法体，通过 linkTable.jvm_external_refs 在调用点解析）
            if (linkTable.jvm_attributed_names.Contains(function.name)) continue;

            // 跳过仅 [clr] 和 [wasm] 外部函数（不生成 JVM 方法体）
            if (linkTable.other_external_names.Contains(function.name)) continue;

            var method = build_method(function, constantPool, module.constants.strings, bootstrapMethods, className,
                linkTable, module.functions);
            methods.Add(method);

            if (method.name_index != 0 && get_utf8(constantPool, method.name_index) == "main" &&
                get_utf8(constantPool, method.descriptor_index) == "([Ljava/lang/String;)V")
                hasJavaMain = true;
        }

        // 如果没有 main 入口，检查导出项
        if (!hasJavaMain)
        {
            var mainExport = module.exports.FirstOrDefault(e =>
                e.kind == GenerateExportKind.function && (e.name == "main" || e.name == module.name));
            if (mainExport is null)
                mainExport = module.exports.FirstOrDefault(e => e.kind == GenerateExportKind.function);

            var entryFunc = mainExport != null
                ? module.functions[mainExport.function_index]
                : module.functions.FirstOrDefault(f => f.name == module.name) ??
                  module.functions.FirstOrDefault(f => f.name == "main") ??
                  module.functions.FirstOrDefault(); // 实在找不到，用第一个
            if (entryFunc != null)
            {
                var mainMethod = build_java_main_wrapper(entryFunc, constantPool, className);
                methods.Add(mainMethod);
            }
        }

        var accessFlags = (ushort)(0x0001 | 0x0020); // ACC_PUBLIC | ACC_SUPER

        // 生成全局变量对应的静态字段声明
        var fields = new List<JvmFieldInfo>();
        var hashMapDescriptorIdx = add_utf8(constantPool, "Ljava/util/HashMap;");
        foreach (var globalName in globalVarNames)
        {
            var nameIdx = add_utf8(constantPool, globalName);
            fields.Add(new JvmFieldInfo
            {
                access_flags = 0x0001 | 0x0008, // ACC_PUBLIC | ACC_STATIC
                name_index = nameIdx,
                descriptor_index = hashMapDescriptorIdx,
                attributes = []
            });
        }

        // 生成 <clinit> 初始化器，为每个全局变量创建空 HashMap
        if (globalVarNames.Count > 0)
        {
            var clinitMethod = build_clinit_method(constantPool, className, globalVarNames);
            methods.Add(clinitMethod);
        }

        var attributes = new List<JvmAttributeInfo>();

        if (bootstrapMethods.Count > 0)
        {
            var bsmAttrNameIdx = add_utf8(constantPool, "BootstrapMethods");
            attributes.Add(new JvmBootstrapMethodsAttribute
            {
                attribute_name_index = bsmAttrNameIdx,
                attribute_length =
                    (uint)(2 + bootstrapMethods.Count * 4) // 简化计算                BootstrapMethods = bootstrapMethods
            });
        }

        return new JvmClassFileData
        {
            magic = 0xCAFEBABE,
            minor_version = 0,
            major_version = 49, // Java 5 — 使用旧版类型推断验证器，不强制要求 StackMapTable
            constant_pool = constantPool,
            access_flags = accessFlags,
            this_class = thisClassIndex,
            super_class = superClassIndex,
            interfaces = [],
            fields = fields,
            methods = methods,
            attributes = attributes
        };
    }

    /// <summary>
    ///     构建 &lt;clinit&gt; 方法，为每个全局变量创建空 HashMap 并存储到静态字段。
    ///     全局变量在运行时由各模块的初始化逻辑填充字段值。
    /// </summary>
    private static JvmMethodInfo build_clinit_method(List<JvmConstant> constantPool, string className,
        IEnumerable<string> globalVarNames)
    {
        var nameIdx = add_utf8(constantPool, "<clinit>");
        var descIdx = add_utf8(constantPool, "()V");
        var codeAttrNameIdx = add_utf8(constantPool, "Code");

        var writer = new ByteBufferWriter(1024);
        var hashMapClassIdx = add_class(constantPool, "java/util/HashMap");
        var hashMapInitIdx = add_method_ref(constantPool, "java/util/HashMap", "<init>", "()V");

        var maxStack = 2; // new + dup 需要栈深 2
        foreach (var globalName in globalVarNames)
        {
            // new HashMap
            writer.write_u8((byte)JvmOpcode.@new);
            writer.write_u16_be(hashMapClassIdx);
            // dup
            writer.write_u8((byte)JvmOpcode.dup);
            // invokespecial HashMap.<init>()V
            writer.write_u8((byte)JvmOpcode.invokespecial);
            writer.write_u16_be(hashMapInitIdx);
            // putstatic legion_tools.globalName : HashMap
            var fieldIdx = add_field_ref(constantPool, className, globalName, "Ljava/util/HashMap;");
            writer.write_u8((byte)JvmOpcode.putstatic);
            writer.write_u16_be(fieldIdx);
        }

        // return
        writer.write_u8((byte)JvmOpcode.@return);

        var code = writer.to_array();
        var codeAttr = new JvmCodeAttribute
        {
            attribute_name_index = codeAttrNameIdx,
            attribute_length = (uint)(12 + code.Length),
            max_stack = (ushort)maxStack,
            max_locals = 0,
            code_length = (uint)code.Length,
            code = code,
            exception_table_length = 0,
            exception_table = [],
            attributes_count = 0,
            attributes = []
        };

        return new JvmMethodInfo
        {
            access_flags = 0x0008, // ACC_STATIC
            name_index = nameIdx,
            descriptor_index = descIdx,
            attributes = [codeAttr]
        };
    }

    private static JvmMethodInfo build_java_main_wrapper(GenerateFunction entryFunc, List<JvmConstant> constantPool,
        string className)
    {
        var nameIdx = add_utf8(constantPool, "main");
        var descIdx = add_utf8(constantPool, "([Ljava/lang/String;)V");
        var codeAttrNameIdx = add_utf8(constantPool, "Code");

        var writer = new ByteBufferWriter(1024);

        // 为入口函数的每个参数压入默认值
        var parameterTypes = entryFunc.parameters.Select(p => to_jvm_type_name(p.type_ref)).ToList();
        var maxStack = 0;
        for (var i = 0; i < parameterTypes.Count; i++)
        {
            var paramType = parameterTypes[i];
            // 如果入口函数只有一个参数且为引用类型，传递 main 的 String[] args（局部变量 0）
            if (parameterTypes.Count == 1 && paramType == "java/lang/Object")
            {
                writer.write_u8((byte)JvmOpcode.aload0);
                maxStack = 1;
            }
            else
            {
                emit_default_value(ref writer, paramType, constantPool);
                maxStack += is_wide_type(paramType) ? 2 : 1;
            }
        }

        // 调用 entryFunc
        var returnType = to_jvm_type_name(entryFunc.return_type_ref);
        var methodDesc = to_jvm_method_descriptor(entryFunc.return_type_ref, entryFunc.parameters.Select(p => p.type_ref));
        var methodRefIdx = add_method_ref(constantPool, className, sanitize_method_name(entryFunc.name), methodDesc);

        writer.write_u8((byte)JvmOpcode.invokestatic);
        writer.write_u16_be(methodRefIdx);

        if (returnType != "void")
        {
            if (returnType is "long" or "double")
            {
                writer.write_u8((byte)JvmOpcode.pop2);
                maxStack = Math.Max(maxStack, 2);
            }
            else
            {
                writer.write_u8((byte)JvmOpcode.pop);
                maxStack = Math.Max(maxStack, 1);
            }
        }

        writer.write_u8((byte)JvmOpcode.@return);

        // max_locals 至少为 1（String[] args 占 slot 0），
        // 再加上入口函数参数可能占用的槽位（long/double 占 2 个槽位，其余占 1 个）
        var maxLocals = 1;
        for (var i = 0; i < parameterTypes.Count; i++) maxLocals += is_wide_type(parameterTypes[i]) ? 2 : 1;

        var codeAttr = new JvmCodeAttribute
        {
            attribute_name_index = codeAttrNameIdx,
            max_stack = (ushort)maxStack,
            max_locals = (ushort)maxLocals,
            code = writer.to_array(),
            exception_table = [],
            attributes = []
        };

        return new JvmMethodInfo
        {
            access_flags = 0x0009, // ACC_PUBLIC | ACC_STATIC
            name_index = nameIdx,
            descriptor_index = descIdx,
            attributes = [codeAttr]
        };
    }

    private static JvmMethodInfo build_method(GenerateFunction function, List<JvmConstant> constantPool,
        IReadOnlyList<string> moduleStrings, List<JvmBootstrapMethod> bootstrapMethods, string className,
        JvmExternalLinkTable linkTable,
        IReadOnlyList<GenerateFunction> allFunctions)
    {
        var nameIdx = add_utf8(constantPool, sanitize_method_name(function.name));
        var parameterTypes = function.parameters.Select(p => p.type_ref).ToList();
        var returnType = to_jvm_type_name(function.return_type_ref);
        var descIdx = add_utf8(constantPool, to_jvm_method_descriptor(function.return_type_ref, parameterTypes));
        var codeAttrNameIdx = add_utf8(constantPool, "Code");

        var writer = new ByteBufferWriter(1024);
        var labelEntryDepths = compute_label_entry_depths(function);
        var instructionOffsets =
            compute_instruction_offsets(function, constantPool, moduleStrings, bootstrapMethods, className,
                linkTable, allFunctions, labelEntryDepths);
        var labelOffsets = build_label_offsets(function, instructionOffsets);
        var stackDepth = 0;
        // 诊断：dump LIR（与 WasmBackend 一致的环境变量协议）
        // LEGION_SPY_LIR_DUMP=1 强制开启 dump
        // LEGION_SPY_LIR_FUNC=<func> 指定只 dump 该函数（函数名包含匹配）
        var spyDumpEnabled = Environment.GetEnvironmentVariable("LEGION_SPY_LIR_DUMP") == "1";
        var spyFuncFilter = Environment.GetEnvironmentVariable("LEGION_SPY_LIR_FUNC");
        var shouldDiag = spyDumpEnabled
            ? string.IsNullOrEmpty(spyFuncFilter) || function.name.Contains(spyFuncFilter, StringComparison.Ordinal)
            : false;

        if (shouldDiag)
        {
            Console.Error.WriteLine(
                $"\n=== LIR dump (jvm): {function.name} ({function.instructions.Count} instrs, return={function.return_type_ref}) ===");
            for (var i = 0; i < function.instructions.Count; i++)
            {
                var instr = function.instructions[i];
                var operandsStr = instr.operands.Count > 0
                    ? string.Join(", ", instr.operands.Select(o =>
                    {
                        return o switch
                        {
                            GenerateOperand.I32 i32 => $"I32({i32.value})",
                            GenerateOperand.I64 i64 => $"I64({i64.value})",
                            GenerateOperand.Str s => $"Str(\"{s.value}\")",
                            GenerateOperand.Label l => $"Lbl({l.name})",
                            GenerateOperand.Local loc => $"Local({loc.index}, {loc.type})",
                            GenerateOperand.Param p => $"Param({p.index}, {p.type})",
                            GenerateOperand.FuncRef fr =>
                                $"Func({fr.name}, results=[{string.Join(",", fr.signature.results)}], params=[{string.Join(",", fr.signature.parameters)}])",
                            GenerateOperand.Null => "Null",
                            _ => o.ToString()
                        };
                    }))
                    : "";
                Console.Error.WriteLine($"  [{i:D3}] {instr.head_code} {operandsStr}");
            }

            Console.Error.WriteLine("=== labels ===");
            foreach (var label in function.labels)
                Console.Error.WriteLine($"  label {label.name} -> instr {label.instruction_index}");
            Console.Error.WriteLine($"=== end LIR dump: {function.name} ===\n");
        }

        // 按指令索引排序的标签列表，用于检测标签目标并重置栈深度
        var sortedLabelsForEmit = function.labels
            .OrderBy(l => l.instruction_index)
            .ToList();
        var nextLabelIdxForEmit = 0;
        var skipUntilLabelForEmit = false;

        for (var instructionIndex = 0; instructionIndex < function.instructions.Count; instructionIndex++)
        {
            // 检查当前指令是否是标签目标
            while (nextLabelIdxForEmit < sortedLabelsForEmit.Count &&
                   sortedLabelsForEmit[nextLabelIdxForEmit].instruction_index == instructionIndex)
            {
                var labelName = sortedLabelsForEmit[nextLabelIdxForEmit].name;
                if (labelEntryDepths.TryGetValue(labelName, out var labelStackDepth))
                {
                    // 当到达标签时，如果实际栈深度与预期不一致，需要插入 pop/aconst_null 调整
                    // 这确保 JVM 验证器在所有路径到达此标签时看到一致的栈深度
                    while (stackDepth > labelStackDepth)
                    {
                        writer.write_u8((byte)JvmOpcode.pop);
                        stackDepth--;
                    }

                    while (stackDepth < labelStackDepth)
                    {
                        writer.write_u8((byte)JvmOpcode.aconst_null);
                        stackDepth++;
                    }
                }

                skipUntilLabelForEmit = false;
                nextLabelIdxForEmit++;
            }

            if (skipUntilLabelForEmit) continue;

            var currentInstr = function.instructions[instructionIndex];
            var posBefore = writer.position;
            emit_instruction(ref writer, ref stackDepth, labelEntryDepths, currentInstr,
                function, constantPool, moduleStrings,
                bootstrapMethods, className, instructionIndex, instructionOffsets, labelOffsets, linkTable,
                allFunctions);
            var posAfter = writer.position;
            if (function.name == "legion.voa")
            {
                var bytes = new List<byte>();
                for (var bi = posBefore; bi < posAfter; bi++) bytes.Add(writer.to_array()[bi]);
                Console.Error.WriteLine($"[POS-DEBUG] func=legion.voa idx={instructionIndex} head={currentInstr.head_code} posBefore={posBefore} posAfter={posAfter} delta={posAfter - posBefore} bytes=[{string.Join(",", bytes.Select(b => $"0x{b:X2}"))}]");
            }

            // 无条件跳转或返回后，跳过后续指令直到下一个标签。
            // 如果不跳过，const Null + pop 等清理代码会变成死代码，
            // 导致 JVM 验证器报 "Inconsistent stack height" 错误。
            if (currentInstr.head_code == NyarHeadCode.jump || currentInstr.head_code == NyarHeadCode.@return)
                skipUntilLabelForEmit = true;
        }

        // Ensure methods without an explicit terminator still return a default value.
        if (function.instructions.Count == 0 || !is_return_opcode(function.instructions[^1].head_code))
            emit_implicit_return(ref writer, function.return_type_ref, constantPool);

        if (function.name == "legion.voa")
        {
            var arr = writer.to_array();
            Console.Error.WriteLine($"[DUMP-DEBUG] func=legion.voa total_bytes={arr.Length} hex=[{string.Join(",", arr.Select(b => $"0x{b:X2}"))}]");
        }

        var codeAttr = new JvmCodeAttribute
        {
            attribute_name_index = codeAttrNameIdx,
            max_stack = compute_jvm_max_stack(function),
            max_locals = compute_max_locals(function),
            code = writer.to_array(),
            exception_table = [],
            attributes = []
        };

        return new JvmMethodInfo
        {
            access_flags = 0x0009, // ACC_PUBLIC | ACC_STATIC
            name_index = nameIdx,
            descriptor_index = descIdx,
            attributes = [codeAttr]
        };
    }
}