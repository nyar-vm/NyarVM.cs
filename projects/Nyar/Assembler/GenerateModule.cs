using Std.Data.Binary.NyarIR.Data;
using Nyar.Types.Externals;

namespace Nyar.Assembler;

/// <summary>
///     元编译模块。
///     该类型只描述面向后端的统一输入结构，不承载运行时模块语义，也不定义所谓“核心 IR”。
/// </summary>
public class GenerateModule
{
    /// <summary>
    ///     创建元编译模块
    /// </summary>
    /// <param name="name">模块名称。</param>
    public GenerateModule(string name)
    {
        this.name = name;
        functions = [];
        constants = new GenerateConstantPool();
        imports = [];
        exports = [];
        type_external_imports = [];
        witness_entries = [];
    }

    /// <summary>
    ///     模块名称
    /// </summary>
    public string name { get; }

    /// <summary>
    ///     模块函数列表
    /// </summary>
    public List<GenerateFunction> functions { get; }


    /// <summary>
    ///     模块常量池
    /// </summary>
    public GenerateConstantPool constants { get; }


    /// <summary>
    ///     模块导入列表
    /// </summary>
    public List<GenerateModuleImport> imports { get; }


    /// <summary>
    ///     模块导出列表
    /// </summary>
    public List<GenerateModuleExport> exports { get; }

    /// <summary>
    ///     模块级类型外部导入绑定。
    /// </summary>
    public List<GenerateTypeExternalImportBinding> type_external_imports { get; }

    /// <summary>
    ///     Witness 分派绑定列表。
    ///     仅在需要运行时 witness table 的模块中使用。
    /// </summary>
    public List<GenerateWitnessDispatchEntry> witness_entries { get; }

    /// <summary>
    ///     模块是否携带 witness 元数据。
    ///     这类绑定可供 `NyarVM`/JIT 路径消费，但并不意味着当前模块一定存在运行时 witness 调度。
    /// </summary>
    public bool has_witness_entries => witness_entries.Count > 0;

    /// <summary>
    ///     模块是否包含真正的运行时 witness 分派。
    ///     只有当函数指令流里出现 `CallWitness` 时，AOT 后端才需要显式拒绝。
    /// </summary>
    public bool has_witness_dispatch
    {
        get
        {
            foreach (var function in functions)
            foreach (var instruction in function.instructions)
                if (instruction.head_code == NyarHeadCode.call_witness)
                    return true;

            return false;
        }
    }

    /// <summary>
    ///     源位置
    /// </summary>
    public GenerateLocation? source_location { get; set; }


    /// <summary>
    ///     添加函数
    /// </summary>
    /// <param name="function">函数定义。</param>
    public void add_function(GenerateFunction function)
    {
        functions.Add(function);
    }


    /// <summary>
    ///     添加导入
    /// </summary>
    /// <param name="import">模块导入。</param>
    public void add_import(GenerateModuleImport import)
    {
        imports.Add(import);
    }


    /// <summary>
    ///     添加导出
    /// </summary>
    /// <param name="export">模块导出。</param>
    public void add_export(GenerateModuleExport export)
    {
        exports.Add(export);
    }

    /// <summary>
    ///     添加类型外部导入绑定。
    /// </summary>
    public void add_type_external_import(string typeName, ExternalImport externalImportLink)
    {
        type_external_imports.Add(new GenerateTypeExternalImportBinding(typeName, externalImportLink));
    }

    /// <summary>
    ///     尝试获取指定调用约定的类型外部导入链接。
    /// </summary>
    public bool try_get_type_external_import_link(
        string typeName,
        CallingConvention convention,
        out ExternalImport externalImportLink)
    {
        foreach (var binding in type_external_imports)
        {
            if (!string.Equals(binding.type_name, typeName, StringComparison.Ordinal)) continue;

            if (binding.external_import_link.convention == convention)
            {
                externalImportLink = binding.external_import_link;
                return true;
            }
        }

        externalImportLink = null!;
        return false;
    }


    /// <summary>
    ///     使用结构化类型引用尝试获取指定调用约定的类型外部导入链接。
    /// </summary>
    public bool try_get_type_external_import_link(
        GenerateTypeReference typeReference,
        CallingConvention convention,
        out ExternalImport externalImportLink)
    {
        return try_get_type_external_import_link(typeReference.display_name, convention, out externalImportLink);
    }


    /// <summary>
    ///     添加 witness 分派绑定。
    /// </summary>
    /// <param name="entry">分派绑定。</param>
    public void add_witness_entry(GenerateWitnessDispatchEntry entry)
    {
        witness_entries.Add(entry);
    }
}
