using System.Globalization;
using Nyar.Types.Targets;
using Std.Data.Binary.Frame;
using Std.Data.Binary.NyarIR.Data;
using Std.Data.Binary.Wasm.Data;

namespace Nyar.Assembler.Backends.Wasm;

/// <summary>
///     <see cref="WasmBackend" /> 的 partial 文件，包含函数可达性分析、入口函数解析、
///     导出处理、类型映射和签名键生成等辅助方法。
/// </summary>
public sealed partial class WasmBackend
{
    /// <summary>
    ///     从入口函数出发，通过 BFS 收集所有被调用的可达函数索引。
    ///     外部函数（标记了 [clr]、[wasm]、[jvm] 属性）不参与遍历。
    /// </summary>
    private static HashSet<int> collect_reachable_functions(GenerateModule module)
    {
        // 收集外部函数名
        var externalNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (var function in module.functions)
            if (function.has_external_import)
            {
                externalNames.Add(function.name);

                // 同时注册短名，使 DCE 能通过短名识别外部函数
                var shortFnName = get_short_name(function.name);
                if (!string.Equals(shortFnName, function.name, StringComparison.Ordinal))
                    externalNames.Add(shortFnName);
            }

        // 确定入口函数索引
        var entryIndices = resolve_entry_indices(module);

        var reachable = new HashSet<int>();
        var queue = new Queue<int>();

        foreach (var entryIndex in entryIndices)
            if (entryIndex >= 0 && entryIndex < module.functions.Count &&
                !externalNames.Contains(module.functions[entryIndex].name))
                if (reachable.Add(entryIndex))
                    queue.Enqueue(entryIndex);

        // BFS 遍历调用图
        while (queue.Count > 0)
        {
            var funcIndex = queue.Dequeue();
            var func = module.functions[funcIndex];

            foreach (var instruction in func.instructions)
            {
                if (instruction.head_code is not (NyarHeadCode.call or NyarHeadCode.call_static)) continue;

                if (instruction.operands.FirstOrDefault() is not GenerateOperand.FuncRef funcRef) continue;

                // 跳过对外部函数的调用
                if (externalNames.Contains(funcRef.name)) continue;

                // 按名称和签名匹配被调用函数
                var calleeIndex = match_function_by_signature(funcRef, module);
                if (calleeIndex >= 0 && reachable.Add(calleeIndex)) queue.Enqueue(calleeIndex);
            }
        }

        return reachable;
    }

    /// <summary>
    ///     按优先级确定入口函数索引集合。
    ///     优先级：1. exports 中的 function 导出项；2. 名为 main 的函数；3. 模块同名函数；4. 第一个函数。
    /// </summary>
    private static HashSet<int> resolve_entry_indices(GenerateModule module)
    {
        // 优先级 1: module.exports 中标记为 function 的导出项
        var exportIndices = module.exports
            .Where(e => e is { kind: GenerateExportKind.function, function_index: >= 0 }
                        && e.function_index < module.functions.Count)
            .Select(e => e.function_index)
            .ToHashSet();
        if (exportIndices.Count > 0) return exportIndices;

        // 优先级 2: 名为 main 的函数（取第一个匹配）
        for (var i = 0; i < module.functions.Count; i++)
            if (string.Equals(module.functions[i].name, "main", StringComparison.Ordinal))
                return [i];

        // 优先级 3: 模块同名的函数（取第一个匹配）
        for (var i = 0; i < module.functions.Count; i++)
            if (string.Equals(module.functions[i].name, module.name, StringComparison.Ordinal))
                return [i];

        // 优先级 4: 第一个函数（兜底）
        if (module.functions.Count > 0) return [0];

        return [];
    }

    /// <summary>
    ///     按名称和签名在模块中匹配函数，返回匹配函数的索引，未找到返回 -1。
    ///     正确处理函数重载（同名不同签名），同时支持全限定名和短名匹配。
    /// </summary>
    private static int match_function_by_signature(GenerateOperand.FuncRef funcRef, GenerateModule module)
    {
        for (var i = 0; i < module.functions.Count; i++)
        {
            var func = module.functions[i];
            if (!names_match(func.name, funcRef.name)) continue;

            // 参数数量必须一致
            if (func.parameters.Count != funcRef.signature.parameters.Count) continue;

            // 逐一比较参数类型
            var paramsMatch = true;
            for (var j = 0; j < func.parameters.Count; j++)
                if (normalize_generate_value_type(func.parameters[j].type_ref) !=
                    funcRef.signature.parameters[j])
                {
                    paramsMatch = false;
                    break;
                }

            if (!paramsMatch) continue;

            // 返回类型一致（void 特殊处理）
            if (funcRef.signature.results.Count == 0 &&
                is_void_return_type(func.return_type_ref))
                return i;

            if (funcRef.signature.results.Count > 0 &&
                normalize_generate_value_type(func.return_type_ref) == funcRef.signature.results[0])
                return i;
        }

        return -1;
    }

