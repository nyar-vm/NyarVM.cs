using Std.Data.Text.Valkyrie.AST.Declaration;
using Std.Data.Text.Valkyrie.AST.Type;

namespace Std.Data.Text.Valkyrie.AST.Shader;

/// <summary>
///     Uniform 绑定声明，将 CPU 侧的字段映射到 GPU 的 Uniform 绑定点///
/// </summary>
/// <para>示例：</para>
/// <code>
/// @uniform_binding var sceneData: SceneUniforms @group(0) @binding(0);
/// </code>
public sealed record UniformBindingDecl : ValkyrieNode
{
    /// <summary>
    ///     注解信息
    /// </summary>
    public Annotations annotations { get; init; } = new();

    /// <summary>
    ///     变量名    ///
    /// </summary>
    public string name { get; init; } = string.Empty;

    /// <summary>
    ///     绑定类型
    /// </summary>
    public TypeNode binding_type { get; init; } = null!;

    /// <summary>
    ///     绑定缁勭储寮?    ///
    /// </summary>
    public int? group { get; init; }

    /// <summary>
    ///     绑定妲界储寮?    ///
    /// </summary>
    public int? binding { get; init; }
}