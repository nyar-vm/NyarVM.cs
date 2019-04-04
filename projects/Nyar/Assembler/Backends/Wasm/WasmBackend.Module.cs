using Std.Data.Binary.Frame;
using Std.Data.Binary.Wasm.Data;

namespace Nyar.Assembler.Backends.Wasm;

/// <summary>
///     <see cref="WasmBackend" /> 的 partial 文件，承载模块级构建逻辑。
///     包含 <see cref="build_wasm_module" /> 主构建方法及其直接辅助方法
///     （类型映射、不可达体生成、局部变量构建、局部变量索引解析）。
/// </summary>
public sealed partial class WasmBackend
{
    /// <summary>
    ///     构建 WASM 模块数据结构。按阶段顺序完成：导入函数注册、预扫描、
    ///     可达函数代码生成、导入记录构建、导出构建、GC 类型定义构建。
    /// </summary>
    private WasmModuleData build_wasm_module(GenerateModule module, CompilationOptions options)
    {
        var buildContext = new WasmBuildContext(module, options);
        var types = new List<WasmFunctionType>();
        var functionTypeIndices = new List<uint>();
        var codes = new List<WasmCode>();
        var exports = new List<WasmExport>();
        var imports = new List<WasmImport>();
        var dataSegments = new List<WasmData>();
        var globals = new List<WasmGlobal>();

        buildContext.initialize_module_features();

        // ===== Phase 1: 注册导入函数类型并计算导入函数索引（必须在代码生成前完成）=====

        // 存储每个 [wasm]/[import("wasm",...)] 导入的类型索引，用于后续构建导入记录
        var wasmImportTypeIndices = new Dictionary<string, uint>(StringComparer.Ordinal);
        foreach (var importSpec in buildContext.wasm_import_specs)
        {
            var wasmFunc = importSpec.Function;
            wasmImportTypeIndices[importSpec.FunctionName] = (uint)types.Count;
            types.Add(new WasmFunctionType
            {
                parameters = [.. wasmFunc.parameters.Select(p => map_value_type(p.type_ref))],
                results = wasmFunc.return_type_ref.is_void_like ? [] : [map_value_type(wasmFunc.return_type_ref)]
            });
        }

        // 计算导入函数索引（必须在代码生成前完成，使 emit_call_static 能查找到）
        var nextImportIndex = 0u;
        foreach (var importSpec in buildContext.wasm_import_specs)
        {
            buildContext.wasm_import_function_index[importSpec.FunctionName] = nextImportIndex;

            // 同时注册短名（不含命名空间前缀），使调用点能通过短名匹配
            var shortName = get_short_name(importSpec.FunctionName);
            if (!string.Equals(shortName, importSpec.FunctionName, StringComparison.Ordinal))
                buildContext.wasm_import_function_index[shortName] = nextImportIndex;

            nextImportIndex++;
        }

        // 存储计算后的导入函数总数，供 emit_call_static 中内部函数调用索引计算使用
        buildContext.import_function_count = nextImportIndex;

        // ===== Phase 1.5: 预扫描 OOP 指令并收集字符串字面量 =====
        var reachableFunctions = collect_reachable_functions(module);
        var usesOop = buildContext.pre_scan(module, reachableFunctions);

        // 如果使用了 OOP 指令，添加堆指针全局变量
        if (usesOop)
        {
            var heapInitValue = buildContext.next_data_offset;
            var initWriter = new ByteBufferWriter(16);
            initWriter.write_u8((byte)WasmOpcode.i32_const);
            initWriter.write_leb128_i32(heapInitValue);
            initWriter.write_u8((byte)WasmOpcode.end);
            globals.Add(new WasmGlobal
            {
                type = new WasmGlobalType
                {
                    value_type = WasmValueType.int32,
                    mutable = true
                },
                init_expression = initWriter.to_array()
            });
            buildContext.heap_ptr_global_index = 0;
        }

        // ===== Phase 2: 生成可达函数代码（此时导入函数索引已就绪）=====

        // 建立 originalIndex → wasmFunctionIndex 的映射
        var originalToWasmIndex = new Dictionary<int, int>();

        var wasmImportFunctionNames = new HashSet<string>(
            buildContext.wasm_import_specs.Select(s => s.FunctionName),
            StringComparer.Ordinal);

        // 预先计算原始函数索引 → WASM 代码段索引的映射，使 emit_call_static 能获取正确的 WASM 调用目标索引
        buildContext.build_original_to_wasm_index_map(
            reachableFunctions,
            wasmImportFunctionNames,
            buildContext.cross_backend_external_names);

        for (var functionIndex = 0; functionIndex < module.functions.Count; functionIndex++)
        {
            // DCE：跳过不可达的函数
            if (!reachableFunctions.Contains(functionIndex)) continue;

            var func = module.functions[functionIndex];

            // 跳过 [wasm] 外部导入函数
            if (wasmImportFunctionNames.Contains(func.name)) continue;

            // 跳过跨后端外部函数（[clr] 或 [jvm]）
            if (buildContext.cross_backend_external_names.Contains(func.name)) continue;

            // 记录原始索引到 WASM 函数索引的映射
            originalToWasmIndex[functionIndex] = codes.Count;

            types.Add(new WasmFunctionType
            {
                parameters = [.. func.parameters.Select(p => map_value_type(p.type_ref))],
                results = func.return_type_ref.is_void_like ? [] : [map_value_type(func.return_type_ref)]
            });

            functionTypeIndices.Add((uint)(types.Count - 1));
            codes.Add(build_code(func, buildContext));
        }

        // ===== Phase 3: 构建导入记录 =====

        // 添加 [wasm]/[import("wasm",...)] 外部导入记录
        foreach (var importSpec in buildContext.wasm_import_specs)
            imports.Add(new WasmImport
            {
                module = importSpec.Module,
                field = importSpec.Field,
                descriptor = new WasmImportDescriptor
                {
                    kind = WasmExternalKind.function,
                    function_type_index = wasmImportTypeIndices[importSpec.FunctionName]
                }
            });

        // 添加 memory 导入和数据段（当存在字符串字面量时）
        var hasMemoryImport = false;
        if (buildContext.string_literals.Count > 0)
        {
            imports.Add(new WasmImport
            {
                module = "env",
                field = "memory",
                descriptor = new WasmImportDescriptor
                {
                    kind = WasmExternalKind.memory,
                    memory_type = new WasmMemoryType
                    {
                        limits = new WasmLimits
                        {
                            minimum = 1
                        }
                    }
                }
            });
            hasMemoryImport = true;

            foreach (var literal in buildContext.string_literals.OrderBy(item => item.offset))
                dataSegments.Add(new WasmData
                {
                    memory_index = 0,
                    offset_expression = build_i32_offset_expression(literal.offset),
                    initializer = literal.bytes
                });
        }

        var importFunctionCount =
            (uint)imports.Count(importItem => importItem.descriptor.kind == WasmExternalKind.function);

        if (module.exports.Count > 0)
            foreach (var export in module.exports.Where(e => e.kind == GenerateExportKind.function))
            {
                if (!originalToWasmIndex.TryGetValue(export.function_index, out var wasmIndex)) continue;

                exports.Add(new WasmExport
                {
                    name = export.name,
                    kind = WasmExternalKind.function,
                    index = importFunctionCount + (uint)wasmIndex
                });
            }
        else
            // 导出所有可达函数的默认名称
            foreach (var (originalIndex, wasmIndex) in originalToWasmIndex)
            {
                var func = module.functions[originalIndex];
                exports.Add(new WasmExport
                {
                    name = func.name,
                    kind = WasmExternalKind.function,
                    index = importFunctionCount + (uint)wasmIndex
                });
            }

        if (hasMemoryImport)
            exports.Add(new WasmExport
            {
                name = "memory",
                kind = WasmExternalKind.memory,
                index = 0
            });

        var entryFunctionIndex = find_entry_function_index(module);
        if (entryFunctionIndex >= 0 && originalToWasmIndex.TryGetValue(entryFunctionIndex, out var entryWasmIndex))
        {
            var entryExportName = resolve_entry_export_name(options);
            // 若入口函数已通过显式导出列表导出，则不再重复添加同名导出，避免名称冲突
            if (!exports.Any(e => e.kind == WasmExternalKind.function &&
                                  string.Equals(e.name, entryExportName, StringComparison.Ordinal)))
                exports.Add(new WasmExport
                {
                    name = entryExportName,
                    kind = WasmExternalKind.function,
                    index = importFunctionCount + (uint)entryWasmIndex
                });
        }

        // ===== Phase 3.5: 构建 GC 类型定义（扫描所有函数的 new_object 指令）=====
        // GC 类型在 type 段中以 rec group 形式发出，typeidx 从现有函数类型数量之后开始编号
        var (gcSubTypes, gcTypeNameToIndex, _) = WasmGcTypeBuilder.build(module, types);
        buildContext.gc_type_name_to_index = gcTypeNameToIndex;

        return new WasmModuleData
        {
            types = types,
            gc_sub_types = gcSubTypes.Count > 0 ? gcSubTypes : null,
            imports = imports,
            function_type_indices = functionTypeIndices,
            codes = codes,
            exports = distinct_exports(exports),
            data_segments = dataSegments,
            globals = globals
        };
    }

