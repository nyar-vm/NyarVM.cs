using System.Text;
using Nyar.Types.Externals;
using Std.Data.Binary.NyarIR.Data;
using Std.Data.Binary.Wasm;

namespace Nyar.Assembler.Backends.Wasm;

/// <summary>
///     <see cref="WasmBackend" /> 的嵌套类型与构建上下文定义�?///
/// </summary>
public sealed partial class WasmBackend
{
    private sealed class WasmBuildContext
    {
        private readonly Dictionary<string, List<WasmFunctionIndexEntry>> _function_indices;
        private readonly Dictionary<string, WasmStringLiteral> _string_literal_map = new(StringComparer.Ordinal);

        /// <summary>
        ///     跨后端外部函数名集合（标记了 [clr] �?[jvm] 但在 WASM 后端编译的函数）�?    ///     在调用点使用 fallback：弹出参数，压入默认返回值�?    ///
        /// </summary>
        public readonly HashSet<string> cross_backend_external_names = new(StringComparer.Ordinal);

        /// <summary>
        ///     [wasm] 外部函数�?�?导入函数索引（从0开始）�?    ///
        /// </summary>
        public readonly Dictionary<string, uint> wasm_import_function_index = new(StringComparer.Ordinal);

        /// <summary>
        ///     [wasm] 外部导入函数规格列表�?    ///
        /// </summary>
        public readonly List<(string Module, string Field, string FunctionName, GenerateFunction Function)>
            wasm_import_specs = [];

        /// <summary>
        ///     原始函数索引（module.functions 中的索引）→ WASM 代码段索引的映射�?        ///     �?Phase 2 代码生成前由
        ///     <see cref="build_original_to_wasm_index_map" /> 填充�?        ///     调用点通过此映射计算正确的 WASM call 指令目标索引�?        ///
        /// </summary>
        private Dictionary<int, int> _original_to_wasm_index = [];

        /// <summary>
        ///     用于诊断输出的函数计数（�?0 开始递增）�?        ///
        /// </summary>
        public int function_count;

        public WasmBuildContext(GenerateModule module, CompilationOptions options)
        {
            this.module = module;
            this.options = options;
            _function_indices = new Dictionary<string, List<WasmFunctionIndexEntry>>(StringComparer.Ordinal);
            foreach (var item in module.functions.Select((function, index) =>
                         new { Function = function, Name = function.name, Index = index }))
            {
                if (string.IsNullOrWhiteSpace(item.Name)) continue;

                var signatureKey = create_function_signature_key(item.Function);
                if (!_function_indices.TryGetValue(item.Name, out var overloads))
                {
                    overloads = [];
                    _function_indices[item.Name] = overloads;
                }

                if (overloads.Any(candidate =>
                        string.Equals(candidate.signature_key, signatureKey, StringComparison.Ordinal)))
                {
                    var duplicates = module.functions
                        .Select((function, index) => new
                        {
                            Name = function.name,
                            Index = index,
                            SignatureKey = create_function_signature_key(function)
                        })
                        .Where(candidate =>
                            string.Equals(candidate.Name, item.Name, StringComparison.Ordinal) &&
                            string.Equals(candidate.SignatureKey, signatureKey, StringComparison.Ordinal))
                        .Select(candidate => $"{candidate.Name}@{candidate.Index}")
                        .ToArray();
                    throw new InvalidOperationException(
                        $"WASM 函数签名冲突：{item.Name}；签名键：{signatureKey}；重复项：{string.Join(", ", duplicates)}");
                }

                overloads.Add(new WasmFunctionIndexEntry(item.Index, signatureKey));
            }
        }

        public GenerateModule module { get; }

        public CompilationOptions options { get; }

        /// <summary>
        ///     导入函数总数（在 Phase 1 中计算，Phase 2 代码生成时使用）�?    ///     仅在 build_wasm_module �?Phase 1 完成后有效�?    ///
        /// </summary>
        public uint import_function_count { get; set; }

        public uint heap_ptr_global_index { get; set; } = uint.MaxValue;

        public IReadOnlyCollection<WasmStringLiteral> string_literals => _string_literal_map.Values;

        /// <summary>
        ///     当前字符串数据末尾偏移量，用于确定堆指针初始值�?        ///
        /// </summary>
        public int next_data_offset { get; private set; }

