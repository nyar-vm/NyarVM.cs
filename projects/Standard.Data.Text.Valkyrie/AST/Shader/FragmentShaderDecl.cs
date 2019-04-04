namespace Std.Data.Text.Valkyrie.AST.Shader;

/// <summary>
///     片元（像素）着色器阶段声明，处理每个像素的颜色计算
/// </summary>
/// <para>示例：</para>
/// <code>
/// fragment main {
///     @varying var worldPos: vec3;
///     fn main() -> vec4 { ... }
/// }
/// </code>
public sealed record FragmentShaderDecl : ShaderStageDecl;