using Std.Data.Text.Valkyrie.AST.Declaration;
using Std.Data.Text.Valkyrie.AST.Type;

namespace Std.Data.Text.Valkyrie.AST.Shader;

/// <summary>
///     Varying 变量声明，用于在顶点和片元着色器之间传递插值数据///
/// </summary>
/// <para>示例：</para>
/// <code>
/// @varying var worldNormal: vec3;
/// @varying(linear) var texCoord: vec2;
/// </code>
public sealed record VaryingDecl : ValkyrieNode
{
    /// <summary>
    ///     注解信息
    /// </summary>
    public Annotations annotations { get; init; } = new();

    /// <summary>
    ///     Varying 变量名    ///
    /// </summary>
    public string name { get; init; } = string.Empty;

    /// <summary>
    ///     Varying 数据类型
    /// </summary>
    public TypeNode varying_type { get; init; } = null!;

    /// <summary>
    ///     插值修饰符
    /// </summary>
    public string? interpolation { get; init; }
}