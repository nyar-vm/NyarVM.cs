namespace Std.Data.Binary.Clr;

/// <summary>
///     CLR 泛型类型引用，表示带有类型参数的泛型类型
/// </summary>
public sealed class ClrGenericType : ClrType
{
    /// <summary>
    ///     创建泛型类型引用
    /// </summary>
    /// <param name="name">泛型类型名称。</param>
    /// <param name="typeArguments">类型参数列表。</param>
    public ClrGenericType(string name, List<ClrType> typeArguments)
    {
        this.name = name;
        type_arguments = typeArguments;
    }

    /// <summary>
    ///     泛型类型名称
    /// </summary>
    public string name { get; }

    /// <summary>
    ///     泛型类型参数列表
    /// </summary>
    public List<ClrType> type_arguments { get; }
}