namespace Nyar.Assembler;

/// <summary>
///     为 GenerateModule 提供按入口函数的静态可达性裁剪工具，减少多入口重复后端编译。
/// </summary>
public static class GenerateModuleSlicer
{
    /// <summary>
    ///     从给定的完整 GenerateModule 中，按指定入口函数裁剪出只包含该入口及其静态可达函数、导出的最小子模块。
    /// </summary>
    public static GenerateModule slice_by_entry(GenerateModule fullModule, string entryFunctionName)
    {
        return new Context(fullModule).slice_by_entry(entryFunctionName);
    }

    private static void copy_all_constants(GenerateModule fullModule, GenerateModuleBuilder builder)
    {
        foreach (var c in fullModule.constants.strings) builder.add_string(c);
        foreach (var c in fullModule.constants.int64_s) builder.add_int64(c);
        foreach (var c in fullModule.constants.float64_s) builder.add_float64(c);
    }

    /// <summary>
    ///     提供预计算依赖图的切片上下文，适合多入口并行切片场景。
    /// </summary>
    public sealed class Context
    {
        private readonly Dictionary<string, List<string>> _call_graph;
        private readonly Dictionary<string, GenerateFunction> _function_map;
        private readonly GenerateModule _module;
        private readonly Dictionary<string, string> _short_name_to_full_name_map;

        public Context(GenerateModule module)
        {
            _module = module;
            _function_map = module.functions.ToDictionary(f => f.name, f => f, StringComparer.Ordinal);
            _short_name_to_full_name_map = build_unique_short_name_map(module.functions);
            _call_graph = new Dictionary<string, List<string>>(StringComparer.Ordinal);

            foreach (var func in module.functions)
            {
                var deps = new HashSet<string>(StringComparer.Ordinal);
                foreach (var instr in func.instructions)
                foreach (var operand in instr.operands)
                    check_and_add(operand, deps);

                _call_graph[func.name] = [.. deps];
            }
        }

        private void check_and_add(GenerateOperand? operand, HashSet<string> deps)
        {
            if (operand is GenerateOperand.FuncRef funcRef)
                if (try_resolve_function_name(funcRef.name, out var resolvedName))
                    deps.Add(resolvedName);
        }

        public GenerateModule slice_by_entry(string entryFunctionName)
        {
            if (!try_resolve_function_name(entryFunctionName, out var resolvedEntryFunctionName)) return _module;

            var reachableNames = collect_reachable(resolvedEntryFunctionName);
            var builder = new GenerateModuleBuilder(_module.name);

            // 1. 复制常量
            copy_all_constants(_module, builder);

            // 2. 复制导入
            foreach (var import in _module.imports) builder.add_import(import);

            // 3. 复制可达函数（完整复制：参数名、局部变量、标签、属性、外部导入链接等）
            var functionIndexMap = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var origFunc in _module.functions)
                if (reachableNames.Contains(origFunc.name))
                {
                    var idx = builder.add_function_copy(origFunc);
                    functionIndexMap[origFunc.name] = idx;
                }

            // 4. 复制导出
            foreach (var origExport in _module.exports)
                if (origExport.kind == GenerateExportKind.function)
                {
                    var origIndex = origExport.function_index;
                    if (origIndex >= 0 && origIndex < _module.functions.Count)
                    {
                        var origFuncName = _module.functions[origIndex].name;
                        if (functionIndexMap.TryGetValue(origFuncName, out var newIdx)) builder.add_export(origExport.name, origExport.kind, newIdx);
                    }
                }
                else
                {
                    builder.add_export(origExport.name, origExport.kind, origExport.function_index);
                }

            return builder.build();
        }

        private HashSet<string> collect_reachable(string entry)
        {
            var reachable = new HashSet<string>(StringComparer.Ordinal);
            var queue = new Queue<string>();
            queue.Enqueue(entry);
            reachable.Add(entry);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (_call_graph.TryGetValue(current, out var deps))
                    foreach (var dep in deps)
                        if (reachable.Add(dep))
                            queue.Enqueue(dep);
            }

            return reachable;
        }

        /// <summary>
        ///     将 `FuncRef`/入口名统一解析到模块内的完整函数名。
        ///     优先保留精确全名，其次只在短名唯一时回填，避免多模块同名 helper 被误连。
        /// </summary>
        private bool try_resolve_function_name(string functionName, out string resolvedName)
        {
            if (_function_map.ContainsKey(functionName))
            {
                resolvedName = functionName;
                return true;
            }

            if (_short_name_to_full_name_map.TryGetValue(functionName, out resolvedName)) return true;

            resolvedName = string.Empty;
            return false;
        }

        private static Dictionary<string, string> build_unique_short_name_map(
            IEnumerable<GenerateFunction> functions)
        {
            var map = new Dictionary<string, string>(StringComparer.Ordinal);
            var duplicates = new HashSet<string>(StringComparer.Ordinal);

            foreach (var function in functions)
            {
                var shortName = get_short_function_name(function.name);
                if (string.Equals(shortName, function.name, StringComparison.Ordinal)) continue;

                if (!map.TryAdd(shortName, function.name)) duplicates.Add(shortName);
            }

            foreach (var duplicate in duplicates) map.Remove(duplicate);

            return map;
        }

        private static string get_short_function_name(string functionName)
        {
            if (string.IsNullOrWhiteSpace(functionName)) return functionName;

            var lastDot = functionName.LastIndexOf('.');
            return lastDot >= 0 ? functionName[(lastDot + 1)..] : functionName;
        }
    }
}