namespace VOA.ToolChain.Tests;

/// <summary>
///     编译单元桩实现（测试用），表示一个 Nyar 模块
/// </summary>
public sealed class CompilationUnit
{
    private readonly List<ModuleExport> _exports = [];
    private readonly List<NyarFunction> _functions = [];
    private readonly List<ModuleImport> _imports = [];

    /// <summary>
    ///     创建编译单元
    /// </summary>
    /// <param name="name">模块名称</param>
    public CompilationUnit(string name)
    {
        Name = name;
    }

    /// <summary>模块名称</summary>
    public string Name { get; }

    /// <summary>函数列表</summary>
    public IReadOnlyList<NyarFunction> Functions => _functions;

    /// <summary>导出列表</summary>
    public IReadOnlyList<ModuleExport> Exports => _exports;

    /// <summary>导入列表</summary>
    public IReadOnlyList<ModuleImport> Imports => _imports;

    /// <summary>
    ///     添加函数到编译单元
    /// </summary>
    /// <param name="function">Nyar 函数</param>
    public void AddFunction(NyarFunction function)
    {
        _functions.Add(function);
    }

    /// <summary>
    ///     添加导出声明
    /// </summary>
    /// <param name="export">模块导出</param>
    public void AddExport(ModuleExport export)
    {
        _exports.Add(export);
    }

    /// <summary>
    ///     添加导入声明
    /// </summary>
    /// <param name="import">模块导入</param>
    public void AddImport(ModuleImport import)
    {
        _imports.Add(import);
    }

    /// <summary>
    ///     移除不在白名单中的函数
    /// </summary>
    internal void RemoveUnusedFunctions(HashSet<string> keepNames)
    {
        _functions.RemoveAll(f => !keepNames.Contains(f.Name));
    }

    /// <summary>
    ///     移除不在白名单中的导入
    /// </summary>
    internal void RemoveUnusedImports(HashSet<string> keepNames)
    {
        _imports.RemoveAll(i => !keepNames.Contains(i.Name));
    }
}

/// <summary>
///     Nyar 函数桩实现（测试用）
/// </summary>
public sealed class NyarFunction
{
    private readonly List<Instruction> _body = [];

    /// <summary>
    ///     创建 Nyar 函数
    /// </summary>
    /// <param name="name">函数名</param>
    /// <param name="returnType">返回类型</param>
    public NyarFunction(string name, string returnType)
    {
        Name = name;
        ReturnType = returnType;
    }

    /// <summary>函数名</summary>
    public string Name { get; }

    /// <summary>返回类型</summary>
    public string ReturnType { get; }

    /// <summary>函数体指令列表</summary>
    public IReadOnlyList<Instruction> Body => _body;

    /// <summary>
    ///     添加指令到函数体
    /// </summary>
    /// <param name="instruction">指令</param>
    public void AddInstruction(Instruction instruction)
    {
        _body.Add(instruction);
    }
}

/// <summary>
///     指令桩实现（测试用）
/// </summary>
public sealed class Instruction
{
    /// <summary>
    ///     创建指令
    /// </summary>
    /// <param name="opcode">操作码</param>
    /// <param name="operand">操作数（可选）</param>
    public Instruction(int opcode, Operand? operand = null)
    {
        Opcode = opcode;
        Operand = operand;
    }

    /// <summary>操作码</summary>
    public int Opcode { get; }

    /// <summary>操作数</summary>
    public Operand? Operand { get; }
}

/// <summary>
///     操作数桩实现（测试用）
/// </summary>
public sealed class Operand
{
    private Operand(string kind, string name)
    {
        Kind = kind;
        Name = name;
    }

    /// <summary>操作数种类</summary>
    public string Kind { get; }

    /// <summary>操作数关联名称（如函数引用名）</summary>
    public string Name { get; }

    /// <summary>
    ///     创建函数引用操作数
    /// </summary>
    /// <param name="functionName">被引用的函数名</param>
    /// <returns>函数引用操作数</returns>
    public static Operand CreateFunctionRef(string functionName)
    {
        return new Operand("function_ref", functionName);
    }
}

/// <summary>
///     模块导出桩实现（测试用）
/// </summary>
public sealed class ModuleExport
{
    /// <summary>
    ///     创建模块导出
    /// </summary>
    /// <param name="name">导出名称</param>
    /// <param name="kind">导出种类</param>
    public ModuleExport(string name, string kind)
    {
        Name = name;
        Kind = kind;
    }

    /// <summary>导出名称</summary>
    public string Name { get; }

    /// <summary>导出种类</summary>
    public string Kind { get; }
}

/// <summary>
///     模块导入桩实现（测试用）
/// </summary>
public sealed class ModuleImport
{
    /// <summary>
    ///     创建模块导入
    /// </summary>
    /// <param name="module">来源模块</param>
    /// <param name="name">导入名称</param>
    /// <param name="kind">导入种类</param>
    public ModuleImport(string module, string name, string kind)
    {
        Module = module;
        Name = name;
        Kind = kind;
    }

    /// <summary>来源模块</summary>
    public string Module { get; }

    /// <summary>导入名称</summary>
    public string Name { get; }

    /// <summary>导入种类</summary>
    public string Kind { get; }
}

/// <summary>
///     模块死代码消除桩实现（测试用）
///     移除不可达函数和未使用的导入
/// </summary>
public static class ModuleDce
{
    /// <summary>
    ///     对编译单元执行死代码消除，原地修改
    /// </summary>
    /// <param name="unit">要优化的编译单元</param>
    public static void Run(CompilationUnit unit)
    {
        var reachableFunctions = CollectReachableFunctions(unit);
        unit.RemoveUnusedFunctions(reachableFunctions);

        var usedImports = CollectUsedImports(unit, reachableFunctions);
        unit.RemoveUnusedImports(usedImports);
    }

    private static HashSet<string> CollectReachableFunctions(CompilationUnit unit)
    {
        var reachable = new HashSet<string>();
        var functionByName = unit.Functions.ToDictionary(f => f.Name);

        foreach (var export in unit.Exports)
        {
            if (!reachable.Add(export.Name)) continue;

            CollectCalledFunctions(functionByName, export.Name, reachable);
        }

        return reachable;
    }

    private static void CollectCalledFunctions(
        Dictionary<string, NyarFunction> functionByName,
        string funcName,
        HashSet<string> reachable)
    {
        if (!functionByName.TryGetValue(funcName, out var func)) return;

        foreach (var instruction in func.Body)
            if (instruction.Operand is { Kind: "function_ref" } operand)
                if (reachable.Add(operand.Name))
                    CollectCalledFunctions(functionByName, operand.Name, reachable);
    }

    private static HashSet<string> CollectUsedImports(CompilationUnit unit, HashSet<string> reachableFunctions)
    {
        var usedImports = new HashSet<string>();
        var functionByName = unit.Functions
            .Where(f => reachableFunctions.Contains(f.Name))
            .ToDictionary(f => f.Name);

        foreach (var funcName in reachableFunctions)
        {
            if (!functionByName.TryGetValue(funcName, out var func)) continue;

            foreach (var instruction in func.Body)
                if (instruction.Operand is { Kind: "function_ref" } operand)
                    if (!functionByName.ContainsKey(operand.Name))
                        usedImports.Add(operand.Name);
        }

        return usedImports;
    }
}