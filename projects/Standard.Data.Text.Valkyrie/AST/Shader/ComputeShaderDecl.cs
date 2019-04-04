namespace Std.Data.Text.Valkyrie.AST.Shader;

/// <summary>
///     计算着色器阶段声明，用于 GPU 通用计算
/// </summary>
/// <para>示例：</para>
/// <code>
/// compute main {
///     @buffer(0) var data: [f32];
///     fn main() { ... }
/// }
/// </code>
public sealed record ComputeShaderDecl : ShaderStageDecl;