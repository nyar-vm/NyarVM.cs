using Std.Data.Text.Valkyrie.AST.Declaration;
using Std.Data.Text.Valkyrie.AST.Type;

namespace Std.Data.Text.Valkyrie.AST.Shader;

/// <summary>
///     Shader Uniform 变量声明，定义从 CPU 传入 GPU 的常量数据///
/// </summary>
/// <para>示例：</para>
/// <code>
/// @uniform var mvp: mat4 @group(0) @binding(0);
/// @uniform var lightPos: vec3 @group(1) @binding(0);
/// </code>
public sealed record UniformDecl : ValkyrieNode
{
    /// <summary>
    ///     注解信息
    /// </summary>
    public Annotations annotations { get; init; } = new();

    /// <summary>
    ///     Uniform 变量名    ///
    /// </summary>
    public string name { get; init; } = string.Empty;

    /// <summary>
    ///     Uniform 数据类型
    /// </summary>
    public TypeNode uniform_type { get; init; } = null!;

    /// <summary>
    ///     绑定组索引    ///
    /// </summary>
    public int? group { get; init; }

    /// <summary>
    ///     绑定槽索引    ///
    /// </summary>
    public int? binding { get; init; }
}