    /// <summary>
    ///     将 Nyar 类型名映射为 WASM 值类型。
    ///     注意：`object` / `any` 映射为引用类型以支持 GC 对象。
    /// </summary>
    private static WasmValueType map_value_type(string typeName)
    {
        return map_value_type(GenerateTypeReference.parse(typeName));
    }

    /// <summary>
    ///     将结构化类型引用映射为 WASM 值类型。
    /// </summary>
    private static WasmValueType map_value_type(GenerateTypeReference typeName)
    {
        return typeName.kind switch
        {
            GenerateTypeKind.unit => WasmValueType.int32,
            GenerateTypeKind.@bool or GenerateTypeKind.i8 or GenerateTypeKind.i16 or GenerateTypeKind.i32 =>
                WasmValueType.int32,
            GenerateTypeKind.i64 => WasmValueType.int64,
            GenerateTypeKind.f32 => WasmValueType.float32,
            GenerateTypeKind.f64 => WasmValueType.float64,
            GenerateTypeKind.v128 => WasmValueType.v128,
            GenerateTypeKind.function_ref => WasmValueType.func_ref,
            // WASM 后端将所有引用类型（字符串、对象、数组、任意类型、外部引用）视为
            // 线性内存中的 i32 句柄/偏移量，因为 Str 常量、string_eq 等都使用 i32。
            // 真正的 host `externref` 仅用于 [wasm] / [import("wasm",...)] 导入函数，
            // 这些导入的类型在 build_import_type_indices 中单独构建。
            GenerateTypeKind.external_ref or GenerateTypeKind.utf8 or GenerateTypeKind.@object or GenerateTypeKind.any
                => WasmValueType.int32,
            _ => WasmValueType.int32
        };
    }