        /// <summary>
        ///     GC 类型名到 typeidx 的映射字典，�?WasmGcTypeBuilder 构建后填充�?        ///     键为 new_object 指令中的类型名，值为对应�?WASM typeidx�?
        ///     ///     空字典表示模块中不包�?GC 类型定义�?        ///
        /// </summary>
        public Dictionary<string, uint> gc_type_name_to_index { get; set; } = [];

        /// <summary>
        ///     GC 字段名到字段索引的全局映射字典�?        ///     键为 get_field/set_field 指令中的字段名，值为对应�?struct 字段索引�?        ///
        ///     空字典表示模块中不包含字段访问�?        ///
        /// </summary>
        public Dictionary<string, uint> gc_field_name_to_index { get; } = [];

        /// <summary>
        ///     字段操作（get_field/set_field）指令索引到类型名的映射�?        ///     键为指令在函数指令列表中的索引，值为 new_object 指令中的类型名�?        ///     用于
        ///     emit_get_field/emit_set_field 查找对应�?struct typeidx�?        ///
        /// </summary>
        public Dictionary<int, string> gc_field_op_type_name { get; } = [];

        public void initialize_module_features()
        {
            // 预处理：将 [js_builtin("...")] 声明的函数桥接为 WASM 导入。
            // [js_builtin] 函数无函数体，在 WASM 目标下需要作为 env.<短名> 导入，
            // 由 JS 胶水层提供实现（如 console_log 从线性内存读取 UTF-8 字符串并调用 console.log）。
            // 此处补充 WASM 外部导入，使后续收集逻辑将其纳入 wasm_import_specs，
            // 同时使 has_external_import 为 true，从而在可达性分析中排除（不生成函数体）。
            foreach (var function in module.functions)
            {
                if (function.try_get_external_import_link(CallingConvention.wasm, out _)) continue;

                var jsBuiltinAttr = function.attributes.FirstOrDefault(a => string.Equals(a.name, "js_builtin", StringComparison.OrdinalIgnoreCase));
                if (jsBuiltinAttr is null) continue;

                var shortName = get_short_name(function.name);
                function.add_external_import_link(new ExternalWasmFunctionImport("env", shortName));
            }

            // 收集 [wasm] 或 [import("wasm",...)] 外部导入函数
            foreach (var function in module.functions)
            {
                // 1. 收集目标为 WASM 的外部导入链。
                if (function.try_get_external_import_link(CallingConvention.wasm, out var externalImportLink) &&
                    externalImportLink is ExternalWasmFunctionImport wasmImportLink &&
                    wasmImportLink.wasm_type is WasmFuncTypeRef funcTypeRef)
                    wasm_import_specs.Add((funcTypeRef.module, funcTypeRef.field, function.name,
                        function));

                // 2. 收集其他目标的外部导入函数（非 WASM 目标的外部导入）
                if (function.has_external_import && !function.try_get_external_import_link(CallingConvention.wasm, out _))
                {
                    cross_backend_external_names.Add(function.name);
                    var shortName = get_short_name(function.name);
                    if (!string.Equals(shortName, function.name, StringComparison.Ordinal))
                        cross_backend_external_names.Add(shortName);
                }
            }
        }

        /// <summary>
        ///     根据 <paramref name="reachableFunctions" />�?paramref name="wasmImportNames" /> �?    ///
        ///     <paramref name="crossBackendNames" /> 预先计算原始函数索引�?WASM 代码段索引的映射�?    ///     必须�?Phase 2 代码生成前调用，�?emit_call_static
        ///     能获取正确的 WASM 调用目标索引�?    ///
        /// </summary>
        public void build_original_to_wasm_index_map(
            HashSet<int> reachableFunctions,
            HashSet<string> wasmImportNames,
            HashSet<string> crossBackendNames)
        {
            _original_to_wasm_index = new Dictionary<int, int>();
            var wasmCodeIndex = 0;

            for (var functionIndex = 0; functionIndex < module.functions.Count; functionIndex++)
            {
                if (!reachableFunctions.Contains(functionIndex)) continue;

                var func = module.functions[functionIndex];
                if (wasmImportNames.Contains(func.name) || crossBackendNames.Contains(func.name)) continue;

                _original_to_wasm_index[functionIndex] = wasmCodeIndex;
                wasmCodeIndex++;
            }

            _ = _original_to_wasm_index.Count;
        }