    private static int find_entry_function_index(GenerateModule module)
    {
        foreach (var export in module.exports.Where(item =>
                     item.kind == GenerateExportKind.function &&
                     string.Equals(item.name, "main", StringComparison.Ordinal)))
            if (export.function_index >= 0 && export.function_index < module.functions.Count)
                return export.function_index;

        for (var i = 0; i < module.functions.Count; i++)
        {
            var function = module.functions[i];
            if (!string.Equals(function.name, "main", StringComparison.Ordinal)) continue;

            if (function.parameters.Count == 0) return i;
        }

        foreach (var export in module.exports.Where(item => item.kind == GenerateExportKind.function))
            if (export.function_index >= 0 && export.function_index < module.functions.Count)
                return export.function_index;

        return -1;
    }

    private static string resolve_entry_export_name(CompilationOptions options)
    {
        var target = options.target;
        var wantsStartAlias = target?.os == TargetSpecification.wasi ||
                              target?.abi == TargetAbi.wasi_p1 ||
                              target?.abi == TargetAbi.wasi_p2;
        return wantsStartAlias ? "_start" : "main";
    }

    /// <summary>
    ///     检测导出名重复时抛出异常，而非静默丢弃。
    /// </summary>
    private static IReadOnlyList<WasmExport> distinct_exports(IEnumerable<WasmExport> exports)
    {
        var result = new List<WasmExport>();
        var seenNames = new HashSet<string>(StringComparer.Ordinal);

        foreach (var export in exports)
        {
            if (!seenNames.Add(export.name))
                throw new InvalidOperationException(
                    $"WASM 导出名冲突：`{export.name}` 已存在；每个导出名必须唯一，请检查函数重载的消歧逻辑。");

            result.Add(export);
        }

        return result;
    }

    private static byte[] build_i32_offset_expression(int offset)
    {
        var writer = new ByteBufferWriter(16);
        writer.write_u8((byte)WasmOpcode.i32_const);
        writer.write_leb128_i32(offset);
        writer.write_u8((byte)WasmOpcode.end);
        return writer.to_array();
    }

    private static bool should_generate_js_glue(CompilationOptions options)
    {
        var target = options.target;
        return target?.arch is TargetArch.wasm32 or TargetArch.wasm64;
    }

    /// <summary>
    ///     计算 WASM 输出文件名（不含扩展名）。
    ///     优先使用入口函数短名（如 "legion.legion" → "legion"），否则用模块名。
    ///     非字母数字下划线字符会被替换为下划线，首字符为数字时添加 "M_" 前缀。
    /// </summary>
    /// <param name="entryFunctionName">入口函数名（可为 null）</param>
    /// <param name="moduleName">模块名（回退用）</param>
    /// <returns>规范化后的输出文件名</returns>
    private static string compute_output_name(string? entryFunctionName, string moduleName)
    {
        var entryName = entryFunctionName is not null ? get_short_name(entryFunctionName) : null;
        return sanitize_output_name(entryName ?? moduleName);
    }

    /// <summary>
    ///     规范化输出文件名：非字母数字下划线字符替换为下划线，首字符为数字时添加 "M_" 前缀。
    /// </summary>
    /// <param name="name">待规范化的名称</param>
    /// <returns>规范化后的名称</returns>
    private static string sanitize_output_name(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "Module";

        var chars = name.Select(ch => char.IsLetterOrDigit(ch) || ch == '_' ? ch : '_').ToArray();
        var result = new string(chars);
        if (char.IsDigit(result[0])) result = $"M_{result}";

        return result;
    }

    /// <summary>
    ///     获取函数的短名（不含命名空间前缀，即最后一个 '.' 之后的部分）。
    ///     若函数名不含 '.'，则返回原名。
    /// </summary>
    private static string get_short_name(string functionName)
    {
        var lastDotIndex = functionName.LastIndexOf('.');
        return lastDotIndex >= 0 ? functionName[(lastDotIndex + 1)..] : functionName;
    }

