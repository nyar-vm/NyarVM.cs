namespace Std.Data.Text.Valkyrie.AST.Declaration;

/// <summary>
/// </summary>
/// <para>示例：</para>
/// <code>
///  {
///      field: Type = default
///      method(input: Input) -> Output { ... }
///      domain { ... }
///  }
///  </code>
public sealed record ObjectBody
{
    /// <summary>
    ///     关联类型列表
    /// </summary>
    public IReadOnlyList<DeclareAssociatedType> associated_types { get; init; } = [];

    /// <summary>
    ///     字段列表
    /// </summary>
    public IReadOnlyList<DeclareObjectField> fields { get; init; } = [];

    /// <summary>
    ///     函数列表
    /// </summary>
    public IReadOnlyList<DeclareObjectMethod> methods { get; init; } = [];


    /// <summary>
    ///     子域列表
    /// </summary>
    public IReadOnlyList<DeclareObjectDomain> domains { get; init; } = [];
}