namespace Nyar.Assembler;

/// <summary>
///     元编译局部变量。
/// </summary>
public sealed class GenerateLocalVariable
{
    /// <summary>
    ///     创建局部变量。
    /// </summary>
    /// <param name="name">局部变量名称。</param>
    /// <param name="type">局部变量类型名称。</param>
    /// <param name="index">局部变量索引。</param>
    public GenerateLocalVariable(string name, string type, int index)
        : this(name, GenerateTypeReference.parse(type), index)
    {
    }

    /// <summary>
    ///     创建局部变量。
    /// </summary>
    /// <param name="name">局部变量名称。</param>
    /// <param name="type">局部变量类型引用。</param>
    /// <param name="index">局部变量索引。</param>
    public GenerateLocalVariable(string name, GenerateTypeReference type, int index)
    {
        this.name = name;
        this.index = index;
        type_ref = type;
    }

    /// <summary>
    ///     局部变量名称。
    /// </summary>
    public string name { get; }

    /// <summary>
    ///     局部变量类型名称。
    /// </summary>
    public string type => type_ref.display_name;

    /// <summary>
    ///     局部变量索引。
    /// </summary>
    public int index { get; }

    /// <summary>
    ///     局部变量类型的结构化表示。
    /// </summary>
    public GenerateTypeReference type_ref { get; }
}