        /// <summary>
        ///     按名称和签名查找函数�?WASM 代码段索引�?    ///     先在 <see cref="_function_indices" /> 中查找原始索引，再通过
        ///     <see cref="_original_to_wasm_index" /> 映射�?WASM 代码段索引�?    ///     调用
        ///     <see cref="build_original_to_wasm_index_map" /> 填充映射后有效�?    ///
        /// </summary>
        public bool try_get_function_index(string functionName, GenerateFunctionType signature, out int functionIndex)
        {
            if (!_function_indices.TryGetValue(functionName, out var overloads))
            {
                // 短名回退：例如调�?"path_join" 时尝试匹�?"legion.path_join"
                foreach (var kvp in _function_indices)
                {
                    var shortName = get_short_name(kvp.Key);
                    if (string.Equals(shortName, functionName, StringComparison.Ordinal))
                    {
                        overloads = kvp.Value;
                        break;
                    }
                }

                if (overloads == null)
                {
                    functionIndex = default;
                    return false;
                }
            }

            var signatureKey = create_function_signature_key(signature);
            int? originalIndex = null;
            foreach (var overload in overloads)
                if (string.Equals(overload.signature_key, signatureKey, StringComparison.Ordinal))
                {
                    originalIndex = overload.index;
                    break;
                }

            if (originalIndex is null && overloads.Count == 1)
            {
                var onlyOverload = overloads[0];
                var overloadFunc = module.functions[onlyOverload.index];
                if (overloadFunc.parameters.Count == signature.parameters.Count) originalIndex = onlyOverload.index;
            }

            if (!originalIndex.HasValue)
            {
                functionIndex = default;
                return false;
            }

            if (_original_to_wasm_index.TryGetValue(originalIndex.Value, out var wasmCodeIndex))
            {
                functionIndex = wasmCodeIndex;
                return true;
            }

            functionIndex = default;
            return false;
        }

        /// <summary>
        ///     检查指定函数的实际返回类型是否�?void/unit�?    ///     通过查找 module.functions 中函数的 return_type 字段判断�?    ///     而非依赖调用点的
        ///     funcRef.signature（后者可能与实际 WASM 函数类型不一致）�?    ///
        /// </summary>
        public bool try_get_function_return_is_void(string functionName, GenerateFunctionType signature)
        {
            if (!_function_indices.TryGetValue(functionName, out var overloads))
            {
                // 短名回退：与 try_get_function_index 保持一致。
                foreach (var kvp in _function_indices)
                {
                    var shortName = get_short_name(kvp.Key);
                    if (string.Equals(shortName, functionName, StringComparison.Ordinal))
                    {
                        overloads = kvp.Value;
                        break;
                    }
                }

                if (overloads == null) return false;
            }

            var signatureKey = create_function_signature_key(signature);
            int? originalIndex = null;
            foreach (var overload in overloads)
                if (string.Equals(overload.signature_key, signatureKey, StringComparison.Ordinal))
                {
                    originalIndex = overload.index;
                    break;
                }

            if (originalIndex is null && overloads.Count == 1)
            {
                var onlyOverload = overloads[0];
                var overloadFunc = module.functions[onlyOverload.index];
                if (overloadFunc.parameters.Count == signature.parameters.Count) originalIndex = onlyOverload.index;
            }

            if (originalIndex.HasValue)
            {
                var func = module.functions[originalIndex.Value];
                var isVoid = func.return_type_ref.is_void_like;
                return isVoid;
            }

            return false;
        }

        public WasmStringLiteral register_string_literal(string value)
        {
            if (_string_literal_map.TryGetValue(value, out var existing)) return existing;

            var bytes = Encoding.UTF8.GetBytes(value);
            var terminatedBytes = new byte[bytes.Length + 1];
            bytes.CopyTo(terminatedBytes, 0);
            var literal = new WasmStringLiteral(value, next_data_offset, terminatedBytes);
            _string_literal_map[value] = literal;
            next_data_offset += terminatedBytes.Length;
            return literal;
        }

