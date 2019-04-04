using Std.Data.Text.Valkyrie.AST.Declaration;

namespace Std.Data.Text.Valkyrie.AST.Shader;

/// <summary>
///     Shader 声明，定义一个完整的着色器程序（包含多个着色器阶段）
/// </summary>
/// <para>示例：</para>
/// <code>
/// shader PBR {
///     vertex main_vs {
///         @attribute(0) var position: vec3;
///         @attribute(1) var normal: vec3;
///         @attribute(2) var uv: vec2;
/// 
///         @varying var worldNormal: vec3;
///         @varying var texCoord: vec2;
/// 
///         @uniform var mvp: mat4 @group(0) @binding(0);
/// 
///         fn main() -> vec4 {
///             return mvp * vec4(position, 1.0);
///         }
///     }
/// 
///     fragment main_fs {
///         @varying var worldNormal: vec3;
///         @varying var texCoord: vec2;
/// 
///         @uniform var albedoMap: texture2D @group(1) @binding(0);
///         @uniform var albedoSampler: sampler @group(1) @binding(1);
/// 
///         fn main() -> vec4 {
///             return texture_sample(albedoMap, albedoSampler, texCoord);
///         }
///     }
/// }
/// </code>
public sealed record ShaderDecl : ValkyrieNode, IDeclarationNode
{
    /// <summary>
    ///     着色器阶段列表（顶点/片元/计算）
    /// </summary>
    public IReadOnlyList<ShaderStageDecl> stages { get; init; } = [];

    /// <summary>
    ///     注解信息
    /// </summary>
    public Annotations annotations { get; init; } = new();

    /// <summary>
    ///     Shader 名称
    /// </summary>
    public IdentifierNode? name { get; init; } = new();
}