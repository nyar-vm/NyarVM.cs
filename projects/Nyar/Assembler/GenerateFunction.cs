using Nyar.Types.Externals;

namespace Nyar.Assembler;

/// <summary>
///     元编译函数定义。
/// </summary>
public sealed class GenerateFunction
{
    /// <summary>
    ///     创建函数定义
    /// </summary>
    /// <param name="name">函数名称。</param>
    /// <param name="returnType">返回值类型。</param>
    public GenerateFunction(string name, string returnType)
        : this(name, GenerateTypeReference.parse(returnType))
    {
    }

    /// <summary>
    ///     创建函数定义
    /// </summary>
    /// <param name="name">函数名称。</param>
    /// <param name="returnType">返回值类型引用。</param>
    public GenerateFunction(string name, GenerateTypeReference returnType)
    {
        this.name = name;
        return_type_ref = returnType;
        parameters = [];
        instructions = [];
        labels = [];
        local_variables = [];
        attributes = [];
        external_import_links = [];
    }

    /// <summary>
    ///     函数名称
    /// </summary>
    public string name { get; }


    /// <summary>
    ///     函数参数列表
    /// </summary>
    public List<GenerateFunctionParameter> parameters { get; }


    /// <summary>
    ///     返回值类型
    /// </summary>
    public string return_type => return_type_ref.display_name;

    /// <summary>
    ///     兼容旧后端使用的函数签名视图。
    /// </summary>
    public GenerateFunctionType signature => new()
    {
        parameters = [.. parameters.Select(parameter => parameter.type_ref.value_type ?? GenerateValueType.@null)],
        results = return_type_ref.kind is GenerateTypeKind.@void or GenerateTypeKind.unit
            ? []
            : [return_type_ref.value_type ?? GenerateValueType.@null]
    };

    /// <summary>
    ///     返回值类型的结构化表示。
    /// </summary>
    public GenerateTypeReference return_type_ref { get; }


    /// <summary>
    ///     指令列表
    /// </summary>
    public List<GenerateInstruction> instructions { get; }

    /// <summary>
    ///     标签列表。
    ///     标签绑定到“某条指令之前”的位置，用于跳转类控制流。
    /// </summary>
    public List<GenerateLabelMarker> labels { get; }


    /// <summary>
    ///     局部变量列表
    /// </summary>
    public List<GenerateLocalVariable> local_variables { get; }


    /// <summary>
    ///     函数属性列表
    /// </summary>
    public List<GenerateAttribute> attributes { get; }

    /// <summary>
    ///     函数的外部导入链接列表。
    /// </summary>
    public List<ExternalImport> external_import_links { get; }

    /// <summary>
    ///     是否存在任意外部导入链接。
    /// </summary>
    public bool has_external_import => external_import_links.Count > 0;


    /// <summary>
    ///     添加参数
    /// </summary>
    /// <param name="name">参数名。</param>
    /// <param name="type">参数类型。</param>
    public void add_parameter(string name, string type)
    {
        add_parameter(name, GenerateTypeReference.parse(type));
    }

    /// <summary>
    ///     添加参数
    /// </summary>
    /// <param name="name">参数名。</param>
    /// <param name="type">参数类型引用。</param>
    public void add_parameter(string name, GenerateTypeReference type)
    {
        parameters.Add(new GenerateFunctionParameter(name, type));
    }


    /// <summary>
    ///     添加指令
    /// </summary>
    /// <param name="instruction">指令。</param>
    public void add_instruction(GenerateInstruction instruction)
    {
        instructions.Add(instruction);
    }

    /// <summary>
    ///     在当前位置添加标签。
    /// </summary>
    /// <param name="name">标签名。</param>
    public void add_label(string name)
    {
        if (labels.Any(label => string.Equals(label.name, name, StringComparison.Ordinal)))
            throw new InvalidOperationException($"标签 `{name}` 已存在，不能重复定义。");

        labels.Add(new GenerateLabelMarker(name, instructions.Count));
    }


    /// <summary>
    ///     添加局部变量
    /// </summary>
    /// <param name="name">变量名。</param>
    /// <param name="type">变量类型。</param>
    /// <param name="index">变量索引。</param>
    public void add_local_variable(string name, string type, int index)
    {
        add_local_variable(name, GenerateTypeReference.parse(type), index);
    }

    /// <summary>
    ///     添加局部变量
    /// </summary>
    /// <param name="name">变量名。</param>
    /// <param name="type">变量类型引用。</param>
    /// <param name="index">变量索引。</param>
    public void add_local_variable(string name, GenerateTypeReference type, int index)
    {
        local_variables.Add(new GenerateLocalVariable(name, type, index));
    }


    /// <summary>
    ///     添加函数属性
    /// </summary>
    /// <param name="attribute">属性。</param>
    public void add_attribute(GenerateAttribute attribute)
    {
        attributes.Add(attribute);
    }

    /// <summary>
    ///     添加外部导入链接
    /// </summary>
    /// <param name="externalImportLink">外部导入链接。</param>
    public void add_external_import_link(ExternalImport externalImportLink)
    {
        external_import_links.Add(externalImportLink);
    }

    /// <summary>
    ///     尝试获取指定调用约定的外部导入链接。
    /// </summary>
    /// <param name="convention">目标调用约定。</param>
    /// <param name="externalImportLink">匹配到的外部导入链接。</param>
    /// <returns>是否找到匹配链接。</returns>
    public bool try_get_external_import_link(CallingConvention convention, out ExternalImport externalImportLink)
    {
        foreach (var candidate in external_import_links)
            if (candidate.convention == convention)
            {
                externalImportLink = candidate;
                return true;
            }

        externalImportLink = null!;
        return false;
    }
}
