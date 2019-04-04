using Std.Data.Text.Valkyrie.AST.Type;

namespace Std.Data.Text.Valkyrie.AST.Declaration;

/// <summary>
///     字段声明，结构体、类或组件中的成员变量定义///
/// </summary>
/// <para>示例：</para>
/// <code>
/// name: utf8;
/// health: f32 = 100.0;
/// </code>
public sealed record DeclareObjectField : ValkyrieNode
{
    /// <summary>
    ///     注解信息
    /// </summary>
    public Annotations annotations { get; init; } = new();

    /// <summary>
    ///     字段名称
    /// </summary>
    public string name { get; init; } = string.Empty;

    /// <summary>
    ///     字段类型注解
    /// </summary>
    public TypeNode field_type { get; init; } = null!;

    /// <summary>
    ///     榛樿鍊艰〃杈惧紡锛屽彲涓?<c>null</c>
    /// </summary>
    public ValkyrieNode? default_value { get; init; }
}