        /// <summary>
        ///     预扫描所有可达函数中的指令，收集字符串字面量并检�?OOP 指令使用情况�?        ///     在代码生成前调用，确保字符串数据偏移量在全局变量初始化时已知�?        ///
        /// </summary>
        public bool pre_scan(GenerateModule module, HashSet<int> reachableFunctions)
        {
            var usesOop = false;
            var globalFieldIndex = 0;

            foreach (var funcIndex in reachableFunctions)
            {
                var func = module.functions[funcIndex];

                // 追踪每个函数中最新的 new_object 类型。
                string? lastNewObjectType = null;

                foreach (var inst in func.instructions)
                    switch (inst.head_code)
                    {
                        case NyarHeadCode.new_object:
                            usesOop = true;
                            if (inst.operands.FirstOrDefault() is GenerateOperand.Str strOp)
                                lastNewObjectType = strOp.value;
                            break;

                        case NyarHeadCode.get_field:
                        case NyarHeadCode.set_field:
                            usesOop = true;
                            // 追踪字段操作对应的类型名
                            if (lastNewObjectType != null)
                                gc_field_op_type_name[inst.operands.GetHashCode()] = lastNewObjectType;

                            // 向前查找 preceding const(Str) 指令获取字段名。
                            for (var prevIdx = func.instructions.IndexOf(inst) - 1; prevIdx >= 0; prevIdx--)
                            {
                                var prevInst = func.instructions[prevIdx];
                                if (prevInst.head_code == NyarHeadCode.@const &&
                                    prevInst.operands.FirstOrDefault() is GenerateOperand.Str fieldOp)
                                {
                                    if (!gc_field_name_to_index.ContainsKey(fieldOp.value))
                                        gc_field_name_to_index[fieldOp.value] = (uint)globalFieldIndex++;
                                    break;
                                }

                                // 遇到值产生指令就停止回溯
                                if (prevInst.head_code == NyarHeadCode.load_arg ||
                                    prevInst.head_code == NyarHeadCode.load_local ||
                                    prevInst.head_code == NyarHeadCode.call_static)
                                    break;
                            }

                            break;

                        case NyarHeadCode.get_offset_index:
                        case NyarHeadCode.set_offset_index:
                            usesOop = true;
                            break;

                        case NyarHeadCode.@const:
                            if (inst.operands.FirstOrDefault() is GenerateOperand.Str constStrOp)
                                register_string_literal(constStrOp.value);
                            break;
                    }
            }

            return usesOop;
        }
    }

    private sealed class WasmFunctionEmitContext
    {
        public WasmFunctionEmitContext(WasmBuildContext buildContext, GenerateFunction function)
        {
            build_context = buildContext;
            this.function = function;
        }

        public WasmBuildContext build_context { get; }

        public GenerateFunction function { get; }

        /// <summary>
        ///     dup 和新对象操作使用的临时局部变量索引（WASM 局部变量索引），uint.MaxValue 表示未分配�?        ///
        /// </summary>
        public uint scratch_local_index { get; set; } = uint.MaxValue;

        /// <summary>
        ///     i32_store / i64_store 地址保留使用的第二个临时局部变量索引，uint.MaxValue 表示未分配�?        ///
        /// </summary>
        public uint scratch_local_index2 { get; set; } = uint.MaxValue;

        /// <summary>
        ///     堆指针全局变量索引（从 WasmBuildContext 复制），uint.MaxValue 表示未分配�?        ///
        /// </summary>
        public uint heap_ptr_global_index { get; set; } = uint.MaxValue;

        /// <summary>
        ///     GC 类型名到 typeidx 的映射字典，�?WasmBuildContext 复制�?        ///     用于 emit_new_object 查找 struct.new_default 的类型索引�?
        ///     ///
        /// </summary>
        public Dictionary<string, uint> gc_type_name_to_index { get; set; } = [];

        /// <summary>
        ///     GC 字段名到字段索引的映射字典，�?WasmBuildContext 复制�?        ///     用于 emit_get_field/emit_set_field 查找字段索引�?        ///
        /// </summary>
        public Dictionary<string, uint> gc_field_name_to_index { get; set; } = [];

        /// <summary>
        ///     字段操作指令索引到类型名的映射，�?WasmBuildContext 复制�?        ///     用于 emit_get_field/emit_set_field 查找对应�?struct typeidx�?
        ///     ///
        /// </summary>
        public Dictionary<int, string> gc_field_op_type_name { get; set; } = [];

        /// <summary>
        ///     当前函数的指令列表，用于防御�?drop 清理时查看下一条指令�?        ///
        /// </summary>
        public IReadOnlyList<GenerateInstruction>? instructions { get; set; }

        /// <summary>
        ///     当前正在发射的指令索引，用于防御�?drop 清理时查看下一条指令�?        ///
        /// </summary>
        public int instruction_index { get; set; } = -1;

        /// <summary>
        ///     标记上一发射的指令是否为 void/unit 调用�?        ///     用于 pop 指令跳过 drop：void 函数调用后栈上无返回值�?        ///
        /// </summary>
        public bool previous_was_void_call { get; set; }

