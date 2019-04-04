using Std.Data.Binary.NyarIR.Data;
using Std.Data.Binary.Jvm;
using Nyar.Types.Externals;

namespace Nyar.Assembler.Backends.Jvm;

/// <summary>
///     JVM 外部链接表。
///     统一收集模块中的外部函数信息（[jvm]、[clr]、[wasm]、[import(...)] 属性），
///     并负责可达性分析（死代码消除）、函数签名匹配以及外部引用模糊查找。
///     该表只在 JVM 后端内部使用，不参与其他后端的目标解析。
/// </summary>
internal sealed class JvmExternalLinkTable
{
    private JvmExternalLinkTable(
        Dictionary<string, (string ClassName, string MethodName, string? DescriptorOverride)> jvmExternalRefs,
        HashSet<string> jvmAttributedNames,
        HashSet<string> otherExternalNames)
    {
        jvm_external_refs = jvmExternalRefs;
        jvm_attributed_names = jvmAttributedNames;
        other_external_names = otherExternalNames;
    }

    /// <summary>
    ///     [jvm] 属性或 [import("jvm", ...)] 属性绑定的外部函数映射。
    ///     键为 Nyar 函数名，值为 (JVM 内部类名, JVM 方法名, 可选的 JVM 描述符覆盖)。
    ///     当提供描述符覆盖时，将直接使用该描述符而非从 Valkyrie 类型签名推导。
    /// </summary>
    public Dictionary<string, (string ClassName, string MethodName, string? DescriptorOverride)> jvm_external_refs { get; }

    /// <summary>
    ///     被标记为 JVM 外部函数（含 [jvm] / [import("jvm", ...)]）的函数名集合。
    ///     这些函数不生成 JVM 方法体，在调用点通过 <see cref="jvm_external_refs" /> 解析。
    /// </summary>
    public HashSet<string> jvm_attributed_names { get; }

    /// <summary>
    ///     仅含 [clr] / [wasm] / 非 jvm 的 [import(...)] 属性的外部函数名集合。
    ///     这些函数不生成 JVM 方法体，在调用点弹出参数后压入默认返回值。
    /// </summary>
    public HashSet<string> other_external_names { get; }

    /// <summary>
    ///     扫描模块的所有函数属性，构建 JVM 外部链接表。
    ///     优先级：[jvm] 属性 &gt; [import("jvm", ...)] 属性 &gt; 其他 [import]/[clr]/[wasm] 属性。
    /// </summary>
    /// <param name="module">元编译模块。</param>
    /// <returns>已填充的 <see cref="JvmExternalLinkTable" /> 实例。</returns>
    public static JvmExternalLinkTable build(GenerateModule module)
    {
        var jvmExternalRefs =
            new Dictionary<string, (string ClassName, string MethodName, string? DescriptorOverride)>(StringComparer
                .Ordinal);
        var jvmAttributedNames = new HashSet<string>(StringComparer.Ordinal);
        var otherExternalNames = new HashSet<string>(StringComparer.Ordinal);

        foreach (var function in module.functions)
        {
            // 1. 目标为 JVM 的外部导入链接
            if (function.try_get_external_import_link(CallingConvention.jvm, out var externalImportLink) &&
                externalImportLink is ExternalJvmMethodImport jvmImportLink)
            {
                jvmExternalRefs[function.name] = (
                    jvmImportLink.jvm_type.to_type_name(),
                    jvmImportLink.method_name,
                    jvmImportLink.descriptor_override);
                jvmAttributedNames.Add(function.name);
                continue;
            }

            // 2. 其他目标的外部导入链接：不生成 JVM 方法体
            if (function.has_external_import) otherExternalNames.Add(function.name);
        }

        return new JvmExternalLinkTable(jvmExternalRefs, jvmAttributedNames, otherExternalNames);
    }

    /// <summary>
    ///     判断指定函数是否为外部函数（不生成 JVM 方法体）。
    ///     包含 [jvm] / [import("jvm", ...)] 绑定的函数和其他平台的外部函数。
    /// </summary>
    public bool is_external_function(string functionName)
    {
        return jvm_attributed_names.Contains(functionName) || other_external_names.Contains(functionName);
    }

