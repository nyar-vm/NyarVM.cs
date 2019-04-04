using Std.Data.Text.Valkyrie.AST.Declaration;
using Std.Data.Text.Valkyrie.AST.Type;

namespace Std.Data.Text.Valkyrie.AST.Shader;

/// <summary>
///     着色器顶点属性声明，定义顶点缓冲区的布局
/// </summary>
/// <para>示例：</para>
/// <code>
/// @attribute(0) var position: vec3;
/// @attribute(1) var normal: vec3;
/// @attribute(2) var uv: vec2;
/// </code>
public sealed record ShaderAttributeDecl : ValkyrieNode
{
    /// <summary>
    ///     注解信息
    /// </summary>
    public Annotations annotations { get; init; } = new();

    /// <summary>
    ///     属性名
    /// </summary>
    public string name { get; init; } = string.Empty;

    /// <summary>
    ///     灞炴€х被鍨?    ///
    /// </summary>
    public TypeNode attr_type { get; init; } = null!;

    /// <summary>
    ///     浣嶇疆绱㈠紩
    /// </summary>
    public int? location { get; init; }
}