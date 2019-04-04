using System.Text;
using Std.Data.Binary.Wasm.Data;

namespace Std.Data.Binary.Wasm;

/// <summary>
///     WAT（WebAssembly Text Format）文本发射器的
///     的WasmModuleData 生成可读的WAT 文本，用于调试和验证的
/// </summary>
public static class WatTextEmitter
{
    /// <summary>
    ///     的WasmModuleData 生成 WAT 文本
    /// </summary>
    /// <param name="module">WASM 模块数据。</param>
    /// <returns>WAT 文本内容。</returns>
    public static string emit(WasmModuleData module)
    {
        var sb = new StringBuilder();

        sb.AppendLine("(module");

        emit_types(sb, module);
        emit_imports(sb, module);
        emit_functions(sb, module);
        emit_tables(sb, module);
        emit_memories(sb, module);
        emit_globals(sb, module);
        emit_exports(sb, module);
        emit_data_segments(sb, module);

        sb.AppendLine(")");

        return sb.ToString();
    }

    #region 类型的

    private static void emit_types(StringBuilder sb, WasmModuleData module)
    {
        for (var i = 0; i < module.types.Count; i++)
        {
            var type = module.types[i];
            var paramStr = format_value_types(type.parameters, "param");
            var resultStr = format_value_types(type.results, "result");

            if (type.parameters.Count == 0 && type.results.Count == 0)
                sb.AppendLine($"  (type (;{i};) (func))");
            else
                sb.AppendLine($"  (type (;{i};) (func {paramStr}{resultStr}))");
        }
    }

    #endregion

    #region 导入的

    private static void emit_imports(StringBuilder sb, WasmModuleData module)
    {
        foreach (var import in module.imports)
        {
            var descStr = import.descriptor.kind switch
            {
                WasmExternalKind.function =>
                    $"(func (;{import.descriptor.function_type_index};) (type {import.descriptor.function_type_index}))",
                WasmExternalKind.memory => format_memory_type(import.descriptor.memory_type),
                WasmExternalKind.table => format_table_type(import.descriptor.table_type),
                WasmExternalKind.global => format_global_type(import.descriptor.global_type),
                _ => "(unknown)"
            };

            sb.AppendLine(
                $"  (import \"{escape_wat_string(import.module)}\" \"{escape_wat_string(import.field)}\" {descStr})");
        }
    }

    #endregion

    #region 函数的

    private static void emit_functions(StringBuilder sb, WasmModuleData module)
    {
        var importFuncCount = (uint)module.imports.Count(i => i.descriptor.kind == WasmExternalKind.function);

        for (var i = 0; i < module.function_type_indices.Count; i++)
        {
            var typeIdx = module.function_type_indices[i];
            var funcIdx = importFuncCount + (uint)i;

            if (i < module.codes.Count)
            {
                var code = module.codes[i];
                var type = module.types[(int)typeIdx];

                var paramStr = format_param_list(type.parameters);
                var resultStr = format_result_list(type.results);
                var localStr = format_locals(code.locals);

                sb.AppendLine($"  (func (;{funcIdx};) (type {typeIdx}){paramStr}{resultStr}{localStr}");
                sb.AppendLine($"    ;; 函数的 {code.body.Length} 字节");
                sb.AppendLine("  )");
            }
            else
            {
                sb.AppendLine($"  (func (;{funcIdx};) (type {typeIdx}))");
            }
        }
    }

    #endregion

    #region 表段

    private static void emit_tables(StringBuilder sb, WasmModuleData module)
    {
        for (var i = 0; i < module.tables.Count; i++)
        {
            var table = module.tables[i];
            sb.AppendLine($"  (table (;{i};) {format_table_type(table.type)})");
        }
    }

    #endregion

    #region 内存的

    private static void emit_memories(StringBuilder sb, WasmModuleData module)
    {
        for (var i = 0; i < module.memories.Count; i++)
        {
            var memory = module.memories[i];
            sb.AppendLine($"  (memory (;{i};) {format_memory_type(memory.type)})");
        }
    }

    #endregion

    #region 全局的

    private static void emit_globals(StringBuilder sb, WasmModuleData module)
    {
        for (var i = 0; i < module.globals.Count; i++)
        {
            var global = module.globals[i];
            var mutStr = global.type.mutable ? "(mut " : "";
            var closeMutStr = global.type.mutable ? ")" : "";
            sb.AppendLine($"  (global (;{i};) {mutStr}{format_value_type(global.type.value_type)}{closeMutStr})");
        }
    }