        /// <summary>
        ///     标记当前函数发射是否已遇�?return 或无条件 jump，后续指令不可达�?        ///     用于跨段传播不可达状态，避免�?void block 中遗留栈值�?        ///
        /// </summary>
        public bool unreachable { get; set; }

        /// <summary>
        ///     标记不可达状态是否由 return 指令引起（而非无条�?br）�?        ///     return 引起的不可达不能�?end 指令重置，因为函数已返回�?        ///     end
        ///     后的代码仍然不可达。br 引起的不可达可以�?end 重置�?        ///     因为 br 跳出 block 后，end 后的代码可通过 fallthrough 到达�?        ///
        /// </summary>
        public bool unreachable_by_return { get; set; }

        /// <summary>
        ///     当前 emit 栈深度估计值。
        ///     由 emit_instruction 在每条指令发射后更新，build_code_internal
        ///     据此决定函数末尾的 return/drop/push 兜底逻辑。
        ///     结构化控制流路径下，block/loop 的 end 会弹出块内栈值，
        ///     因此在 block/loop 创建时通过 block_depth_stack 保存现场，
        ///     关闭时恢复，使该值在控制流路径下仍然可信。
        /// </summary>
        public int stack_depth { get; set; }

        /// <summary>
        ///     结构化控制流 block/loop 栈深度现场栈。
        ///     每次发射 block/loop 时压入当前 stack_depth，
        ///     发射对应 end 时弹出并恢复 stack_depth，
        ///     以抵消 WASM void block 结束时丢弃块内栈值的语义。
        /// </summary>
        public Stack<int> block_depth_stack { get; } = new();

        /// <summary>
        ///     结构化控制流 block 类型现场栈，与 block_depth_stack 同步使用。
        ///     true 表示 block i32 (0x7F)，end 时产生 1 个 i32 值到外层栈（stack_depth = entry + 1）；
        ///     false 表示 block void (0x40) 或 loop，end 时丢弃块内栈值（stack_depth = entry）。
        ///     条件跳转目标（jump_if_false / jump_if_true）需要 block i32，
        ///     因为条件值在 block 内计算并通过 end 逃逸到外层供目标指令消费。
        /// </summary>
        public Stack<bool> block_type_stack { get; } = new();

        /// <summary>
        ///     检查当前指令的下一条是否为 store_local �?store_arg�?        ///     用于防御�?drop 清理：store 指令本身会消费栈顶值，无需�?drop�?        ///
        /// </summary>
        public bool is_next_instruction_store()
        {
            if (instructions is null) return false;

            var nextIndex = instruction_index + 1;
            if (nextIndex >= instructions.Count) return false;

            var nextOpcode = instructions[nextIndex].head_code;
            return nextOpcode is NyarHeadCode.store_local or NyarHeadCode.store_arg
                or NyarHeadCode.store_global or NyarHeadCode.field_store
                or NyarHeadCode.index_store or NyarHeadCode.set_upvalue;
        }

        /// <summary>
        ///     检查当前指令的上一条是否为 call_static �?void/unit 函数�?        ///     用于防御�?drop 清理：void 函数调用后栈上无返回值，
        ///     后续�?pop 指令不应发射 drop�?        ///
        /// </summary>
        public bool is_previous_void_call()
        {
            if (instructions is null || instruction_index <= 0) return false;

            var prevInst = instructions[instruction_index - 1];
            if (prevInst.head_code != NyarHeadCode.call_static) return false;

            if (prevInst.operands.FirstOrDefault() is GenerateOperand.FuncRef funcRef)
            {
                var isVoid = funcRef.signature.results.Count == 0 ||
                             funcRef.signature.results[0] == GenerateValueType.@void ||
                             funcRef.signature.results[0] == GenerateValueType.unit;
                return isVoid;
            }

            return false;
        }
    }

    private sealed record WasmStructuredControlFlowPlan(
        IReadOnlyDictionary<string, int> label_targets,
        IReadOnlyList<int> target_indices,
        IReadOnlyList<WasmLoopRegion> loops)
    {
        public static WasmStructuredControlFlowPlan empty { get; } =
            new(new Dictionary<string, int>(StringComparer.Ordinal), [], []);
    }

    private readonly record struct WasmLoopRegion(int start_index, int end_index);

    private readonly record struct WasmFunctionIndexEntry(int index, string signature_key);

    private sealed record WasmStringLiteral(string value, int offset, byte[] bytes)
    {
        public int length => bytes.Length;
    }
}
