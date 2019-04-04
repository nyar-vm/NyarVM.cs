namespace Nyar.Optimizer;

/// <summary>
///     模块级死代码消除
///     从导出函数出发做函数粒度的可达性分析，剔除未被引用的函数和导入
///     在 AsmCompilationUnit 级别工作，在代码生成之后、后端编译之前执行
/// </summary>
public static class ModuleDce
{
    /// <summary>
    ///     对编译单元执行模块级死代码消除
    /// </summary>
    /// <param name="unit">编译单元。</param>
    /// <returns>消除后的编译单元（原地修改）。</returns>
    public static GenerateModule run(GenerateModule unit)
    {
        var reachable = find_reachable_functions(unit);
        remove_unreachable_functions(unit, reachable);
        remove_unused_imports(unit, reachable);
        return unit;
    }

    private static HashSet<string> find_reachable_functions(GenerateModule unit)
    {
        var reachable = new HashSet<string>();
        var queue = new Queue<string>();

        foreach (var export in unit.exports)
        {
            var func = unit.functions.FirstOrDefault(f => f.name == export.name);
            if (func is not null)
            {
                reachable.Add(func.name);
                queue.Enqueue(func.name);
            }
        }

        while (queue.Count > 0)
        {
            var funcName = queue.Dequeue();
            var func = unit.functions.FirstOrDefault(f => f.name == funcName);
            if (func is null) continue;

            foreach (var callee in get_callees(func))
                if (reachable.Add(callee))
                    queue.Enqueue(callee);
        }

        return reachable;
    }

    private static IEnumerable<string> get_callees(GenerateFunction function)
    {
        foreach (var instruction in function.instructions)
        foreach (var operand in instruction.operands)
            if (operand is GenerateOperand.FuncRef funcRef && !string.IsNullOrEmpty(funcRef.name))
                yield return funcRef.name;
    }

    private static void remove_unreachable_functions(GenerateModule unit, HashSet<string> reachable)
    {
        unit.functions.RemoveAll(f => !reachable.Contains(f.name));
    }

    private static void remove_unused_imports(GenerateModule unit, HashSet<string> reachable)
    {
        var usedImportNames = new HashSet<string>();

        foreach (var func in unit.functions)
        foreach (var instruction in func.instructions)
        foreach (var operand in instruction.operands)
            if (operand is GenerateOperand.FuncRef funcRef && !string.IsNullOrEmpty(funcRef.name))
                usedImportNames.Add(funcRef.name);

        var definedFunctions = new HashSet<string>(unit.functions.Select(f => f.name));
        var neededImports = new HashSet<string>();

        foreach (var importName in usedImportNames)
            if (!definedFunctions.Contains(importName))
                neededImports.Add(importName);

        unit.imports.RemoveAll(i => !neededImports.Contains(i.symbol_name));
    }
}