    /// <summary>
    ///     从入口函数出发，沿调用图做 BFS，返回可达的非外部函数索引集合（死代码消除）。
    /// </summary>
    /// <param name="module">元编译模块。</param>
    /// <returns>可达函数在 <paramref name="module" />.functions 中的索引集合。</returns>
    public HashSet<int> collect_reachable_functions(GenerateModule module)
    {
        var entryIndices = resolve_entry_indices(module);

        var reachable = new HashSet<int>();
        var queue = new Queue<int>();

        foreach (var entryIndex in entryIndices)
        {
            if (entryIndex < 0 || entryIndex >= module.functions.Count) continue;

            // 外部函数不参与可达性遍历
            if (is_external_function(module.functions[entryIndex].name)) continue;

            if (reachable.Add(entryIndex)) queue.Enqueue(entryIndex);
        }

        while (queue.Count > 0)
        {
            var funcIndex = queue.Dequeue();
            var func = module.functions[funcIndex];

            foreach (var instruction in func.instructions)
            {
                if (instruction.head_code is not (NyarHeadCode.call or NyarHeadCode.call_static)) continue;

                if (instruction.operands.FirstOrDefault() is not GenerateOperand.FuncRef funcRef) continue;

                // 跳过对外部函数的调用
                if (is_external_function(funcRef.name)) continue;

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
    public static HashSet<int> resolve_entry_indices(GenerateModule module)
    {
        // 优先级 1: module.exports 中标记为 function 的导出项
        var exportIndices = module.exports
            .Where(e => e is { kind: GenerateExportKind.function, function_index: >= 0 }
                        && e.function_index < module.functions.Count)
            .Select(e => e.function_index)
            .ToHashSet();
        if (exportIndices.Count > 0) return exportIndices;

        // 优先级 2: 名为 main 的函数
        for (var i = 0; i < module.functions.Count; i++)
            if (string.Equals(module.functions[i].name, "main", StringComparison.Ordinal))
                return [i];

        // 优先级 3: 模块同名的函数（支持 namespace.function_name 格式）
        for (var i = 0; i < module.functions.Count; i++)
        {
            var funcName = module.functions[i].name;
            if (string.Equals(funcName, module.name, StringComparison.Ordinal))
                return [i];
        }

        // 优先级 3.5: 查找 namespace 与 function_name 相同的函数（如 legion.legion）
        // 这对应源码中与模块命名空间同名的入口函数
        for (var i = 0; i < module.functions.Count; i++)
        {
            var funcName = module.functions[i].name;
            var dotIndex = funcName.IndexOf('.', StringComparison.Ordinal);
            if (dotIndex > 0 && funcName.LastIndexOf('.') == dotIndex)
            {
                var ns = funcName[..dotIndex];
                var localName = funcName[(dotIndex + 1)..];
                if (string.Equals(ns, localName, StringComparison.Ordinal))
                    return [i];
            }
        }

        // 优先级 4: 第一个函数（兜底）
        return module.functions.Count > 0 ? [0] : [];
    }

    /// <summary>
    ///     按名称和签名在模块中匹配函数，返回匹配函数的索引，未找到返回 -1。
    ///     正确处理函数重载（同名不同签名），并允许自定义类型（映射为 @null）和 any 通配任意类型。
    /// </summary>
    /// <param name="funcRef">函数引用，包含名称和签名。</param>
    /// <param name="module">元编译模块。</param>
    /// <returns>匹配函数的索引，未找到返回 -1。</returns>
    public static int match_function_by_signature(GenerateOperand.FuncRef funcRef, GenerateModule module)
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

            if (func.parameters.Count != funcRef.signature.parameters.Count) continue;

            // 逐一比较参数类型，自定义类型（映射为 @null）和 any 通配任意类型
            var paramsMatch = true;
            for (var j = 0; j < func.parameters.Count; j++)
            {
                var actualParamType = JvmTypeMap.to_value_type(func.parameters[j].type_ref);
                var sigParamType = funcRef.signature.parameters[j];
                if (!is_type_compatible(actualParamType, sigParamType))
                {
                    paramsMatch = false;
                    break;
                }
            }

            if (!paramsMatch) continue;

            var expectedReturn = funcRef.signature.results.Count == 0
                ? GenerateValueType.@void
                : funcRef.signature.results[0];

            var actualReturnType = JvmTypeMap.to_value_type(func.return_type_ref);
            if (!is_type_compatible(actualReturnType, expectedReturn)) continue;

            return i;
        }

        return -1;
    }

    /// <summary>
    ///     判断两个 <see cref="GenerateValueType" /> 是否在 JVM 调用约定下兼容。
    ///     自定义类型（映射为 <see cref="GenerateValueType.@null" />）和 <see cref="GenerateValueType.any" /> 通配任意类型。
    /// </summary>
    private static bool is_type_compatible(GenerateValueType actual, GenerateValueType expected)
    {
        return actual == expected
               || actual == GenerateValueType.@null
               || actual == GenerateValueType.any
               || expected == GenerateValueType.any;
    }

    /// <summary>
    ///     检查操作数名称是否匹配 <see cref="other_external_names" /> 集合。
    ///     操作数名称可能为短名（如 "console_write_line"）或全限定名（如 "test.console_write_line"），
    ///     而集合中可能存相反形式，需做双向模糊匹配。
    /// </summary>
    public bool is_other_external_name(string operandName)
    {
        return matches_name_set(other_external_names, operandName);
    }

    /// <summary>
    ///     检查操作数名称是否匹配 <see cref="jvm_external_refs" />，返回对应的 JVM 引用。
    ///     操作数名称可能是全限定名，需做模糊匹配。
    /// </summary>
    public bool try_get_jvm_external_ref(string operandName,
        out (string ClassName, string MethodName, string? DescriptorOverride) jvmRef)
    {
        if (jvm_external_refs.TryGetValue(operandName, out jvmRef)) return true;

        // 尝试短名称匹配
        var lastDotIndex = operandName.LastIndexOf('.');
        if (lastDotIndex >= 0)
        {
            var shortName = operandName[(lastDotIndex + 1)..];
            return jvm_external_refs.TryGetValue(shortName, out jvmRef);
        }

        jvmRef = default;
        return false;
    }

    /// <summary>
    ///     在名称集合中做双向模糊匹配：支持短名 ↔ 全限定名互查。
    /// </summary>
    private static bool matches_name_set(IReadOnlySet<string> names, string operandName)
    {
        if (names.Contains(operandName)) return true;

        var lastDotIndex = operandName.LastIndexOf('.');
        if (lastDotIndex >= 0)
        {
            var shortName = operandName[(lastDotIndex + 1)..];
            if (names.Contains(shortName)) return true;
        }
        else
        {
            // 操作数是短名，检查集合中是否有以该短名结尾的全限定名
            var dottedSuffix = "." + operandName;
            foreach (var externalName in names)
                if (externalName.EndsWith(dottedSuffix, StringComparison.Ordinal))
                    return true;
        }

        return false;
    }
}
