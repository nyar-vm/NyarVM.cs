namespace Std.Data.Text.Valkyrie.AST.Shader;

/// <summary>
///     顶点着色器阶段声明，处理每个顶点的变换
/// </summary>
/// <para>示例：</para>
/// <code>
/// vertex main {
///     @attribute(0) var position: vec3;
///     @varying var worldPos: vec3;
///     fn main() -> vec4 { ... }
/// }
/// </code>
public sealed record VertexShaderDecl : ShaderStageDecl;