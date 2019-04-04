using Std.Data.Binary.NyarIR.Data;
using Std.Data.Text.Diagnostics;
using static Nyar.Assembler.Backends.Jvm.JvmTypeMap;

namespace Nyar.Assembler.Backends.Jvm;

/// <summary>
///     <see cref="JvmBackend" /> 的 partial 文件，承载可达性分析、入口函数解析、
///     签名匹配和分支标签验证等分析与解析逻辑。
/// </summary>
public partial class JvmBackend
{
    /// <summary>
    ///     收集从入口函数出发的可达函数索引集合（死代码消除）。
    ///     使用 BFS 从入口函数出发，沿调用图遍历，只包含可达的非外部函数。
    /// </summary>
    /// <param name="module">元编译模块。</param>
    /// <returns>可达函数在 module.functions 中的索引集合。</returns>
    private static HashSet<int> collect_reachable_functions(GenerateModule module)
    {
        // 收集外部函数名（[clr]、[wasm]、[jvm]），这些函数不参与 DCE 遍历
        var externalNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (var function in module.functions)
            if (function.has_external_import)
                externalNames.Add(function.name);

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
    /// <param name="module">元编译模块。</param>
    /// <returns>入口函数索引集合。</returns>
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
    ///     正确处理函数重载（同名不同签名）。
    /// </summary>
    /// <param name="funcRef">函数引用，包含名称和签名。</param>
    /// <param name="module">元编译模块。</param>
    /// <returns>匹配函数的索引，未找到返回 -1。</returns>
    private static int match_function_by_signature(GenerateOperand.FuncRef funcRef, GenerateModule module)
    {
        // 提取短名用于 fallback 匹配
        var refShortName = funcRef.name;
        var lastDot = funcRef.name.LastIndexOf('.');
        if (lastDot >= 0) refShortName = funcRef.name[(lastDot + 1)..];

        for (var i = 0; i < module.functions.Count; i++)
        {
            var func = module.functions[i];

            // 名称匹配：精确匹配或短名匹配
            var nameMatch = string.Equals(func.name, funcRef.name, StringComparison.Ordinal);
            if (!nameMatch)
            {
                // 尝试短名匹配：funcRef 短名 == func 短名
                var funcLastDot = func.name.LastIndexOf('.');
                if (funcLastDot >= 0)
                {
                    var funcShortName = func.name[(funcLastDot + 1)..];
                    nameMatch = string.Equals(funcShortName, refShortName, StringComparison.Ordinal);
                }
                else
                {
                    nameMatch = string.Equals(func.name, refShortName, StringComparison.Ordinal);
                }
            }

            if (!nameMatch) continue;

            // 参数数量必须一致
            if (func.parameters.Count != funcRef.signature.parameters.Count) continue;

            // 逐一比较参数类型，自定义类型（映射为 @null）和 any 通配任意类型
            var paramsMatch = true;
            for (var j = 0; j < func.parameters.Count; j++)
            {
                var actualParamType = to_value_type(func.parameters[j].type_ref);
                var sigParamType = funcRef.signature.parameters[j];
                if (actualParamType != sigParamType
                    && actualParamType != GenerateValueType.@null
                    && actualParamType != GenerateValueType.any
                    && sigParamType != GenerateValueType.any)
                {
                    paramsMatch = false;
                    break;
                }
            }

            if (!paramsMatch) continue;

            // 比较返回值类型，自定义类型（映射为 @null）和 any 通配任意类型
            var expectedReturn = funcRef.signature.results.Count == 0
                ? GenerateValueType.@void
                : funcRef.signature.results[0];

            var actualReturnType = to_value_type(func.return_type_ref);
            if (actualReturnType != expectedReturn
                && actualReturnType != GenerateValueType.@null
                && actualReturnType != GenerateValueType.any
                && expectedReturn != GenerateValueType.any)
                continue;

            return i;
        }

        return -1;
    }

    /// <summary>
    ///     校验所有分支指令的目标标签是否已在所属函数中定义。
    /// </summary>
    private static bool validate_branch_labels(GenerateModule module, ICollection<Diagnostic> diagnostics)
    {
        foreach (var function in module.functions)
        {
            var labels = function.labels.Select(label => label.name).ToHashSet(StringComparer.Ordinal);
            foreach (var instruction in function.instructions.Where(instruction =>
                         instruction.head_code is NyarHeadCode.jump or NyarHeadCode.jump_if_true
                             or NyarHeadCode.jump_if_false))
            {
                if (instruction.operands.FirstOrDefault() is not GenerateOperand.Label label)
                {
                    diagnostics.Add(new Diagnostic(
                        default,
                        $"JVM 分支指令 `{instruction.head_code}` 缺少标签操作数。",
                        DiagnosticSeverity.error));
                    return false;
                }

                if (!labels.Contains(label.name))
                {
                    diagnostics.Add(new Diagnostic(
                        default,
                        $"JVM 分支目标标签 `{label.name}` 未定义。",
                        DiagnosticSeverity.error));
                    return false;
                }
            }
        }

        return true;
    }
}