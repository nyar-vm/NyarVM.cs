namespace Nyar.Assembler;

/// <summary>
///     元编译函数参数。
/// </summary>
public sealed class GenerateFunctionParameter
{
    /// <summary>
    ///     创建函数参数。
    /// </summary>
    /// <param name="name">参数名称。</param>
    /// <param name="type">参数类型名称。</param>
    public GenerateFunctionParameter(string name, string type)
        : this(name, GenerateTypeReference.parse(type))
    {
    }

    /// <summary>
    ///     创建函数参数。
    /// </summary>
    /// <param name="name">参数名称。</param>
    /// <param name="type">参数类型引用。</param>
    public GenerateFunctionParameter(string name, GenerateTypeReference type)
    {
        this.name = name;
        type_ref = type;
    }

    /// <summary>
    ///     参数名称。
    /// </summary>
    public string name { get; }

    /// <summary>
    ///     参数类型名称。
    /// </summary>
    public string type => type_ref.display_name;

    /// <summary>
    ///     参数类型的结构化表示。
    /// </summary>
    public GenerateTypeReference type_ref { get; }
}