    #endregion

    #region 导出的

    private static void emit_exports(StringBuilder sb, WasmModuleData module)
    {
        foreach (var export in module.exports)
        {
            var kindStr = export.kind switch
            {
                WasmExternalKind.function => "func",
                WasmExternalKind.table => "table",
                WasmExternalKind.memory => "memory",
                WasmExternalKind.global => "global",
                WasmExternalKind.tag => "tag",
                _ => "unknown"
            };

            sb.AppendLine($"  (export \"{escape_wat_string(export.name)}\" ({kindStr} {export.index}))");
        }
    }

    #endregion

    #region 数据的

    private static void emit_data_segments(StringBuilder sb, WasmModuleData module)
    {
        for (var i = 0; i < module.data_segments.Count; i++)
        {
            var data = module.data_segments[i];
            var initStr = data.initializer.Length > 0
                ? $" \"{escape_wat_string(Encoding.UTF8.GetString(data.initializer))}\""
                : "";

            sb.AppendLine($"  (data (;{i};) (memory {data.memory_index}) (offset i32.const 0){initStr})");
        }
    }

    #endregion

    #region 格式化辅的

    private static string format_value_type(WasmValueType type)
    {
        return type switch
        {
            WasmValueType.int32 => "i32",
            WasmValueType.int64 => "i64",
            WasmValueType.float32 => "f32",
            WasmValueType.float64 => "f64",
            WasmValueType.func_ref => "funcref",
            WasmValueType.extern_ref => "externref",
            WasmValueType.any_ref => "anyref",
            WasmValueType.eq_ref => "eqref",
            WasmValueType.i31_ref => "i31ref",
            WasmValueType.struct_ref => "structref",
            WasmValueType.array_ref => "arrayref",
            WasmValueType.null_ref => "nullref",
            WasmValueType.null_func_ref => "nullfuncref",
            WasmValueType.null_extern_ref => "nullexternref",
            WasmValueType.v128 => "v128",
            _ => $"0x{(byte)type:X2}"
        };
    }

    private static string format_value_types(IReadOnlyList<WasmValueType> types, string keyword)
    {
        if (types.Count == 0) return "";

        var parts = types.Select(t => $"{keyword} {format_value_type(t)}");
        return string.Join(" ", parts) + " ";
    }

    private static string format_param_list(IReadOnlyList<WasmValueType> parameters)
    {
        if (parameters.Count == 0) return "";

        var sb = new StringBuilder();
        foreach (var param in parameters) sb.Append($" (param {format_value_type(param)})");

        return sb.ToString();
    }

    private static string format_result_list(IReadOnlyList<WasmValueType> results)
    {
        if (results.Count == 0) return "";

        var sb = new StringBuilder();
        foreach (var result in results) sb.Append($" (result {format_value_type(result)})");

        return sb.ToString();
    }

    private static string format_locals(IReadOnlyList<WasmLocal> locals)
    {
        if (locals.Count == 0) return "";

        var sb = new StringBuilder();
        foreach (var local in locals) sb.Append($" (local {local.count} {format_value_type(local.type)})");

        return sb.ToString();
    }

    private static string format_memory_type(WasmMemoryType? memoryType)
    {
        if (memoryType is null) return "0";

        var limits = memoryType.limits;
        return limits.maximum.HasValue
            ? $"{limits.minimum} {limits.maximum.Value}"
            : $"{limits.minimum}";
    }

    private static string format_table_type(WasmTableType? tableType)
    {
        if (tableType is null) return "0 funcref";

        var limits = tableType.limits;
        var elemType = format_value_type(tableType.element_type);
        return limits.maximum.HasValue
            ? $"{limits.minimum} {limits.maximum.Value} {elemType}"
            : $"{limits.minimum} {elemType}";
    }

    private static string format_global_type(WasmGlobalType? globalType)
    {
        if (globalType is null) return "i32";

        return globalType.mutable
            ? $"(mut {format_value_type(globalType.value_type)})"
            : format_value_type(globalType.value_type);
    }

    private static string escape_wat_string(string s)
    {
        return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }

    #endregion
}