    /// <summary>
    ///     生成仅包含 unreachable + end 的占位函数体，用于无法生成有效代码的函数。
    /// </summary>
    private static WasmCode generate_unreachable_body()
    {
        var writer = new ByteBufferWriter(16);
        writer.write_u8((byte)WasmOpcode.unreachable);
        writer.write_u8((byte)WasmOpcode.end);
        return new WasmCode { locals = [], body = writer.to_array() };
    }

    /// <summary>
    ///     为函数构建 WASM 局部变量列表。
    ///     局部变量索引必须连续，否则抛出 <see cref="InvalidOperationException" />。
    /// </summary>
    private static IReadOnlyList<WasmLocal> build_locals(GenerateFunction function)
    {
        if (function.local_variables.Count == 0) return [];

        var orderedLocals = function.local_variables
            .OrderBy(local => local.index)
            .ToArray();
        var locals = new List<WasmLocal>(orderedLocals.Length);

        for (var expectedIndex = 0; expectedIndex < orderedLocals.Length; expectedIndex++)
        {
            var local = orderedLocals[expectedIndex];
            if (local.index != expectedIndex)
                throw new InvalidOperationException(
                    $"WASM 局部变量索引必须连续，函数 `{function.name}` 在槽位 `{expectedIndex}` 处收到 `{local.index}`。");

            locals.Add(new WasmLocal
            {
                count = 1,
                type = map_value_type(local.type_ref)
            });
        }

        return locals;
    }

    /// <summary>
    ///     将 Nyar 局部变量逻辑索引转换为 WASM 局部变量槽位索引
    ///     （偏移 = 函数参数数 + 逻辑索引）。
    /// </summary>
    private static uint get_wasm_local_index(GenerateFunction function, int localIndex)
    {
        if (localIndex < 0 || localIndex >= function.local_variables.Count)
            throw new InvalidOperationException(
                $"WASM 局部变量槽位越界，函数 `{function.name}` 请求 `{localIndex}`，实际局部变量数为 `{function.local_variables.Count}`。");

        return checked((uint)(function.parameters.Count + localIndex));
    }
}