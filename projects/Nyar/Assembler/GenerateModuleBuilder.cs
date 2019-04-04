namespace Nyar.Assembler;

/// <summary>
///     元编译模块构建器。
///     该类型用于构建 <see cref="GenerateModule" />，替代历史模块构建路径。
/// </summary>
public sealed class GenerateModuleBuilder
{
    private readonly GenerateModule _module;


    /// <summary>
    ///     创建模块构建器
    /// </summary>
    /// <param name="name">模块名称。</param>
    public GenerateModuleBuilder(string name)
    {
        _module = new CompilationUnit(name);
    }


    /// <summary>
    ///     添加字符串常量
    /// </summary>
    /// <param name="value">字符串值。</param>
    /// <returns>常量索引。</returns>
    public int add_string(string value)
    {
        return _module.constants.add_string(value);
    }


    /// <summary>
    ///     添加整型常量
    /// </summary>
    /// <param name="value">整数值。</param>
    /// <returns>常量索引。</returns>
    public int add_int64(long value)
    {
        return _module.constants.add_int64(value);
    }


    /// <summary>
    ///     添加浮点常量
    /// </summary>
    /// <param name="value">浮点值。</param>
    /// <returns>常量索引。</returns>
    public int add_float64(double value)
    {
        return _module.constants.add_float64(value);
    }


    /// <summary>
    ///     添加函数
    /// </summary>
    /// <param name="name">函数名称。</param>
    /// <param name="parameters">参数类型列表。</param>
    /// <param name="results">返回值类型列表。</param>
    /// <param name="instructions">函数体指令列表。</param>
    /// <returns>函数在模块中的索引。</returns>
    public int add_function(string name, List<string> parameters, List<string> results,
        List<GenerateInstruction> instructions)
    {
        var returnType = results.Count > 0 ? results[0] : "void";
        var func = new GenerateFunction(name, returnType);

        for (var i = 0; i < parameters.Count; i++) func.add_parameter($"%{i}", parameters[i]);

        foreach (var instr in instructions) func.add_instruction(instr);

        _module.add_function(func);
        return _module.functions.Count - 1;
    }


    /// <summary>
    ///     添加函数（带标签）
    /// </summary>
    /// <param name="name">函数名称。</param>
    /// <param name="parameters">参数类型列表。</param>
    /// <param name="results">返回值类型列表。</param>
    /// <param name="instructions">函数体指令列表。</param>
    /// <param name="labels">函数标签列表。</param>
    /// <returns>函数在模块中的索引。</returns>
    public int add_function(string name, List<string> parameters, List<string> results,
        List<GenerateInstruction> instructions, List<GenerateLabelMarker> labels)
    {
        var returnType = results.Count > 0 ? results[0] : "void";
        var func = new GenerateFunction(name, returnType);

        for (var i = 0; i < parameters.Count; i++) func.add_parameter($"%{i}", parameters[i]);

        foreach (var instr in instructions) func.add_instruction(instr);

        // 复制标签，保留原始 instruction_index
        foreach (var label in labels) func.labels.Add(label);

        _module.add_function(func);
        return _module.functions.Count - 1;
    }


    /// <summary>
    ///     添加函数的完整副本（保留参数名、局部变量、标签、属性、外部导入链接等全部字段）
    /// </summary>
    /// <param name="source">源函数。</param>
    /// <returns>新函数在模块中的索引。</returns>
    public int add_function_copy(GenerateFunction source)
    {
        var func = new GenerateFunction(source.name, source.return_type_ref);

        // 复制参数（保留原始参数名和类型）
        foreach (var param in source.parameters) func.add_parameter(param.name, param.type);

        // 复制指令
        foreach (var instr in source.instructions) func.add_instruction(instr);

        // 复制标签，保留原始 instruction_index
        foreach (var label in source.labels) func.labels.Add(label);

        // 复制局部变量
        foreach (var local in source.local_variables) func.add_local_variable(local.name, local.type_ref, local.index);

        // 复制属性
        foreach (var attr in source.attributes) func.add_attribute(attr);

        // 复制外部导入链接
        foreach (var link in source.external_import_links) func.add_external_import_link(link);

        _module.add_function(func);
        return _module.functions.Count - 1;
    }


    /// <summary>
    ///     添加导出
    /// </summary>
    /// <param name="name">导出名称。</param>
    /// <param name="kind">导出类型。</param>
    /// <param name="functionIndex">导出的函数索引。</param>
    public void add_export(string name, GenerateExportKind kind, int functionIndex)
    {
        _module.add_export(new GenerateModuleExport(name, kind, functionIndex));
    }


    /// <summary>
    ///     添加导入
    /// </summary>
    /// <param name="moduleName">来源模块名称。</param>
    /// <param name="importName">导入符号名称。</param>
    /// <param name="kind">导入类型。</param>
    public void add_import(string moduleName, string importName, GenerateImportKind kind)
    {
        _module.add_import(new GenerateModuleImport(moduleName, importName, kind));
    }


    /// <summary>
    ///     添加完整导入
    /// </summary>
    /// <param name="import">模块导入描述。</param>
    public void add_import(GenerateModuleImport import)
    {
        _module.add_import(import);
    }


    /// <summary>
    ///     构建并返回模块
    /// </summary>
    /// <returns>构建完成的元编译模块。</returns>
    public GenerateModule build()
    {
        return _module;
    }
}