    /// <summary>
    ///     比较两个函数名是否匹配，支持全限定名和短名两种形式。
    ///     例如 "test.console_log" 与 "console_log" 视为匹配。
    /// </summary>
    private static bool names_match(string funcName, string refName)
    {
        if (string.Equals(funcName, refName, StringComparison.Ordinal)) return true;

        var funcShort = get_short_name(funcName);
        var refShort = get_short_name(refName);
        return string.Equals(funcShort, refShort, StringComparison.Ordinal);
    }

    private static WasmValueType map_value_type(GenerateValueType valueType)
    {
        return valueType switch
        {
            GenerateValueType.unit => WasmValueType.int32,
            GenerateValueType.@bool => WasmValueType.int32,
            GenerateValueType.i8 => WasmValueType.int32,
            GenerateValueType.i16 => WasmValueType.int32,
            GenerateValueType.i32 => WasmValueType.int32,
            GenerateValueType.i64 => WasmValueType.int64,
            GenerateValueType.f32 => WasmValueType.float32,
            GenerateValueType.f64 => WasmValueType.float64,
            GenerateValueType.function_ref => WasmValueType.func_ref,
            GenerateValueType.external_ref => WasmValueType.int32,
            GenerateValueType.v128 => WasmValueType.v128,
            GenerateValueType.utf8 => WasmValueType.int32,
            GenerateValueType.@object => WasmValueType.int32,
            GenerateValueType.any => WasmValueType.int32,
            _ => WasmValueType.int32
        };
    }

    private static string create_function_signature_key(GenerateFunctionType signature)
    {
        return create_signature_key(signature.parameters, signature.results);
    }

    private static string create_function_signature_key(GenerateFunction function)
    {
        var parameters = function.parameters
            .Select(parameter => normalize_generate_value_type(parameter.type_ref))
            .ToArray();
        var results = is_void_return_type(function.return_type_ref)
            ? Array.Empty<GenerateValueType>()
            : [normalize_generate_value_type(function.return_type_ref)];
        return create_signature_key(parameters, results);
    }

    private static string create_signature_key(
        IReadOnlyList<GenerateValueType> parameters,
        IReadOnlyList<GenerateValueType> results)
    {
        var parameterKey = string.Join(",",
            parameters.Select(static type => ((byte)type).ToString(CultureInfo.InvariantCulture)));
        var resultKey = string.Join(",",
            results.Select(static type => ((byte)type).ToString(CultureInfo.InvariantCulture)));
        return $"{parameterKey}->{resultKey}";
    }

    private static bool is_void_return_type(GenerateTypeReference typeName)
    {
        return typeName.is_void_like;
    }

    private static GenerateValueType normalize_generate_value_type(string typeName)
    {
        return normalize_generate_value_type(GenerateTypeReference.parse(typeName));
    }

    private static GenerateValueType normalize_generate_value_type(GenerateTypeReference typeName)
    {
        return typeName.kind switch
        {
            GenerateTypeKind.@void => GenerateValueType.@void,
            GenerateTypeKind.unit => GenerateValueType.unit,
            GenerateTypeKind.@bool => GenerateValueType.@bool,
            GenerateTypeKind.@char => GenerateValueType.i32,
            GenerateTypeKind.i8 => GenerateValueType.i8,
            GenerateTypeKind.i16 => GenerateValueType.i16,
            GenerateTypeKind.i32 => GenerateValueType.i32,
            GenerateTypeKind.i64 => GenerateValueType.i64,
            GenerateTypeKind.i128 => GenerateValueType.i128,
            GenerateTypeKind.f32 => GenerateValueType.f32,
            GenerateTypeKind.f64 => GenerateValueType.f64,
            GenerateTypeKind.utf8 => GenerateValueType.utf8,
            GenerateTypeKind.utf16 => GenerateValueType.utf16,
            GenerateTypeKind.@object => GenerateValueType.@object,
            GenerateTypeKind.any => GenerateValueType.any,
            GenerateTypeKind.@null => GenerateValueType.@null,
            GenerateTypeKind.function_ref => GenerateValueType.function_ref,
            GenerateTypeKind.external_ref => GenerateValueType.external_ref,
            GenerateTypeKind.v128 => GenerateValueType.v128,
            _ => GenerateValueType.any
        };
    }
}